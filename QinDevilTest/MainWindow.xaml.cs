using QinDevilCommon.ColorClass;
using QinDevilCommon.SystemLay;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QinDevilTest {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        public struct POINT {
            public int X;
            public int Y;

            public POINT(int x, int y) {
                this.X = x;
                this.Y = y;
            }
        }
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out POINT pPoint);
        private readonly Timer timer = new Timer();
        private readonly GameData gameData = new GameData();
        public MainWindow() {
            InitializeComponent();
            gamePanel.DataContext = gameData;
            timer.Interval = 200;
            timer.AutoReset = true;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
            gamePanel.Dispatcher.Invoke(() => {
                _ = GetCursorPos(out POINT pnt);
                gameData.MousePoint = string.Format("({0},{1})", pnt.X, pnt.Y);
                DeviceContext DC = new DeviceContext();
                ARGBColor color = ARGBColor.FromInt(DC.GetPointColor(pnt.X, pnt.Y));
                gameData.MouseColor = string.Format("({0},{1},{2})", color.R, color.G, color.B);
                gameData.Color = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)color.R, (byte)color.G, (byte)color.B));
                AYUVColor[] qinKeyColor = {
                    ARGBColor.FromRGB(192, 80, 78).ToAYUVColor(),
                    ARGBColor.FromRGB(156,188,89).ToAYUVColor(),
                    ARGBColor.FromRGB(131,103,164).ToAYUVColor(),
                    ARGBColor.FromRGB(75,172,197).ToAYUVColor(),
                    ARGBColor.FromRGB(246,150,71).ToAYUVColor()
                };
                double[] diff = { 0, 0, 0, 0, 0 };
                AYUVColor Color1 = color.ToAYUVColor();
                for (int i = 0; i < 5; i++) {
                    diff[i] = Color1.GetVariance(qinKeyColor[i]);
                }
                gameData.ColorDifference = diff[0].ToString("0.##") + " | " + diff[1].ToString("0.##") + " | " + diff[2].ToString("0.##") + " | " + diff[3].ToString("0.##") + " | " + diff[4].ToString("0.##");
            });
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            timer.Stop();
        }
    }
}
