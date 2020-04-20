using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace QinDevilCommon.SystemLay {
    public class DeviceContext {
        [StructLayout(LayoutKind.Sequential)]
        public struct Point {
            public int x;
            public int y;
        }
        [DllImport("user32.dll")]//取设备场景 
        private static extern IntPtr GetDC(IntPtr hwnd);//返回设备场景句柄
        [DllImport("user32.dll")]//释放设备场景 
        private static extern int ReleaseDC(IntPtr hDC);//释放设备场景句柄 
        [DllImport("gdi32.dll")]//取指定点颜色 
        private static extern int GetPixel(IntPtr hdc, Point p);
        private readonly IntPtr hdc;
        public DeviceContext() {
            hdc = GetDC(IntPtr.Zero);//取到设备场景(0就是全屏的设备场景) 
        }
        public DeviceContext(IntPtr hwnd) {
            hdc = GetDC(hwnd);//取到设备场景
        }
        ~DeviceContext() {
            _ = ReleaseDC(hdc);
        }
        public int GetPointColor(int x, int y) {
            //置坐标
            Point p = new Point() {
                x = x,
                y = y
            };
            return GetPixel(hdc, p);//取指定点颜色
        }
    }
}
