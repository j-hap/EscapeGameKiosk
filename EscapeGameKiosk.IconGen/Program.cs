// EscapeGameKiosk.IconGen
// Renders logo.svg (via SharpVectors) at 16/32/48/256 px and writes a
// multi-size PNG-in-ICO file. Called by the main project's BeforeBuild target.
//
// Usage: dotnet run -- <svg-path> <ico-output-path>

using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

if (args.Length != 2)
{
  Console.Error.WriteLine("Usage: EscapeGameKiosk.IconGen <svg-path> <ico-path>");
  return 1;
}

var svgPath = args[0];
var icoPath = args[1];

// WPF rendering requires STA apartment state.
int exitCode = 0;
var thread = new Thread(() => exitCode = Run(svgPath, icoPath));
thread.SetApartmentState(ApartmentState.STA);
thread.Start();
thread.Join();
return exitCode;

static int Run(string svgPath, string icoPath)
{
  try
  {
    // Load and parse the SVG
    var settings = new WpfDrawingSettings
    {
      IncludeRuntime = true,
      TextAsGeometry = false,
    };
    var reader = new FileSvgReader(settings);
    var drawing = reader.Read(svgPath)
        ?? throw new InvalidOperationException("SharpVectors returned null drawing.");

    var image = new DrawingImage(drawing);

    // Render each target size to a PNG byte array
    int[] sizes = [16, 32, 48, 256];
    var frames = new Dictionary<int, byte[]>(sizes.Length);

    foreach (int sz in sizes)
    {
      var visual = new DrawingVisual();
      using (var ctx = visual.RenderOpen())
        ctx.DrawImage(image, new Rect(0, 0, sz, sz));

      var bmp = new RenderTargetBitmap(sz, sz, 96, 96, PixelFormats.Pbgra32);
      bmp.Render(visual);
      bmp.Freeze();

      var encoder = new PngBitmapEncoder();
      encoder.Frames.Add(BitmapFrame.Create(bmp));
      using var ms = new MemoryStream();
      encoder.Save(ms);
      frames[sz] = ms.ToArray();
    }

    // Write multi-size ICO (PNG-in-ICO format)
    Directory.CreateDirectory(Path.GetDirectoryName(icoPath)!);
    using var fs = new FileStream(icoPath, FileMode.Create, FileAccess.Write);
    using var bw = new BinaryWriter(fs);

    // ICONDIR header
    bw.Write((ushort)0);             // reserved
    bw.Write((ushort)1);             // type = icon
    bw.Write((ushort)sizes.Length);  // image count

    // ICONDIRENTRY array (6 + 16 * count bytes before first image data)
    int dataOffset = 6 + 16 * sizes.Length;
    foreach (int sz in sizes)
    {
      bw.Write(sz == 256 ? (byte)0 : (byte)sz);  // width  (0 = 256)
      bw.Write(sz == 256 ? (byte)0 : (byte)sz);  // height (0 = 256)
      bw.Write((byte)0);    // color count (0 = no palette)
      bw.Write((byte)0);    // reserved
      bw.Write((ushort)1);  // planes
      bw.Write((ushort)32); // bits per pixel
      bw.Write((uint)frames[sz].Length);
      bw.Write((uint)dataOffset);
      dataOffset += frames[sz].Length;
    }

    // Image data
    foreach (int sz in sizes)
      bw.Write(frames[sz]);

    Console.WriteLine($"Generated: {icoPath}");
    return 0;
  }
  catch (Exception ex)
  {
    Console.Error.WriteLine($"IconGen failed: {ex.Message}");
    return 1;
  }
}
