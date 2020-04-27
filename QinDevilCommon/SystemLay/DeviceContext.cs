using QinDevilCommon.ColorClass;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace QinDevilCommon.SystemLay {
    public class DeviceContext {
        [StructLayout(LayoutKind.Sequential)]
        public struct Point {
            public int x;
            public int y;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFO {
            public BITMAPINFOHEADER bmiHeader;
            public RGBQUAD bmiColors;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFOHEADER {
            public int biSize;
            public int biWidth;
            public int biHeight;
            public short biPlanes;
            public short biBitCount;
            public int biCompression;
            public int biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public int biClrUsed;
            public int biClrImportant;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct RGBQUAD {
            public byte rgbBlue;
            public byte rgbGreen;
            public byte rgbRed;
            public byte rgbReserved;
        }
        [DllImport("user32.dll")]//取设备场景 
        private static extern IntPtr GetDC(IntPtr hwnd);//返回设备场景句柄
        [DllImport("user32.dll")]//释放设备场景
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);//释放设备场景句柄 
        [DllImport("gdi32.dll")]//取指定点颜色 
        private static extern int GetPixel(IntPtr hdc, Point p);
        [DllImport("Gdi32.dll")]//创建兼容设备场景 
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);//返回兼容设备场景句柄
        [DllImport("Gdi32.dll")]//创建与设备无关的位图
        private static extern IntPtr CreateDIBSection(IntPtr hdc, IntPtr pbmi, uint usage, ref IntPtr ppvBits, IntPtr hSection, int offset);//返回与设备无关的位图句柄
        [DllImport("Gdi32.dll")]//选择对象
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);//选择对象
        [DllImport("Gdi32.dll")]//位图拷贝
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BitBlt(IntPtr hdc, int x, int y, int cx, int cy, IntPtr hdcSrc, int x1, int y1, int rop);//位图拷贝
        [DllImport("Gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteDC(IntPtr hdc);//删除设备场景
        [DllImport("Gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr ho);//删除对象
        public class Rect {
            public int left, right, top, bottom;
        }
        private IntPtr handleWindow = IntPtr.Zero;
        private IntPtr handleDeviceContext = IntPtr.Zero;
        private IntPtr handleCompatibleDeviceContext = IntPtr.Zero;
        private Rect cacheRect = null;
        private List<int> colors = new List<int>();
        public bool GetDeviceContext(IntPtr hwnd) {
            if (!handleDeviceContext.Equals(IntPtr.Zero)) {
                _ = ReleaseDC(hwnd, handleDeviceContext);
            }
            handleWindow = hwnd;
            handleDeviceContext = GetDC(handleWindow);
            if (handleDeviceContext.Equals(IntPtr.Zero)) {
                return false;
            }
            return true;
        }
        ~DeviceContext() {
            if (!handleDeviceContext.Equals(IntPtr.Zero)) {
                _ = ReleaseDC(handleWindow, handleDeviceContext);
            }
        }
        public bool CacheRegion(Rect rect) {
            cacheRect = null;
            if (rect == null) {
                return false;
            }
            handleCompatibleDeviceContext = CreateCompatibleDC(handleDeviceContext);
            if (handleCompatibleDeviceContext.Equals(IntPtr.Zero)) {
                return false;
            }
            int width = rect.right - rect.left, height = rect.bottom - rect.top;
            BITMAPINFO pbmi = new BITMAPINFO();
            pbmi.bmiHeader.biSize = 40;
            pbmi.bmiHeader.biWidth = width;
            pbmi.bmiHeader.biHeight = -height;
            pbmi.bmiHeader.biPlanes = 1;
            pbmi.bmiHeader.biBitCount = 24;
            pbmi.bmiHeader.biCompression = 0;
            pbmi.bmiHeader.biSizeImage = width * height * 3;
            int rawsize = Marshal.SizeOf(pbmi);
            IntPtr buffer = Marshal.AllocHGlobal(rawsize), g_hBmp = IntPtr.Zero, g_hOldBmp = IntPtr.Zero, g_pBits = new IntPtr();
            Marshal.StructureToPtr(pbmi, buffer, false);
            g_hBmp = CreateDIBSection(handleCompatibleDeviceContext, buffer, 0, ref g_pBits, IntPtr.Zero, 0);
            Marshal.FreeHGlobal(buffer);
            if (g_hBmp.Equals(IntPtr.Zero)) {
                DeleteDC(handleCompatibleDeviceContext);
                return false;
            }
            g_hOldBmp = SelectObject(handleCompatibleDeviceContext, g_hBmp);
            BitBlt(handleCompatibleDeviceContext, 0, 0, width, height, handleDeviceContext, rect.left, rect.top, 0x00CC0020);
            colors.Clear();
            int fill = (4 - (width * 3 % 4)) % 4;
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    byte R = Marshal.ReadByte(g_pBits, (y * width * 3) + (x * 3) + 2 + fill * y);
                    byte G = Marshal.ReadByte(g_pBits, (y * width * 3) + (x * 3) + 1 + fill * y);
                    byte B = Marshal.ReadByte(g_pBits, (y * width * 3) + (x * 3) + fill * y);
                    colors.Add((R & 0xFF) | ((G & 0xFF) << 8) | ((B & 0xFF) << 16));
                }
            }
            SelectObject(handleCompatibleDeviceContext, g_hOldBmp);
            DeleteObject(g_hBmp);
            DeleteDC(handleCompatibleDeviceContext);
            cacheRect = rect;
            return true;
        }
        public int GetPointColor(int x, int y) {
            if (cacheRect != null && x >= cacheRect.left && x < cacheRect.right && y >= cacheRect.top && y < cacheRect.bottom) {
                return colors[x - cacheRect.left + ((y - cacheRect.top) * (cacheRect.right - cacheRect.left))];
            } else {
                //置坐标
                Point p = new Point() {
                    x = x,
                    y = y
                };
                return GetPixel(handleDeviceContext, p);//取指定点颜色
            }
        }
        /*private void GdiRectangleAlpha(IntPtr hdc, Rect rect, ARGBColor color, byte alpha) {
            IntPtr g_pBits = new IntPtr();
            IntPtr g_hMemDC;
            IntPtr g_hBmp, g_hOldBmp;
            if (rect == null || hdc.Equals(IntPtr.Zero)) {
                return;
            }
            int xMin = rect.left;
            int yMin = rect.top;
            int xMax = rect.right;
            int yMax = rect.bottom;
            int x, y;
            byte r = (byte)color.R;
            byte g = (byte)color.G;
            byte b = (byte)color.B;
            ARGBColor clSrc;
            byte rSrc;
            byte gSrc;
            byte bSrc;
            g_hMemDC = CreateCompatibleDC(hdc);
            if (g_hMemDC.Equals(IntPtr.Zero)) {
                DeleteDC(hdc);
            }
            int iWidth = rect.right - rect.left;
            int iHeight = rect.bottom - rect.top;
            byte[] bmibuf = new byte[40 + 256 * 4];
            //memset(bmibuf, 0, sizeof(bmibuf));
            BITMAPINFO pbmi = new BITMAPINFO();
            // BITMAPINFO pbmi;
            pbmi.bmiHeader.biSize = 40;
            pbmi.bmiHeader.biWidth = iWidth;
            pbmi.bmiHeader.biHeight = iHeight;
            pbmi.bmiHeader.biPlanes = 1;
            pbmi.bmiHeader.biBitCount = 24;
            pbmi.bmiHeader.biCompression = 0;
            pbmi.bmiHeader.biSizeImage = iWidth * iHeight * 3;
            g_hBmp = CreateDIBSection(g_hMemDC, IntPtr.Zero, 0, ref g_pBits, IntPtr.Zero, 0);
            if (g_hBmp.Equals(IntPtr.Zero)) {
                DeleteDC(g_hMemDC);
            }
            g_hOldBmp = SelectObject(g_hMemDC, g_hBmp);
            BitBlt(g_hMemDC, 0, 0, iWidth, iHeight, hdc, 0, 0, 0x00CC0020);
            // offset = y * (width * 24 / 8) + x * (24 / 8)
            for (y = 0; y < iHeight; y++) {
                for (x = 0; x < iWidth; x++) {
                    
                    //rSrc = g_pBits[y * iWidth * 3 + x * 3 + 2];

                    //gSrc = g_pBits[y * iWidth * 3 + x * 3 + 1];

                    //bSrc = g_pBits[y * iWidth * 3 + x * 3];



                    //rSrc = (rSrc * alpha + r * (255 - alpha)) >> 8;

                    //gSrc = (gSrc * alpha + g * (255 - alpha)) >> 8;

                    //bSrc = (bSrc * alpha + b * (255 - alpha)) >> 8;

                    //g_pBits[y * iWidth * 3 + x * 3 + 2] = rSrc;

                    //g_pBits[y * iWidth * 3 + x * 3 + 1] = gSrc;

                    //g_pBits[y * iWidth * 3 + x * 3] = bSrc;
                    
                }
            }
            //BitBlt(hdc, 0, 0, iWidth, iHeight, g_hMemDC, 0, 0, SRCCOPY);
            //SelectObject(g_hMemDC, g_hOldBmp);
            DeleteObject(g_hBmp);
            DeleteDC(g_hMemDC);
            ReleaseDC(IntPtr.Zero, hdc);
        }*/
        /*
        public DeviceContext(Rect rect = null) {
            hwnd = IntPtr.Zero;
            hdc = GetDC(IntPtr.Zero);//取到设备场景(0就是全屏的设备场景) 
            if (rect == null) {

            } else {

            }
        }
        public DeviceContext(IntPtr hwnd, Rect rect = null) {
            this.hwnd = hwnd;
            hdc = GetDC(hwnd);//取到设备场景
        }
        ~DeviceContext() {
            _ = ReleaseDC(hwnd, hdc);
        }
        public int GetPointColor(int x, int y) {
            //置坐标
            Point p = new Point() {
                x = x,
                y = y
            };
            return GetPixel(hdc, p);//取指定点颜色
        }*/
    }
}
