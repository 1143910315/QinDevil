using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace QinDevilCommon.SystemLay {
    public class SystemScreen {
        public static int ScreenWidth() {
            return Screen.PrimaryScreen.Bounds.Width;
        }
        public static int ScreenHeight() {
            return Screen.PrimaryScreen.Bounds.Height;
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
