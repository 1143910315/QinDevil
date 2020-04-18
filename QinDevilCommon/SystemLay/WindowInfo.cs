using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace QinDevilCommon.SystemLay {
    public class WindowInfo {
        [StructLayout(LayoutKind.Sequential)]
        public struct Rect {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct Point {
            public int x;
            public int y;
        }
        /*
        [StructLayout(LayoutKind.Sequential)]
        public struct MenuBarInfo
 {
            DWORD cbSize;
            RECT rcBar;
            HMENU hMenu;
            IntPtr hwndMenu;
            bool fBarFocused = true;
            bool fFocused = true;
        }*/
        [DllImport("User32.dll", EntryPoint = "GetTopWindow")]
        private static extern IntPtr GetTopWindow_DLL(IntPtr hWnd);
        [DllImport("User32.dll", EntryPoint = "GetWindow")]
        private static extern IntPtr GetWindow_DLL(IntPtr hWnd, uint uCmd);
        [DllImport("User32.dll", EntryPoint = "GetWindowRect")]
        private static extern bool GetWindowRect_DLL(IntPtr hWnd, ref Rect lpRect);
        [DllImport("User32.dll", EntryPoint = "GetClientRect")]
        private static extern bool GetClientRect_DLL(IntPtr hWnd, ref Rect lpRect);
        [DllImport("User32.dll", EntryPoint = "ClientToScreen")]
        private static extern bool ClientToScreen_DLL(IntPtr hWnd, ref Point lpPoint);
        /*[DllImport("User32.dll", EntryPoint = "GetMenuBarInfo")]
        private static extern bool GetMenuBarInfo_DLL(IntPtr hwnd,  int idObject,  int idItem,ref MenuBarInfo pmbi);*/
        public enum GettingType : uint {
            GW_HWNDNEXT = 2,
            GW_HWNDPREV = 3
        }
        public static IntPtr GetTopWindow() {
            return GetTopWindow_DLL(IntPtr.Zero);
        }
        public static IntPtr GetWindow(IntPtr windowHandle, GettingType gettingType) {
            return GetWindow_DLL(windowHandle, (uint)gettingType);
        }
        public static Rect GetWindowRect(IntPtr windowHandle) {
            Rect rect = new Rect();
            _ = GetWindowRect_DLL(windowHandle, ref rect);
            return rect;
        }
        public static Rect GetWindowClientRect(IntPtr windowHandle) {
            Rect rect = new Rect();
            _ = GetClientRect_DLL(windowHandle, ref rect);
            return rect;
        }
        public static bool GetScreenPointFromClientPoint(IntPtr windowHandle, ref Point clientPoint) {
            return ClientToScreen_DLL(windowHandle, ref clientPoint);
        }
    }
}
