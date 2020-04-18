using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace QinDevilCommon.SystemLay {
    public class SystemScreen {
        [DllImport("user32.dll")]//取设备场景 
        private static extern IntPtr GetDC(IntPtr hwnd);//返回设备场景句柄 
        [DllImport("user32.dll")]//释放设备场景 
        private static extern int ReleaseDC(IntPtr hDC);//释放设备场景句柄 
        [DllImport("gdi32.dll")]//取指定点颜色 
        private static extern int GetPixel(IntPtr hdc, Point p);

        public static Color GetScreenPointColor(int x, int y) {
            Point p = new Point(x, y);//取置顶点坐标 
            IntPtr hdc = GetDC(IntPtr.Zero);//取到设备场景(0就是全屏的设备场景) 
            int c = GetPixel(hdc, p);//取指定点颜色 
            int r = (c & 0xFF);//转换R 
            int g = (c & 0xFF00) / 256;//转换G 
            int b = (c & 0xFF0000) / 65536;//转换B 
            _ = ReleaseDC(hdc);
            return Color.FromArgb(r, g, b);
        }
        public static Bitmap CaptureScreen() {
            Bitmap baseImage = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics g = Graphics.FromImage(baseImage);
            g.CopyFromScreen(new Point(0, 0), new Point(0, 0), Screen.AllScreens[0].Bounds.Size);
            g.Dispose();
            return baseImage;
        }
    }
}
