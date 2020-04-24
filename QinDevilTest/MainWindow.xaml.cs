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
using Color = System.Drawing.Color;
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
        [DllImport("Kernel32.dll", EntryPoint = "QueryFullProcessImageNameA")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryFullProcessImageNameA(IntPtr hProcess, int dwFlags, [Out] StringBuilder lpExeName, ref int lpdwSize);
        private readonly Timer timer = new Timer();
        private readonly Timer timer1 = new Timer();
        private readonly Timer timer2 = new Timer();
        private readonly Timer timer3 = new Timer();
        private readonly GameData gameData = new GameData();
        public MainWindow() {
            InitializeComponent();
            gamePanel.DataContext = gameData;
            timer.Interval = 200;
            timer.AutoReset = true;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
            timer1.Interval = 200;
            timer1.AutoReset = false;
            timer1.Elapsed += Timer1_Elapsed;
            //timer1.Start();
            timer2.Interval = 200;
            timer2.AutoReset = false;
            timer2.Elapsed += Timer2_Elapsed;
            //timer2.Start();
            timer3.Interval = 20000;
            timer3.AutoReset = false;
            timer3.Elapsed += Timer3_Elapsed;
            timer3.Start();
        }

        private void Timer3_Elapsed(object sender, ElapsedEventArgs e) {
            Debug.WriteLine(Environment.TickCount);
            DeviceContext DC = new DeviceContext();
            if (DC.GetDeviceContext(IntPtr.Zero)) {
                if (DC.CacheRegion(new DeviceContext.Rect { left = 1324, right = 1346, top = 242, bottom = 253 })) {
                    for (int x = 1324; x < 1344; x++) {
                        for (int y = 242; y < 252; y++) {
                            Debug.WriteLine(DC.GetPointColor(x,y));
                        }
                    }
                }
            }
            Debug.WriteLine(Environment.TickCount);
        }

        private void Timer2_Elapsed(object sender, ElapsedEventArgs e) {
            int start = Environment.TickCount;
            DeviceContext deviceContext = new DeviceContext();
            for (int x = 0; x < 900; x++) {
                for (int y = 0; y < 200; y++) {
                    int v = deviceContext.GetPointColor(x, y);
                    if (v == -1) {
                        return;
                    }
                }
            }
            gameData.Key = (Environment.TickCount - start).ToString();
            timer2.Start();
        }
        private void Timer1_Elapsed(object sender, ElapsedEventArgs e) {
            Process process = GetWuXiaProcess();
            if (process != null) {
                int i = 0;
                int length;
                StringBuilder stringBuilder;
                do {
                    i++;
                    length = i * 260;
                    stringBuilder = new StringBuilder(length);
                    QueryFullProcessImageNameA(process.Handle, 0, stringBuilder, ref length);
                    if (length == 0) {
                        stringBuilder = stringBuilder.Clear();
                    }
                } while (i * 260 == length);
                gameData.GamePath = stringBuilder.ToString();
            }
            timer1.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
            gamePanel.Dispatcher.Invoke(() => {
                _ = GetCursorPos(out POINT pnt);
                gameData.MousePoint = string.Format("({0},{1})", pnt.X, pnt.Y);
                DeviceContext DC = new DeviceContext();
                DC.GetDeviceContext(IntPtr.Zero);
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
        private Process GetWuXiaProcess() {
            Process[] process = Process.GetProcessesByName("WuXia_Client_x64");
            if (process.Length == 0) {
                process = Process.GetProcessesByName("WuXia_Client");
            }
            if (process.Length > 0) {
                Process temp = process[0];
                if (process.Length > 0) {
                    IntPtr topWindow = WindowInfo.GetTopWindow();
                    while (!topWindow.Equals(IntPtr.Zero)) {
                        for (int i = 0; i < process.Length; i++) {
                            if (topWindow.Equals(process[i].MainWindowHandle)) {
                                return process[i];
                            }
                        }
                        topWindow = WindowInfo.GetWindow(topWindow, WindowInfo.GettingType.GW_HWNDNEXT);
                    }
                }
                return temp;
            }
            return null;
        }
    }
}
