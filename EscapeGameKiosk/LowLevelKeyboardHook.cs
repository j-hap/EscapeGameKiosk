using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EscapeGameKiosk;

public sealed class LowLevelKeyboardHook : IDisposable
{
  private const int WhKeyboardLl = 13;
  private const int WmKeydown = 0x0100;
  private const int WmSyskeydown = 0x0104;

  private readonly HookProc _proc;
  private readonly Func<KbdllHookStruct, bool> _shouldBlock;
  private IntPtr _hookId = IntPtr.Zero;

  public LowLevelKeyboardHook(Func<KbdllHookStruct, bool> shouldBlock)
  {
    _shouldBlock = shouldBlock;
    _proc = HookCallback;
  }

  public void Start()
  {
    if (_hookId != IntPtr.Zero)
    {
      return;
    }

    using Process currentProcess = Process.GetCurrentProcess();
    using ProcessModule? currentModule = currentProcess.MainModule;
    IntPtr moduleHandle = currentModule != null ? GetModuleHandle(currentModule.ModuleName) : IntPtr.Zero;
    _hookId = SetWindowsHookEx(WhKeyboardLl, _proc, moduleHandle, 0);
  }

  public void Stop()
  {
    if (_hookId == IntPtr.Zero)
    {
      return;
    }

    UnhookWindowsHookEx(_hookId);
    _hookId = IntPtr.Zero;
  }

  public void Dispose()
  {
    Stop();
    GC.SuppressFinalize(this);
  }

  private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
  {
    if (nCode >= 0 && (wParam == (IntPtr)WmKeydown || wParam == (IntPtr)WmSyskeydown))
    {
      KbdllHookStruct data = Marshal.PtrToStructure<KbdllHookStruct>(lParam);
      if (_shouldBlock(data))
      {
        return (IntPtr)1;
      }
    }

    return CallNextHookEx(_hookId, nCode, wParam, lParam);
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct KbdllHookStruct
  {
    public int VkCode;
    public int ScanCode;
    public int Flags;
    public int Time;
    public IntPtr ExtraInfo;
  }

  private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

  [DllImport("user32.dll", SetLastError = true)]
  private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

  [DllImport("user32.dll", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static extern bool UnhookWindowsHookEx(IntPtr hhk);

  [DllImport("user32.dll", SetLastError = true)]
  private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

  [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
  private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
