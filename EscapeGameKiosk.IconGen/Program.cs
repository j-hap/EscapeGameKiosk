// EscapeGameKiosk.IconGen
// Renders .svg (via SharpVectors) at various sizes. Supports two modes:
//
// ICO mode  (existing):
//   dotnet run -- <svg-path> <ico-output-path>
//   → Writes a multi-size PNG-in-ICO at 16/32/48/256 px.
//
// MSIX images mode  (new):
//   dotnet run -- --msix-images <output-dir> <svg-path>
//   → Writes all seven PNG images required for an MSIX Package.appxmanifest.

using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

// ── argument dispatch ────────────────────────────────────────────────────────
if (args.Length == 3 && args[0] == "--msix-images")
{
  string outputDir = args[1];
  string svgPath = args[2];
  return RunOnSta(() => GenerateMsixImages(svgPath, outputDir));
}

if (args.Length == 2)
{
  string svgPath = args[0];
  string icoPath = args[1];
  return RunOnSta(() => GenerateIco(svgPath, icoPath));
}

Console.Error.WriteLine("Usage:");
Console.Error.WriteLine("  EscapeGameKiosk.IconGen <svg-path> <ico-path>");
Console.Error.WriteLine("  EscapeGameKiosk.IconGen --msix-images <output-dir> <svg-path>");
return 1;

// ── helpers ──────────────────────────────────────────────────────────────────
static int RunOnSta(Func<int> work)
{
  int exitCode = 0;
  var t = new Thread(() => exitCode = work());
  t.SetApartmentState(ApartmentState.STA);
  t.Start();
  t.Join();
  return exitCode;
}

/// <summary>
/// Loads an SVG file via SharpVectors and also reads the declared viewBox
/// (or width/height) directly from the XML so callers know the intended canvas
/// size.  SharpVectors returns a <see cref="DrawingGroup"/> whose
/// <see cref="DrawingGroup.Bounds"/> reflects actual path extents, not the SVG
/// viewport – we need the original dimensions separately to render correctly.
/// </summary>
static (DrawingGroup Drawing, double ViewW, double ViewH) LoadSvg(string svgPath)
{
  // Read declared canvas size from the SVG XML.
  double viewW = 512, viewH = 512; // safe fallback
  try
  {
    var root = XDocument.Load(svgPath).Root!;
    var vb = root.Attribute("viewBox")?.Value
               ?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (vb is { Length: 4 }
        && double.TryParse(vb[2], System.Globalization.NumberStyles.Any,
                           System.Globalization.CultureInfo.InvariantCulture, out double vbw)
        && double.TryParse(vb[3], System.Globalization.NumberStyles.Any,
                           System.Globalization.CultureInfo.InvariantCulture, out double vbh))
    {
      viewW = vbw; viewH = vbh;
    }
    else
    {
      if (double.TryParse(root.Attribute("width")?.Value,
                          System.Globalization.NumberStyles.Any,
                          System.Globalization.CultureInfo.InvariantCulture, out double w)) viewW = w;
      if (double.TryParse(root.Attribute("height")?.Value,
                          System.Globalization.NumberStyles.Any,
                          System.Globalization.CultureInfo.InvariantCulture, out double h)) viewH = h;
    }
  }
  catch { /* keep defaults */ }

  var settings = new WpfDrawingSettings { IncludeRuntime = true, TextAsGeometry = false };
  var reader = new FileSvgReader(settings);
  var drawing = reader.Read(svgPath)
      ?? throw new InvalidOperationException("SharpVectors returned null drawing.");
  return (drawing, viewW, viewH);
}

/// <summary>
/// Renders <paramref name="drawing"/> (whose coordinate space spans
/// <paramref name="viewW"/>×<paramref name="viewH"/> as declared by the SVG
/// viewBox) into a <paramref name="w"/>×<paramref name="h"/> bitmap.
///
/// Uses an explicit <see cref="MatrixTransform"/> to map from the declared SVG
/// canvas to the destination rect, bypassing SharpVectors' path-bounding-box
/// (which would otherwise cause paths that don't fill the full viewBox to be
/// scaled up and appear warped).
///
/// When <paramref name="fillCanvas"/> is <c>true</c> the SVG fills the entire
/// bitmap (used for ICO frames).  When <c>false</c> the logo is centered with
/// an 80 % fit and transparent margins (used for MSIX tile/splash images).
/// </summary>
static RenderTargetBitmap RenderToBitmap(
    DrawingGroup drawing, double viewW, double viewH,
    int w, int h, bool fillCanvas = false)
{
  double logoW, logoH, x, y;
  if (fillCanvas)
  {
    logoW = w; logoH = h; x = 0; y = 0;
  }
  else
  {
    // Keep the logo square, fitting inside the shortest dimension with 10 % margin.
    double logoSz = Math.Min(w, h) * 0.80;
    logoW = logoH = logoSz;
    x = (w - logoSz) / 2.0;
    y = (h - logoSz) / 2.0;
  }

  // Scale from the declared SVG viewport (viewW × viewH) to the target rect.
  // We deliberately do NOT use drawing.Bounds here – that is the path bounding
  // box and would cause paths that sit in only part of the viewBox to be
  // stretched to fill the whole frame.
  double sx = logoW / viewW;
  double sy = logoH / viewH;

  var visual = new DrawingVisual();
  using (var ctx = visual.RenderOpen())
  {
    ctx.PushTransform(new MatrixTransform(sx, 0, 0, sy, x, y));
    ctx.DrawDrawing(drawing);
    ctx.Pop();
  }

  var bmp = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
  bmp.Render(visual);
  bmp.Freeze();
  return bmp;
}

static byte[] BitmapToPngBytes(RenderTargetBitmap bmp)
{
  var enc = new PngBitmapEncoder();
  enc.Frames.Add(BitmapFrame.Create(bmp));
  using var ms = new MemoryStream();
  enc.Save(ms);
  return ms.ToArray();
}

static void WritePng(RenderTargetBitmap bmp, string path)
{
  Directory.CreateDirectory(Path.GetDirectoryName(path)!);
  File.WriteAllBytes(path, BitmapToPngBytes(bmp));
  Console.WriteLine($"Generated: {path}");
}

// ── ICO generation (original behaviour) ─────────────────────────────────────
static int GenerateIco(string svgPath, string icoPath)
{
  try
  {
    var (drawing, viewW, viewH) = LoadSvg(svgPath);

    int[] sizes = [16, 32, 48, 256];
    var frames = new Dictionary<int, byte[]>(sizes.Length);

    foreach (int sz in sizes)
      frames[sz] = BitmapToPngBytes(RenderToBitmap(drawing, viewW, viewH, sz, sz, fillCanvas: true));

    Directory.CreateDirectory(Path.GetDirectoryName(icoPath)!);
    using var fs = new FileStream(icoPath, FileMode.Create, FileAccess.Write);
    using var bw = new BinaryWriter(fs);

    // ICONDIR header
    bw.Write((ushort)0);             // reserved
    bw.Write((ushort)1);             // type = icon
    bw.Write((ushort)sizes.Length);  // image count

    // ICONDIRENTRY array
    int dataOffset = 6 + 16 * sizes.Length;
    foreach (int sz in sizes)
    {
      bw.Write(sz == 256 ? (byte)0 : (byte)sz);
      bw.Write(sz == 256 ? (byte)0 : (byte)sz);
      bw.Write((byte)0);
      bw.Write((byte)0);
      bw.Write((ushort)1);
      bw.Write((ushort)32);
      bw.Write((uint)frames[sz].Length);
      bw.Write((uint)dataOffset);
      dataOffset += frames[sz].Length;
    }

    foreach (int sz in sizes)
      bw.Write(frames[sz]);

    Console.WriteLine($"Generated: {icoPath}");
    return 0;
  }
  catch (Exception ex)
  {
    Console.Error.WriteLine($"IconGen (ICO) failed: {ex.Message}");
    return 1;
  }
}

// ── MSIX image generation ────────────────────────────────────────────────────
// Required files and pixel sizes (already at the scale-200 / target resolution):
//   Square44x44Logo.scale-200.png                   →  88 ×  88  (square)
//   Square44x44Logo.targetsize-24_altform-unplated  →  24 ×  24  (square)
//   Square150x150Logo.scale-200.png                 → 300 × 300  (square)
//   Wide310x150Logo.scale-200.png                   → 620 × 300  (wide)
//   LockScreenLogo.scale-200.png                    →  48 ×  48  (square)
//   SplashScreen.scale-200.png                      →1240 × 600  (wide)
//   StoreLogo.png                                   →  50 ×  50  (square)
static int GenerateMsixImages(string svgPath, string outputDir)
{
  try
  {
    var (drawing, viewW, viewH) = LoadSvg(svgPath);

    // square tiles
    WritePng(RenderToBitmap(drawing, viewW, viewH, 88, 88), Path.Combine(outputDir, "Square44x44Logo.scale-200.png"));
    WritePng(RenderToBitmap(drawing, viewW, viewH, 24, 24), Path.Combine(outputDir, "Square44x44Logo.targetsize-24_altform-unplated.png"));
    WritePng(RenderToBitmap(drawing, viewW, viewH, 300, 300), Path.Combine(outputDir, "Square150x150Logo.scale-200.png"));
    WritePng(RenderToBitmap(drawing, viewW, viewH, 48, 48), Path.Combine(outputDir, "LockScreenLogo.scale-200.png"));
    WritePng(RenderToBitmap(drawing, viewW, viewH, 50, 50), Path.Combine(outputDir, "StoreLogo.png"));

    // wide tiles (logo square, centered, transparent margins)
    WritePng(RenderToBitmap(drawing, viewW, viewH, 620, 300), Path.Combine(outputDir, "Wide310x150Logo.scale-200.png"));
    WritePng(RenderToBitmap(drawing, viewW, viewH, 1240, 600), Path.Combine(outputDir, "SplashScreen.scale-200.png"));

    return 0;
  }
  catch (Exception ex)
  {
    Console.Error.WriteLine($"IconGen (MSIX images) failed: {ex.Message}");
    return 1;
  }
}
