using System.ComponentModel;

namespace LuluPet.Win32;

public static class WindowStyleService
{
    public static void ApplyPetWindowStyles(nint hwnd, bool clickThrough)
    {
        if (hwnd == nint.Zero)
        {
            return;
        }

        var exStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GwlExStyle).ToInt64();
        exStyle |= NativeMethods.WsExLayered | NativeMethods.WsExToolWindow;

        if (clickThrough)
        {
            exStyle |= NativeMethods.WsExTransparent;
        }
        else
        {
            exStyle &= ~NativeMethods.WsExTransparent;
        }

        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GwlExStyle, new nint(exStyle));
        KeepTopMost(hwnd);
    }

    public static void KeepTopMost(nint hwnd)
    {
        if (hwnd == nint.Zero)
        {
            return;
        }

        if (!NativeMethods.SetWindowPos(
                hwnd,
                NativeMethods.HwndTopmost,
                0,
                0,
                0,
                0,
                NativeMethods.SwpNoMove
                | NativeMethods.SwpNoSize
                | NativeMethods.SwpNoActivate
                | NativeMethods.SwpNoOwnerZOrder))
        {
            throw new Win32Exception();
        }
    }
}
