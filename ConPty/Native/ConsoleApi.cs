using Microsoft.Win32.SafeHandles;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ConPty.Native
{
    /// <summary>
    /// PInvoke signatures for Win32's Console API.
    /// </summary>
    static class ConsoleApi
    {
        internal const int STD_OUTPUT_HANDLE = -11;
        internal const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        internal const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;
        internal const uint GENERIC_WRITE = 0x40000000;
        internal const uint GENERIC_READ = 0x80000000;
        internal const uint FILE_SHARE_READ = 0x00000001;
        internal const uint FILE_SHARE_WRITE = 0x00000002;
        internal const uint OPEN_EXISTING = 0x00000003;
        internal const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        internal const uint ERROR_ACCESS_DENIED = 5;
        internal const string ConsoleOutPseudoFilename = "CONOUT$";
        
        internal const uint ATTACH_PARRENT = 0xFFFFFFFF;

        [DllImport("kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        internal static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SetConsoleMode(SafeFileHandle hConsoleHandle, uint mode);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetConsoleMode(SafeFileHandle handle, out uint mode);

        [DllImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr CreateFileW(
             string lpFileName,
             uint dwDesiredAccess,
             uint dwShareMode,
             IntPtr lpSecurityAttributes,
             uint dwCreationDisposition,
             uint dwFlagsAndAttributes,
             IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

        [DllImport("kernel32.dll", EntryPoint = "GetConsoleScreenBufferInfo", SetLastError = true)]
        internal static extern bool GetConsoleScreenBufferInfoEx(SafeFileHandle screenBufferHandle, ref CONSOLE_SCREEN_BUFFER_INFO_EX lpConsoleScreenBufferInfo);

        [StructLayout(LayoutKind.Sequential)]
        public struct CONSOLE_SCREEN_BUFFER_INFO_EX
        {
            public uint cbSize;
            public COORD dwSize;
            public COORD dwCursorPosition;
            public short wAttributes;
            public SMALL_RECT srWindow;
            public COORD dwMaximumWindowSize;
            public ushort wPopupAttributes;
            public bool bFullscreenSupported;
            public COLORREF black;
            public COLORREF darkBlue;
            public COLORREF darkGreen;
            public COLORREF darkCyan;
            public COLORREF darkRed;
            public COLORREF darkMagenta;
            public COLORREF darkYellow;
            public COLORREF gray;
            public COLORREF darkGray;
            public COLORREF blue;
            public COLORREF green;
            public COLORREF cyan;
            public COLORREF red;
            public COLORREF magenta;
            public COLORREF yellow;
            public COLORREF white;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct COORD
        {
            public short X;
            public short Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SMALL_RECT
        {
            short Left;
            short Top;
            short Right;
            short Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]        
        public struct COLORREF
        {
            public uint ColorDWORD;

            public COLORREF(Color color)
            {
                ColorDWORD = color.R + (((uint)color.G) << 8) + (((uint)color.B) << 16);
            }

            public COLORREF(uint r, uint g, uint b)
            {
                ColorDWORD = r + (g << 8) + (b << 16);
            }

            public Color GetColor()
            {
                return Color.FromArgb(
                    0,
                    (byte)(0x000000FFU & ColorDWORD),
                    (byte)((0x0000FF00U & ColorDWORD) >> 8),
                    (byte)((0x00FF0000U & ColorDWORD) >> 16));
            }
        }

        internal delegate bool ConsoleEventDelegate(CtrlTypes ctrlType);

        internal enum CtrlTypes : uint
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }        
    }
}
