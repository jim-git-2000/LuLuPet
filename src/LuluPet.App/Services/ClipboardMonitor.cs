using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using LuluPet.Win32;
using WpfTextDataFormat = System.Windows.TextDataFormat;

namespace LuluPet.App.Services;

public sealed class ClipboardMonitor : IDisposable
{
    private HwndSource? _source;
    private nint _windowHandle;
    private bool _isListening;

    public event EventHandler<string>? TextChanged;

    public void Start(nint windowHandle)
    {
        if (windowHandle == nint.Zero || _isListening)
        {
            return;
        }

        _source = HwndSource.FromHwnd(windowHandle);
        if (_source is null)
        {
            return;
        }

        if (!NativeMethods.AddClipboardFormatListener(windowHandle))
        {
            Debug.WriteLine("Failed to register clipboard format listener.");
            _source = null;
            return;
        }

        _windowHandle = windowHandle;
        _source.AddHook(WndProc);
        _isListening = true;
        CaptureCurrentText();
    }

    public void Dispose()
    {
        Stop();
    }

    private void Stop()
    {
        if (!_isListening)
        {
            return;
        }

        _source?.RemoveHook(WndProc);
        if (_windowHandle != nint.Zero)
        {
            NativeMethods.RemoveClipboardFormatListener(_windowHandle);
        }

        _source = null;
        _windowHandle = nint.Zero;
        _isListening = false;
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == NativeMethods.WmClipboardUpdate)
        {
            CaptureCurrentText();
        }

        return nint.Zero;
    }

    private void CaptureCurrentText()
    {
        try
        {
            if (!System.Windows.Clipboard.ContainsText(WpfTextDataFormat.UnicodeText))
            {
                return;
            }

            var text = System.Windows.Clipboard.GetText(WpfTextDataFormat.UnicodeText);
            if (!string.IsNullOrWhiteSpace(text))
            {
                TextChanged?.Invoke(this, text);
            }
        }
        catch (Exception exception) when (exception is ExternalException
            or InvalidOperationException)
        {
            Debug.WriteLine($"Failed to read clipboard text: {exception.Message}");
        }
    }
}
