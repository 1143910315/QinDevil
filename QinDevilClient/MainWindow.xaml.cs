using QinDevilCommon;
using QinDevilCommon.Data;
using QinDevilCommon.Image;
using QinDevilCommon.SystemLay;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Timer = System.Timers.Timer;
using Color = System.Drawing.Color;
using System.Media;
using QinDevilCommon.ColorClass;
using System.Windows.Forms;
using TextBox = System.Windows.Controls.TextBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Cursors = System.Windows.Input.Cursors;
using MessageBox = System.Windows.MessageBox;
using System.Net;
using System.Net.Sockets;
using QinDevilCommon.LogClass;
using QinDevilCommon.Keyboard;
using Label = System.Windows.Controls.Label;
using System.Reflection;
#if service
using QinDevilCommon.Keyboard;
#endif

namespace QinDevilClient {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        private const int SW_HIDE = 0; //隐藏任务栏
        private const int SW_RESTORE = 9;//显示任务栏

        [DllImport("user32.dll")]
        public static extern int ShowWindow(int hwnd, int nCmdShow);
        [DllImport("user32.dll")]
        public static extern int FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        public static extern int GetWindowTextA(IntPtr hWnd, [Out] StringBuilder lpString, int nMaxCount);
        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("Psapi.dll", EntryPoint = "GetModuleFileNameEx")]
        public static extern int GetModuleFileNameEx(IntPtr handle, IntPtr hModule, [Out] StringBuilder lpszFileName, int nSize);
        [DllImport("Kernel32.dll", EntryPoint = "QueryFullProcessImageNameA")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryFullProcessImageNameA(IntPtr hProcess, int dwFlags, [Out] StringBuilder lpExeName, ref int lpdwSize);
        [DllImport("User32.dll", EntryPoint = "keybd_event")]
        public static extern void Keybd_event(int bVk, int bScan, int dwFlags, int dwExtraInfo);
        [DllImport("User32.dll", EntryPoint = "MapVirtualKeyA")]
        public static extern int MapVirtualKeyA(int uCode, int uMapType);
        private readonly SocketClient client;
        private readonly Timer timer = new Timer();
        private readonly Timer pingTimer = new Timer();
        private readonly Timer discernTimer = new Timer();
        private readonly Timer hitKeyTimer = new Timer();
        private readonly Timer hitKeyCDTimer = new Timer();
        private readonly Timer colorDiscriminateTimer = new Timer();
        private readonly Timer secondTimer = new Timer();
        private readonly Timer connectTimer = new Timer();
        private readonly GameData gameData = new GameData();
        private readonly Regex QinKeyLessMatch = new Regex("^(?![1-5]*?([1-5])[1-5]*?\\1)[1-5]{0,3}$");
        private MemoryStream pictureStream = null;
        private MemoryStream pngStream = null;
        private readonly byte[] bigBuffer = new byte[8000];
        private readonly List<byte> sendData = new List<byte>();
        private bool startPing = false;
        private int lastPing;
        private readonly byte[] machineIdentity;
        private bool sendInfoSuccess = false;
        private readonly Random r = new Random();
        private int discernTimers = 0;
        private KeyboardHook hook;
        private bool ctrlState;
        public MainWindow() {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            InitializeComponent();
            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(SystemInfo.GetMacAddress() + SystemInfo.GetCpuID()));
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash) {
                _ = sb.Append(b.ToString("X2"));
            }
            machineIdentity = SerializeTool.StringToByte(sb.ToString());
            GamePanel.DataContext = gameData;
            client = new SocketClient();
            client.OnConnectedEvent += OnConnected;
            client.OnReceivePackageEvent += OnReceivePackage;
            client.OnConnectionBreakEvent += OnConnectionBreak;
            connectTimer.Elapsed += ConnectTimer_Elapsed;
            connectTimer.AutoReset = false;
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = false;
            pingTimer.Interval = 150;
            pingTimer.Elapsed += PingTimer_Elapsed;
            pingTimer.AutoReset = false;
            pingTimer.Start();
            discernTimer.Interval = 900;
            discernTimer.Elapsed += DiscernTimer_Elapsed;
            discernTimer.AutoReset = false;
            hitKeyTimer.Interval = 150;
            hitKeyTimer.Elapsed += HitKeyTimer_Elapsed;
            hitKeyTimer.AutoReset = false;
            hitKeyTimer.Start();
            colorDiscriminateTimer.Interval = 5000;
            colorDiscriminateTimer.Elapsed += ColorDiscriminateTimer_Elapsed;
            colorDiscriminateTimer.AutoReset = false;
            colorDiscriminateTimer.Start();
            secondTimer.Interval = 1000;
            secondTimer.Elapsed += SecondTimer_Elapsed;
            secondTimer.AutoReset = true;
            hitKeyCDTimer.Interval = 1000;
            hitKeyCDTimer.Elapsed += HitKeyCDTimer_Elapsed;
            hitKeyCDTimer.AutoReset = true;
#if Line9420
            gameData.Line = 9420;
#endif
            Connect();
        }
        private void HitKeyCDTimer_Elapsed(object sender, ElapsedEventArgs e) {
            int qinIndex = Autoplay.Dispatcher.Invoke(() => {
                return combo.SelectedIndex;
            });
            if (qinIndex != -1 && JudgeSitOn()) {
                if (gameData.HitKeyCD < 2000) {
                    gameData.HitKeyCD += 1000;
                }
            } else {
                int cd = gameData.HitKeyCD - 2000;
                gameData.HitKeyCD = cd < 0 ? 0 : cd;
            }
        }
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            _ = ShowWindow(FindWindow("Shell_TrayWnd", null), SW_RESTORE);
            string str = "\n--- Error ---\n" + e.ExceptionObject.GetType().FullName;
            if (e.ExceptionObject is Exception ex) {
                str += "\nMessage\n" + ex.Message;
                str += "\nHelpLink\n" + ex.HelpLink;
                str += "\nSource\n" + ex.Source;
                str += "\nStackTrace\n" + ex.StackTrace;
                str += "\nTargetSite\n" + ex.TargetSite;
            }
            File.AppendAllText(".\\错误日志.log", str);
        }
        private void ConnectTimer_Elapsed(object sender, ElapsedEventArgs e) {
            Connect();
        }
        private void SecondTimer_Elapsed(object sender, ElapsedEventArgs e) {
            gameData.Time += 1;
        }
        private void ColorDiscriminateTimer_Elapsed(object sender, ElapsedEventArgs e) {
            if (sendInfoSuccess) {
                Process process = GetWuXiaProcess();
                if (process != null) {
                    WindowInfo.Rect rect = WindowInfo.GetWindowClientRect(process.MainWindowHandle);
                    if (rect.bottom > 100 && rect.right > 100) {
                        WindowInfo.Point point = new WindowInfo.Point() {
                            x = 0,
                            y = 0
                        };
                        if (WindowInfo.GetScreenPointFromClientPoint(process.MainWindowHandle, ref point)) {
                            if (gameData.KillingIntentionStrip == 0) {
                                AYUVColor color = ARGBColor.FromRGB(254, 184, 0).ToAYUVColor();
                                int startX = point.x + (rect.right / 2);
                                int endX = startX + 1;
                                int startY = point.y;
                                int endY = point.y + rect.bottom;
                                DeviceContext DC = new DeviceContext();
                                if (DC.GetDeviceContext(IntPtr.Zero)) {
                                    _ = DC.CacheRegion(new DeviceContext.Rect { left = startX, right = endX, top = startY, bottom = endY });
                                    for (int i = endY - 1; i > startY; i--) {
                                        AYUVColor color2 = ARGBColor.FromInt(DC.GetPointColor(startX, i)).ToAYUVColor();
                                        if (color.GetVariance(color2) < 5) {
                                            gameData.KillingIntentionStrip = rect.bottom - i + point.y;
                                            client.SendPackage(11, SerializeTool.IntToByte(gameData.KillingIntentionStrip));
                                        }
                                    }
                                }
                            } else {
                                AYUVColor[] qinKeyColor = new AYUVColor[5];
                                qinKeyColor[0] = ARGBColor.FromRGB(192, 80, 78).ToAYUVColor();
                                qinKeyColor[1] = ARGBColor.FromRGB(156, 188, 89).ToAYUVColor();
                                qinKeyColor[2] = ARGBColor.FromRGB(129, 101, 162).ToAYUVColor();
                                qinKeyColor[3] = ARGBColor.FromRGB(75, 172, 197).ToAYUVColor();
                                qinKeyColor[4] = ARGBColor.FromRGB(246, 150, 71).ToAYUVColor();
                                int startX = point.x;
                                int endX = point.x + (rect.right / 2);
                                int middleY = point.y + rect.bottom - (gameData.KillingIntentionStrip / 2);
                                int startY = middleY - 5;
                                int endY = startY + 10;
                                DeviceContext DC = new DeviceContext();
                                if (DC.GetDeviceContext(IntPtr.Zero)) {
                                    _ = DC.CacheRegion(new DeviceContext.Rect { left = startX, right = endX, top = startY, bottom = endY });
                                    int[] tempFiveTone = new int[5];
                                    int i = endX;
                                    for (int j = 4; j > -1;) {
                                        for (; i > startX; i--) {
                                            AYUVColor color = ARGBColor.FromInt(DC.GetPointColor(i, middleY)).ToAYUVColor();
                                            if (color.GetVariance(qinKeyColor[j]) < 25) {
                                                int matchTime = 1;
                                                for (int k = 0; k < 8; k++) {
                                                    color = ARGBColor.FromInt(DC.GetPointColor(i, middleY - k - 1)).ToAYUVColor();
                                                    if (matchTime < 8 && color.GetVariance(qinKeyColor[j]) < 25) {
                                                        matchTime += 1;
                                                    } else {
                                                        break;
                                                    }
                                                }
                                                for (int k = 1; k < 8; k++) {
                                                    color = ARGBColor.FromInt(DC.GetPointColor(i, middleY + k)).ToAYUVColor();
                                                    if (matchTime < 8 && color.GetVariance(qinKeyColor[j]) < 25) {
                                                        matchTime += 1;
                                                    } else {
                                                        break;
                                                    }
                                                }
                                                if (matchTime > 7) {
                                                    tempFiveTone[j] = i - point.x;
                                                    if (j == 0) {
                                                        gameData.FiveTone = tempFiveTone;
                                                        gameData.FiveToneReady = true;
                                                        Dispatcher.Invoke(() => {
                                                            combo.IsEnabled = true;
                                                        });
                                                        lock (sendData) {
                                                            sendData.Clear();
                                                            SerializeTool.IntToByteList(gameData.FiveTone[0], sendData);
                                                            SerializeTool.IntToByteList(gameData.FiveTone[1], sendData);
                                                            SerializeTool.IntToByteList(gameData.FiveTone[2], sendData);
                                                            SerializeTool.IntToByteList(gameData.FiveTone[3], sendData);
                                                            SerializeTool.IntToByteList(gameData.FiveTone[4], sendData);
                                                            client.SendPackage(12, sendData.ToArray());
                                                        }
                                                        return;
                                                    }
                                                    break;
                                                }
                                            }
                                        }
                                        j--;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            colorDiscriminateTimer.Start();
        }
        private void HitKeyTimer_Elapsed(object sender, ElapsedEventArgs e) {
            (bool canHit, int qinIndex) = Autoplay.Dispatcher.Invoke(() => {
                return (Autoplay.IsChecked.HasValue && Autoplay.IsChecked.Value, combo.SelectedIndex);
            });
            if (canHit) {
                int i = 0;
                for (; i < gameData.HitQinKey.Length; i++) {
                    if (gameData.HitQinKey[i] == 0) {
                        break;
                    }
                }
                if (i > gameData.HitKeyIndex) {
                    if (qinIndex != -1) {
                        if (JudgeSitOn()) {
                            if (gameData.HitKeyCD > 1500) {
                                lock (sendData) {
                                    sendData.Clear();
                                    SerializeTool.IntToByteList(qinIndex, sendData);
                                    SerializeTool.IntToByteList(gameData.HitKeyIndex + 1, sendData);
                                    client.SendPackage(22, sendData.ToArray());
                                }
                                switch (gameData.HitQinKey[gameData.HitKeyIndex++]) {
                                    case 1: {
                                            Keybd_event(49, MapVirtualKeyA(49, 0), 8, 0);
                                            Thread.Sleep(r.Next(20, 60));
                                            Keybd_event(49, MapVirtualKeyA(49, 0), 10, 0);
                                            break;
                                        }
                                    case 2: {
                                            Keybd_event(50, MapVirtualKeyA(50, 0), 8, 0);
                                            Thread.Sleep(r.Next(20, 60));
                                            Keybd_event(50, MapVirtualKeyA(50, 0), 10, 0);
                                            break;
                                        }
                                    case 3: {
                                            Keybd_event(51, MapVirtualKeyA(51, 0), 8, 0);
                                            Thread.Sleep(r.Next(20, 60));
                                            Keybd_event(51, MapVirtualKeyA(51, 0), 10, 0);
                                            break;
                                        }
                                    case 4: {
                                            Keybd_event(52, MapVirtualKeyA(52, 0), 8, 0);
                                            Thread.Sleep(r.Next(20, 60));
                                            Keybd_event(52, MapVirtualKeyA(52, 0), 10, 0);
                                            break;
                                        }
                                    case 5: {
                                            Keybd_event(53, MapVirtualKeyA(53, 0), 8, 0);
                                            Thread.Sleep(r.Next(20, 60));
                                            Keybd_event(53, MapVirtualKeyA(53, 0), 10, 0);
                                            break;
                                        }
                                    default:
                                        break;
                                }
                            }
                        }
                    } else {
                        if (JudgeSitOn()) {
                            client.SendPackage(19, SerializeTool.IntToByte(gameData.HitKeyIndex));
                        } else {
                            client.SendPackage(20, SerializeTool.IntToByte(gameData.HitKeyIndex));
                        }
                        switch (gameData.HitQinKey[gameData.HitKeyIndex++]) {
                            case 1: {
                                    Keybd_event(49, MapVirtualKeyA(49, 0), 8, 0);
                                    Thread.Sleep(r.Next(20, 60));
                                    Keybd_event(49, MapVirtualKeyA(49, 0), 10, 0);
                                    break;
                                }
                            case 2: {
                                    Keybd_event(50, MapVirtualKeyA(50, 0), 8, 0);
                                    Thread.Sleep(r.Next(20, 60));
                                    Keybd_event(50, MapVirtualKeyA(50, 0), 10, 0);
                                    break;
                                }
                            case 3: {
                                    Keybd_event(51, MapVirtualKeyA(51, 0), 8, 0);
                                    Thread.Sleep(r.Next(20, 60));
                                    Keybd_event(51, MapVirtualKeyA(51, 0), 10, 0);
                                    break;
                                }
                            case 4: {
                                    Keybd_event(52, MapVirtualKeyA(52, 0), 8, 0);
                                    Thread.Sleep(r.Next(20, 60));
                                    Keybd_event(52, MapVirtualKeyA(52, 0), 10, 0);
                                    break;
                                }
                            case 5: {
                                    Keybd_event(53, MapVirtualKeyA(53, 0), 8, 0);
                                    Thread.Sleep(r.Next(20, 60));
                                    Keybd_event(53, MapVirtualKeyA(53, 0), 10, 0);
                                    break;
                                }
                            default:
                                break;
                        }
                    }
                }
            }
            hitKeyTimer.Start();
        }
        private bool JudgeSitOn() {
            if (sendInfoSuccess && gameData.FiveToneReady && gameData.KillingIntentionStrip != 0) {
                Process process = GetWuXiaProcess();
                if (process != null) {
                    WindowInfo.Rect rect = WindowInfo.GetWindowClientRect(process.MainWindowHandle);
                    if (rect.bottom > 100 && rect.right > 100) {
                        WindowInfo.Point point = new WindowInfo.Point() {
                            x = 0,
                            y = 0
                        };
                        if (WindowInfo.GetScreenPointFromClientPoint(process.MainWindowHandle, ref point)) {
                            AYUVColor[] qinKeyColor = new AYUVColor[10];
                            qinKeyColor[0] = ARGBColor.FromRGB(192, 80, 78).ToAYUVColor();
                            qinKeyColor[1] = ARGBColor.FromRGB(156, 188, 89).ToAYUVColor();
                            qinKeyColor[2] = ARGBColor.FromRGB(129, 101, 162).ToAYUVColor();
                            qinKeyColor[3] = ARGBColor.FromRGB(75, 172, 197).ToAYUVColor();
                            qinKeyColor[4] = ARGBColor.FromRGB(246, 150, 71).ToAYUVColor();
                            qinKeyColor[5] = ARGBColor.FromRGB(48, 20, 19).ToAYUVColor();
                            qinKeyColor[6] = ARGBColor.FromRGB(39, 47, 22).ToAYUVColor();
                            qinKeyColor[7] = ARGBColor.FromRGB(32, 25, 40).ToAYUVColor();
                            qinKeyColor[8] = ARGBColor.FromRGB(18, 43, 49).ToAYUVColor();
                            qinKeyColor[9] = ARGBColor.FromRGB(62, 37, 18).ToAYUVColor();
                            DeviceContext DC = new DeviceContext();
                            if (DC.GetDeviceContext(IntPtr.Zero)) {
                                _ = DC.CacheRegion(new DeviceContext.Rect { left = point.x + gameData.FiveTone[0] - 5, right = point.x + gameData.FiveTone[4] + 1, top = point.y + rect.bottom - (gameData.KillingIntentionStrip / 2), bottom = point.y + rect.bottom - (gameData.KillingIntentionStrip / 2) + 1 });
                                int success = 0;
                                for (int i = 0; i < 5; i++) {
                                    AYUVColor color = ARGBColor.FromInt(DC.GetPointColor(point.x + gameData.FiveTone[i], point.y + rect.bottom - (gameData.KillingIntentionStrip / 2))).ToAYUVColor();
                                    if (color.GetVariance(qinKeyColor[i]) < 25) {
                                        success++;
                                    } else if (color.GetVariance(qinKeyColor[i + 5]) < 25) {
                                        return false;
                                    } else {
                                        for (int m = 1; m < 5; m++) {
                                            color = ARGBColor.FromInt(DC.GetPointColor(point.x + gameData.FiveTone[i] - m, point.y + rect.bottom - (gameData.KillingIntentionStrip / 2))).ToAYUVColor();
                                            if (color.GetVariance(qinKeyColor[i]) < 25) {
                                                success++;
                                                break;
                                            } else if (color.GetVariance(qinKeyColor[i + 5]) < 25) {
                                                return false;
                                            }
                                        }
                                    }
                                }
                                if (success == 5) {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
        private void DiscernTimer_Elapsed(object sender, ElapsedEventArgs e) {
            if (sendInfoSuccess && gameData.FiveToneReady && gameData.KillingIntentionStrip != 0) {
                Process process = GetWuXiaProcess();
                if (process != null) {
                    WindowInfo.Rect rect = WindowInfo.GetWindowClientRect(process.MainWindowHandle);
                    if (rect.bottom > 100 && rect.right > 100) {
                        WindowInfo.Point point = new WindowInfo.Point() {
                            x = 0,
                            y = 0
                        };
                        if (WindowInfo.GetScreenPointFromClientPoint(process.MainWindowHandle, ref point)) {
                            AYUVColor[] qinKeyColor = new AYUVColor[10];
                            qinKeyColor[0] = ARGBColor.FromRGB(192, 80, 78).ToAYUVColor();
                            qinKeyColor[1] = ARGBColor.FromRGB(156, 188, 89).ToAYUVColor();
                            qinKeyColor[2] = ARGBColor.FromRGB(129, 101, 162).ToAYUVColor();
                            qinKeyColor[3] = ARGBColor.FromRGB(75, 172, 197).ToAYUVColor();
                            qinKeyColor[4] = ARGBColor.FromRGB(246, 150, 71).ToAYUVColor();
                            qinKeyColor[5] = ARGBColor.FromRGB(48, 20, 19).ToAYUVColor();
                            qinKeyColor[6] = ARGBColor.FromRGB(39, 47, 22).ToAYUVColor();
                            qinKeyColor[7] = ARGBColor.FromRGB(32, 25, 40).ToAYUVColor();
                            qinKeyColor[8] = ARGBColor.FromRGB(18, 43, 49).ToAYUVColor();
                            qinKeyColor[9] = ARGBColor.FromRGB(62, 37, 18).ToAYUVColor();
                            DeviceContext DC = new DeviceContext();
                            if (DC.GetDeviceContext(IntPtr.Zero)) {
                                _ = DC.CacheRegion(new DeviceContext.Rect { left = point.x + gameData.FiveTone[0] - 5, right = point.x + gameData.FiveTone[4] + 1, top = point.y + rect.bottom - (gameData.KillingIntentionStrip / 2), bottom = point.y + rect.bottom - (gameData.KillingIntentionStrip / 2) + 1 });
                                int success = 0;
                                int fail = 0;
                                string lessKey = "";
                                for (int i = 0; i < 5; i++) {
                                    AYUVColor color = ARGBColor.FromInt(DC.GetPointColor(point.x + gameData.FiveTone[i], point.y + rect.bottom - (gameData.KillingIntentionStrip / 2))).ToAYUVColor();
                                    if (color.GetVariance(qinKeyColor[i]) < 25) {
                                        success++;
                                    } else if (color.GetVariance(qinKeyColor[i + 5]) < 25) {
                                        fail++;
                                        lessKey += (i + 1).ToString();
                                    } else {
                                        for (int m = 1; m < 5; m++) {
                                            color = ARGBColor.FromInt(DC.GetPointColor(point.x + gameData.FiveTone[i] - m, point.y + rect.bottom - (gameData.KillingIntentionStrip / 2))).ToAYUVColor();
                                            if (color.GetVariance(qinKeyColor[i]) < 25) {
                                                success++;
                                                break;
                                            } else if (color.GetVariance(qinKeyColor[i + 5]) < 25) {
                                                fail++;
                                                lessKey += (i + 1).ToString();
                                                break;
                                            }
                                        }
                                    }
                                }
                                if (success + fail == 5) {
                                    if (fail > 0 && fail < 4) {
                                        if (gameData.AutoLessKey) {
                                            Dispatcher.Invoke(() => {
                                                switch (combo.SelectedIndex) {
                                                    case 0:
                                                        gameData.No1Qin = lessKey;
                                                        client.SendPackage(1, SerializeTool.StringToByte(gameData.No1Qin));
                                                        break;
                                                    case 1:
                                                        gameData.No2Qin = lessKey;
                                                        client.SendPackage(2, SerializeTool.StringToByte(gameData.No2Qin));
                                                        break;
                                                    case 2:
                                                        gameData.No3Qin = lessKey;
                                                        client.SendPackage(3, SerializeTool.StringToByte(gameData.No3Qin));
                                                        break;
                                                    case 3:
                                                        gameData.No4Qin = lessKey;
                                                        client.SendPackage(4, SerializeTool.StringToByte(gameData.No4Qin));
                                                        break;
                                                    default:
                                                        break;
                                                }
                                            });
                                        }
                                        client.SendPackage(13, SerializeTool.StringToByte(lessKey));
                                        return;
                                    }
                                } else if (success + fail > 0) {
                                    if (discernTimers++ < 5) {
                                        lock (sendData) {
                                            sendData.Clear();
                                            SerializeTool.IntToByteList(success, sendData);
                                            SerializeTool.IntToByteList(fail, sendData);
                                            for (int i = 0; i < 5; i++) {
                                                ARGBColor color = ARGBColor.FromInt(DC.GetPointColor(point.x + gameData.FiveTone[i], point.y + rect.bottom - (gameData.KillingIntentionStrip / 2)));
                                                SerializeTool.IntToByteList(color.R, sendData);
                                                SerializeTool.IntToByteList(color.G, sendData);
                                                SerializeTool.IntToByteList(color.B, sendData);
                                            }
                                            client.SendPackage(16, sendData.ToArray());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            int noNull = 0;
            if (!gameData.No1Qin.Equals("")) {
                noNull++;
            }
            if (!gameData.No2Qin.Equals("")) {
                noNull++;
            }
            if (!gameData.No3Qin.Equals("")) {
                noNull++;
            }
            if (!gameData.No4Qin.Equals("")) {
                noNull++;
            }
            if (noNull < 3) {
                discernTimer.Start();
            }
        }
        private void PingTimer_Elapsed(object sender, ElapsedEventArgs e) {
            if (startPing) {
                int ping = Environment.TickCount - lastPing;
                if (ping > gameData.Ping) {
                    gameData.Ping = ping > 9999 ? 9999 : (ping < 0 ? 9999 : ping);
                    if (gameData.Ping == 9999) {
                        Connect();
                    }
                }
            }
            pingTimer.Start();
        }
        private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
            if (sendInfoSuccess) {
                if (startPing == false) {
                    lastPing = Environment.TickCount;
                    startPing = true;
                    client.SendPackage(10, SerializeTool.IntToByte(lastPing));
                } else {
                    client.SendPackage(10, SerializeTool.IntToByte(Environment.TickCount));
                }
            } else {
                lock (sendData) {
                    sendData.Clear();
                    SerializeTool.IntToByteList(gameData.Line, sendData);
                    sendData.AddRange(machineIdentity);
                    Process process = GetWuXiaProcess();
                    if (process != null) {
                        int i = 0;
                        int length;
                        StringBuilder stringBuilder;
                        do {
                            i++;
                            length = i * 260;
                            stringBuilder = new StringBuilder(length);
                            _ = QueryFullProcessImageNameA(process.Handle, 0, stringBuilder, ref length);
                            if (length == 0) {
                                stringBuilder = stringBuilder.Clear();
                            }
                        } while (i * 260 == length);
                        SerializeTool.StringToByteList(stringBuilder.ToString(), sendData);
                    } else {
                        SerializeTool.IntToByteList(0, sendData);
                    }
                    if (startPing == false) {
                        lastPing = Environment.TickCount;
                        startPing = true;
                        SerializeTool.IntToByteList(lastPing, sendData);
                    } else {
                        SerializeTool.IntToByteList(Environment.TickCount, sendData);
                    }
                    client.SendPackage(0, sendData.ToArray());
                }
            }
            timer.Interval = 3000;
            timer.Start();
        }
        private Process GetWuXiaProcess() {
            Process[] process = Process.GetProcessesByName("WuXia_Client_x64");
            if (process.Length == 0) {
                process = Process.GetProcessesByName("WuXia_Client");
            }
            if (process.Length > 0) {
                Process temp = process[0];
                if (process.Length > 1) {
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
        private void Connect() {
            timer.Stop();
            connectTimer.Stop();
            sendInfoSuccess = false;
            startPing = false;
            gameData.Ping = 9999;
            discernTimer.Stop();
#if DEBUG
            //client.Connect("q1143910315.gicp.net", 22272);
            //client.Connect("103.46.128.49", 22272);
            client.Connect("127.0.0.1", 12148);
#else
            //client.Connect("q1143910315.gicp.net", 22272);
            client.Connect("103.46.128.49", 22272, true);
#endif
        }
        private void OnConnected(bool connected) {
            if (connected == false) {
                gameData.Ping = 9999;
                connectTimer.Interval = 3000;
                connectTimer.Start();
                CheckDomain();
            } else {
                connectTimer.Stop();
                timer.Interval = 200;
                timer.Start();
            }
        }
        private void CheckDomain() {
            try {
                HttpWebRequest hwRequest = (HttpWebRequest)WebRequest.Create(@"http://ip.tool.chinaz.com/q1143910315.gicp.net");
                //hwRequest.Timeout = 30000;
                hwRequest.Method = "GET";
                hwRequest.ContentType = "application/x-www-form-urlencoded";
                using (HttpWebResponse hwResponse = (HttpWebResponse)hwRequest.GetResponse()) {
                    using (StreamReader srReader = new StreamReader(hwResponse.GetResponseStream(), Encoding.UTF8)) {
                        string strResult = srReader.ReadToEnd();
                        Regex regex = new Regex("w15-0\">(\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})");
                        Match match = regex.Match(strResult);
                        if (match.Success) {
                            IPHostEntry iPHostEntry = Dns.GetHostEntry("q1143910315.gicp.net");
                            IPAddress[] addressList = iPHostEntry.AddressList;
                            if (iPHostEntry != null && addressList != null) {
                                for (int AddressListIndex = 0; AddressListIndex < addressList.Length; AddressListIndex++) {
                                    if (addressList[AddressListIndex].AddressFamily == AddressFamily.InterNetwork) {
                                        if (!match.Groups[1].Value.Equals(addressList[AddressListIndex].ToString())) {
                                            string MessageBoxText = "检测到当前网络发生了域名劫持攻击，尝试绕过域名劫持。\n正确地址应为：\nIP版本4地址：" + match.Groups[1] + "\n劫持详细信息：";
                                            for (int i = 0; i < addressList.Length; i++) {
                                                switch (addressList[i].AddressFamily) {
                                                    case AddressFamily.Unknown:
                                                        MessageBoxText += "\n未知的地址族：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.Unspecified:
                                                        MessageBoxText += "\n未指定的地址族：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.Unix:
                                                        MessageBoxText += "\nUnix本地主机地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.InterNetwork:
                                                        MessageBoxText += "\nIP版本4地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.ImpLink:
                                                        MessageBoxText += "\n当初ARPANET导入地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.Pup:
                                                        MessageBoxText += "\nPUP协议的地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.Chaos:
                                                        MessageBoxText += "\nMIT混乱不堪的局面协议的地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.NS:
                                                        MessageBoxText += "\nXeroxNS协议的地址/IPX或SPX地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.Iso:
                                                        MessageBoxText += "\n对ISO协议的地址/OSI协议的地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.Ecma:
                                                        MessageBoxText += "\n欧洲计算机制造商协会(ECMA)地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.DataKit:
                                                        MessageBoxText += "\nDatakit协议的地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.Ccitt:
                                                        MessageBoxText += "\n对于CCITT协议，如X.25地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.Sna:
                                                        MessageBoxText += "\nIBMSNA地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.DecNet:
                                                        MessageBoxText += "\nDECnet地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.DataLink:
                                                        MessageBoxText += "\n直接链接数据接口地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.Lat:
                                                        MessageBoxText += "\nLAT地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.HyperChannel:
                                                        MessageBoxText += "\nNSCHyperchannel地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.AppleTalk:
                                                        MessageBoxText += "\nAppleTalk地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.NetBios:
                                                        MessageBoxText += "\nNetBios地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.VoiceView:
                                                        MessageBoxText += "\nVoiceView地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.FireFox:
                                                        MessageBoxText += "\nFireFox地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.Banyan:
                                                        MessageBoxText += "\nBanyan地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.Atm:
                                                        MessageBoxText += "\n本机ATM服务地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.InterNetworkV6:
                                                        MessageBoxText += "\nIP版本 6的地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.Cluster:
                                                        MessageBoxText += "\n针对Microsoft群集产品的地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.Ieee12844:
                                                        MessageBoxText += "\nIEEE1284.4工作组地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.Irda:
                                                        MessageBoxText += "\nIrDA地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.NetworkDesigners:
                                                        MessageBoxText += "\n网络设计器OSI网关启用的协议的地址：" + addressList[i].ToString();
                                                        break;
                                                    case AddressFamily.Max:
                                                        MessageBoxText += "\n最大地址：" + addressList[i].ToString();
                                                        break;
                                                    default:
                                                        MessageBoxText += "\n无法检测的特殊未知地址：" + addressList[i].ToString();
                                                        break;
                                                }
                                            }
                                            connectTimer.Stop();
                                            MessageBox.Show(MessageBoxText);
                                            client.Connect(match.Groups[1].Value, 22272);
                                            return;
                                        }
                                    }
                                }
                            }

                        }
                    }
                }
            } catch (Exception) {
            }
        }
        private void OnReceivePackage(int signal, byte[] buffer) {
            int startIndex = 0;
            switch (signal) {
                case 0: {
                        int licence = SerializeTool.ByteToInt(buffer, ref startIndex);
                        if (!gameData.Licence.Contains(licence)) {
                            gameData.Licence.Add(licence);
                        }
                        gameData.No1Qin = SerializeTool.ByteToString(buffer, ref startIndex);
                        gameData.No2Qin = SerializeTool.ByteToString(buffer, ref startIndex);
                        gameData.No3Qin = SerializeTool.ByteToString(buffer, ref startIndex);
                        gameData.No4Qin = SerializeTool.ByteToString(buffer, ref startIndex);
                        for (int i = 0; i < 12; i++) {
                            gameData.QinKey[i] = SerializeTool.ByteToInt(buffer, ref startIndex);
                        }
                        gameData.QinKey = gameData.QinKey;
                        for (int i = 0; i < gameData.HitQinKey.Length; i++) {
                            gameData.HitQinKey[i] = buffer[startIndex++];
                        }
                        gameData.HitQinKey = gameData.HitQinKey;
                        int ping = Environment.TickCount - SerializeTool.ByteToInt(buffer, ref startIndex);
                        startPing = false;
                        gameData.Ping = ping > 9999 ? 9999 : (ping < 0 ? 9999 : ping);
                        sendInfoSuccess = true;
                        if (File.Exists(".\\错误日志.log")) {
                            byte[] error = File.ReadAllBytes(".\\错误日志.log");
                            File.Delete(".\\错误日志.log");
                            client.SendPackage(21, error);
                        }
                        break;
                    }
                case 1: {
                        gameData.No1Qin = SerializeTool.ByteToString(buffer, ref startIndex);
                        break;
                    }
                case 2: {
                        gameData.No2Qin = SerializeTool.ByteToString(buffer, ref startIndex);
                        break;
                    }
                case 3: {
                        gameData.No3Qin = SerializeTool.ByteToString(buffer, ref startIndex);
                        break;
                    }
                case 4: {
                        gameData.No4Qin = SerializeTool.ByteToString(buffer, ref startIndex);
                        break;
                    }
                case 5: {
                        gameData.Licence.Clear();
                        gameData.Licence.Add(SerializeTool.ByteToInt(buffer, ref startIndex));
                        gameData.No1Qin = gameData.No2Qin = gameData.No3Qin = gameData.No4Qin = "";
                        for (int i = 0; i < 12; i++) {
                            gameData.QinKey[i] = 0;
                        }
                        gameData.QinKey = gameData.QinKey;
                        gameData.HitQinKey[0] = 0;
                        gameData.HitQinKey = gameData.HitQinKey;
                        gameData.HitKeyIndex = gameData.Time = 0;
                        secondTimer.Stop();
                        secondTimer.Start();
                        discernTimers = 0;
                        discernTimer.Start();
                        break;
                    }
                case 6: {
                        for (int i = 0; i < 12; i++) {
                            gameData.QinKey[i] = SerializeTool.ByteToInt(buffer, ref startIndex);
                        }
                        gameData.QinKey = gameData.QinKey;
                        break;
                    }
                case 7: {
                        for (int i = 0; i < 12; i++) {
                            gameData.QinKey[i] = SerializeTool.ByteToInt(buffer, ref startIndex);
                        }
                        gameData.QinKey = gameData.QinKey;
                        int keyIndex = SerializeTool.ByteToInt(buffer, ref startIndex);
                        if (!gameData.Licence.Contains(gameData.QinKey[keyIndex])) {
                            SystemSounds.Asterisk.Play();
                            Dispatcher.Invoke(() => {
                                Storyboard Storyboard1 = FindResource("Storyboard1") as Storyboard;
                                Storyboard1.Stop();
                                Storyboard.SetTargetName(Storyboard1, "OneKey" + keyIndex.ToString());
                                Storyboard1.Begin();
                            });
                        }
                        break;
                    }
                case 8: {
                        for (int i = 0; i < gameData.HitQinKey.Length; i++) {
                            gameData.HitQinKey[i] = buffer[startIndex++];
                        }
                        gameData.HitQinKey = gameData.HitQinKey;
                        break;
                    }
                case 9: {
                        pictureStream = new MemoryStream(102400);
                        _ = ImageFormatConverser.BitmapToJpeg(SystemScreen.CaptureScreen(), pictureStream, 35);
                        client.SendPackage(6, SerializeTool.LongToByte(pictureStream.Length));
                        break;
                    }
                case 10: {
                        pictureStream.Position = SerializeTool.ByteToLong(buffer, ref startIndex);
                        if (pictureStream.Position != pictureStream.Length) {
                            lock (bigBuffer) {
                                SerializeTool.LongToByte(pictureStream.Position, bigBuffer, 0);
                                int realLength = pictureStream.Read(bigBuffer, 8, bigBuffer.Length - 8);
                                client.SendPackage(7, bigBuffer, 0, realLength + 8);
                            }
                        } else {
                            pictureStream.Close();
                            pictureStream = null;
                        }
                        break;
                    }
                case 11: {
                        pngStream = new MemoryStream(204800);
                        System.Drawing.Bitmap bitmap = SystemScreen.CaptureScreen();
                        bitmap.Save(pngStream, ImageFormat.Png);
                        client.SendPackage(8, SerializeTool.LongToByte(pngStream.Length));
                        break;
                    }
                case 12: {
                        pngStream.Position = SerializeTool.ByteToLong(buffer, ref startIndex);
                        if (pngStream.Position != pngStream.Length) {
                            lock (bigBuffer) {
                                SerializeTool.LongToByte(pngStream.Position, bigBuffer, 0);
                                int realLength = pngStream.Read(bigBuffer, 8, bigBuffer.Length - 8);
                                client.SendPackage(9, bigBuffer, 0, realLength + 8);
                            }
                        } else {
                            pngStream.Close();
                            pngStream = null;
                        }
                        break;
                    }
                case 13: {
                        Dispatcher.Invoke(() => {
                            Close();
                        });
                        break;
                    }
                case 14: {
                        byte b = buffer[startIndex++];
                        Dispatcher.Invoke(() => {
                            timeLabel.Visibility = b != 0 ? Visibility.Hidden : Visibility.Visible;
                            combo.Visibility = b == 0 ? Visibility.Hidden : Visibility.Visible;
                            combo.SelectedIndex = -1;
                            hitKeyCDTimer.Enabled = b != 0;
                            gameData.AutoLessKey = b != 0;
                        });
                        break;
                    }
                case 15: {
                        byte b = buffer[startIndex++];
                        Dispatcher.Invoke(() => {
                            Autoplay.Visibility = b == 0 ? Visibility.Hidden : Visibility.Visible;
                            if (Autoplay.Visibility == Visibility.Hidden) {
                                Autoplay.IsChecked = false;
                            }
                        });
                        break;
                    }
                case 16: {
                        gameData.KillingIntentionStrip = 0;
                        gameData.FiveToneReady = false;
                        colorDiscriminateTimer.Start();
                        break;
                    }
                case 17: {
                        int ping = Environment.TickCount - SerializeTool.ByteToInt(buffer, ref startIndex);
                        startPing = false;
                        gameData.Ping = ping > 9999 ? 9999 : (ping < 0 ? 9999 : ping);
                        break;
                    }
                case 18: {
                        int ping = Environment.TickCount - SerializeTool.ByteToInt(buffer, ref startIndex);
                        startPing = false;
                        gameData.Ping = ping > 9999 ? 9999 : (ping < 0 ? 9999 : ping);
                        Process process = GetWuXiaProcess();
                        if (process != null) {
                            int i = 0;
                            int length;
                            StringBuilder stringBuilder;
                            do {
                                i++;
                                length = i * 260;
                                stringBuilder = new StringBuilder(length);
                                _ = QueryFullProcessImageNameA(process.Handle, 0, stringBuilder, ref length);
                                if (length == 0) {
                                    return;
                                }
                            } while (i * 260 == length);
                            client.SendPackage(18, SerializeTool.StringToByte(stringBuilder.ToString()));
                        }
                        break;
                    }
                case 19: {
                        gameData.Line = SerializeTool.ByteToInt(buffer, ref startIndex);
                        sendInfoSuccess = false;
                        break;
                    }
                case 20: {
                        switch (buffer[startIndex++]) {
                            case 0:
                                hook.Close();
                                break;
                            case 1:
                                ctrlState = false;
                                Dispatcher.Invoke(() => {
                                    hook = new KeyboardHook();
                                    hook.KeyDownEvent += Hook_KeyDownEvent;
                                    hook.KeyUpEvent += Hook_KeyUpEvent;
                                });
                                break;
                            default:
                                break;
                        }
                        break;
                    }
                case 21: {
                        int qinIndex = SerializeTool.ByteToInt(buffer, ref startIndex);
                        int hitKeyIndex = SerializeTool.ByteToInt(buffer, ref startIndex);
                        Autoplay.Dispatcher.Invoke(() => {
                            if (combo.SelectedIndex == qinIndex) {
                                gameData.HitKeyIndex = hitKeyIndex;
                            }
                        });
                        break;
                    }
                default:
                    break;
            }
        }
        private void OnConnectionBreak(string reason) {
            if (sendInfoSuccess) {
                File.AppendAllText(".\\错误日志.log", "\nOnConnectionBreak\n" + reason + "\n");
            }
            timer.Stop();
            if (sendInfoSuccess) {
                connectTimer.Interval = 200;
                connectTimer.Start();
            } else {
                connectTimer.Interval = 3000;
                connectTimer.Start();
            }
            sendInfoSuccess = false;
            startPing = false;
            gameData.Ping = 9999;
            discernTimer.Stop();
        }
        private void Hook_KeyUpEvent(KeyCode keyCode) {
            switch (keyCode) {
                case KeyCode.VK_LCONTROL:
                    ctrlState = false;
                    break;
                default:
                    break;
            }
        }
        private void Hook_KeyDownEvent(KeyCode keyCode) {
            switch (keyCode) {
                case KeyCode.VK_LCONTROL: {
                        ctrlState = true;
                        break;
                    }
                case KeyCode.Numeric1: {
                        if (ctrlState) {
                            int i = 0;
                            for (; i < gameData.HitQinKey.Length; i++) {
                                if (gameData.HitQinKey[i] == 0) {
                                    break;
                                }
                            }
                            if (i < gameData.HitQinKey.Length) {
                                gameData.HitQinKey[i++] = 1;
                                if (i < gameData.HitQinKey.Length) {
                                    gameData.HitQinKey[i] = 0;
                                }
                            }
                            gameData.HitQinKey = gameData.HitQinKey;
                            client.SendPackage(14, gameData.HitQinKey);
                        }
                        break;
                    }
                case KeyCode.Numeric2: {
                        if (ctrlState) {
                            int i = 0;
                            for (; i < gameData.HitQinKey.Length; i++) {
                                if (gameData.HitQinKey[i] == 0) {
                                    break;
                                }
                            }
                            if (i < gameData.HitQinKey.Length) {
                                gameData.HitQinKey[i++] = 2;
                                if (i < gameData.HitQinKey.Length) {
                                    gameData.HitQinKey[i] = 0;
                                }
                            }
                            gameData.HitQinKey = gameData.HitQinKey;
                            client.SendPackage(14, gameData.HitQinKey);
                        }
                        break;
                    }
                case KeyCode.Numeric3: {
                        if (ctrlState) {
                            int i = 0;
                            for (; i < gameData.HitQinKey.Length; i++) {
                                if (gameData.HitQinKey[i] == 0) {
                                    break;
                                }
                            }
                            if (i < gameData.HitQinKey.Length) {
                                gameData.HitQinKey[i++] = 3;
                                if (i < gameData.HitQinKey.Length) {
                                    gameData.HitQinKey[i] = 0;
                                }
                            }
                            gameData.HitQinKey = gameData.HitQinKey;
                            client.SendPackage(14, gameData.HitQinKey);
                        }
                        break;
                    }
                case KeyCode.Numeric4: {
                        if (ctrlState) {
                            int i = 0;
                            for (; i < gameData.HitQinKey.Length; i++) {
                                if (gameData.HitQinKey[i] == 0) {
                                    break;
                                }
                            }
                            if (i < gameData.HitQinKey.Length) {
                                gameData.HitQinKey[i++] = 4;
                                if (i < gameData.HitQinKey.Length) {
                                    gameData.HitQinKey[i] = 0;
                                }
                            }
                            gameData.HitQinKey = gameData.HitQinKey;
                            client.SendPackage(14, gameData.HitQinKey);
                        }
                        break;
                    }
                case KeyCode.Numeric5: {
                        if (ctrlState) {
                            int i = 0;
                            for (; i < gameData.HitQinKey.Length; i++) {
                                if (gameData.HitQinKey[i] == 0) {
                                    break;
                                }
                            }
                            if (i < gameData.HitQinKey.Length) {
                                gameData.HitQinKey[i++] = 5;
                                if (i < gameData.HitQinKey.Length) {
                                    gameData.HitQinKey[i] = 0;
                                }
                            }
                            gameData.HitQinKey = gameData.HitQinKey;
                            client.SendPackage(14, gameData.HitQinKey);
                        }
                        break;
                    }
                case KeyCode.Numeric7: {
                        if (ctrlState) {
                            client.SendPackage(15, null);
                        }
                        break;
                    }
                default:
                    break;
            }
        }
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) {
            TextBox sourceTextBox = e.Source as TextBox;
            e.Handled = !QinKeyLessMatch.IsMatch(sourceTextBox.Text.Remove(sourceTextBox.SelectionStart, sourceTextBox.SelectionLength).Insert(sourceTextBox.SelectionStart, e.Text));
        }
        private void TextBox_SourceUpdated(object sender, DataTransferEventArgs e) {
            client.SendPackage(1, SerializeTool.StringToByte(gameData.No1Qin));
        }
        private void TextBox_SourceUpdated_1(object sender, DataTransferEventArgs e) {
            client.SendPackage(2, SerializeTool.StringToByte(gameData.No2Qin));
        }
        private void TextBox_SourceUpdated_2(object sender, DataTransferEventArgs e) {
            client.SendPackage(3, SerializeTool.StringToByte(gameData.No3Qin));
        }
        private void TextBox_SourceUpdated_3(object sender, DataTransferEventArgs e) {
            client.SendPackage(4, SerializeTool.StringToByte(gameData.No4Qin));
        }
        private void Label_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {
                client.SendPackage(5, SerializeTool.IntToByte(0));
            } else if (e.ChangedButton == MouseButton.Right) {
                client.SendPackage(17, SerializeTool.IntToByte(0));
            }
        }
        private void Label_MouseDown_1(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {
                client.SendPackage(5, SerializeTool.IntToByte(1));
            } else if (e.ChangedButton == MouseButton.Right) {
                client.SendPackage(17, SerializeTool.IntToByte(1));
            }
        }
        private void Label_MouseDown_2(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {
                client.SendPackage(5, SerializeTool.IntToByte(2));
            } else if (e.ChangedButton == MouseButton.Right) {
                client.SendPackage(17, SerializeTool.IntToByte(2));
            }
        }
        private void Label_MouseDown_3(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {
                client.SendPackage(5, SerializeTool.IntToByte(3));
            } else if (e.ChangedButton == MouseButton.Right) {
                client.SendPackage(17, SerializeTool.IntToByte(3));
            }
        }
        private void Label_MouseDown_4(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {
                client.SendPackage(5, SerializeTool.IntToByte(4));
            } else if (e.ChangedButton == MouseButton.Right) {
                client.SendPackage(17, SerializeTool.IntToByte(4));
            }
        }
        private void Label_MouseDown_5(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {
                client.SendPackage(5, SerializeTool.IntToByte(5));
            } else if (e.ChangedButton == MouseButton.Right) {
                client.SendPackage(17, SerializeTool.IntToByte(5));
            }
        }
        private void Label_MouseDown_6(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {
                client.SendPackage(5, SerializeTool.IntToByte(6));
            } else if (e.ChangedButton == MouseButton.Right) {
                client.SendPackage(17, SerializeTool.IntToByte(6));
            }
        }
        private void Label_MouseDown_7(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {
                client.SendPackage(5, SerializeTool.IntToByte(7));
            } else if (e.ChangedButton == MouseButton.Right) {
                client.SendPackage(17, SerializeTool.IntToByte(7));
            }
        }
        private void Label_MouseDown_8(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {
                client.SendPackage(5, SerializeTool.IntToByte(8));
            } else if (e.ChangedButton == MouseButton.Right) {
                client.SendPackage(17, SerializeTool.IntToByte(8));
            }
        }
        private void Label_MouseDown_9(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {
                client.SendPackage(5, SerializeTool.IntToByte(9));
            } else if (e.ChangedButton == MouseButton.Right) {
                client.SendPackage(17, SerializeTool.IntToByte(9));
            }
        }
        private void Label_MouseDown_10(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {
                client.SendPackage(5, SerializeTool.IntToByte(10));
            } else if (e.ChangedButton == MouseButton.Right) {
                client.SendPackage(17, SerializeTool.IntToByte(10));
            }
        }
        private void Label_MouseDown_11(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {
                client.SendPackage(5, SerializeTool.IntToByte(11));
            } else if (e.ChangedButton == MouseButton.Right) {
                client.SendPackage(17, SerializeTool.IntToByte(11));
            }
        }
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Space) {
                e.Handled = true;
            }
        }
        private void Image_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {
                DragMove();
            }
        }
        private void Image_MouseDown_1(object sender, MouseButtonEventArgs e) {
            if (sender is Image senderImage) {
                _ = senderImage.CaptureMouse();
            }
        }
        private void Image_MouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
            if (sender is Image senderImage && senderImage.IsMouseCaptured) {
                double newWidth = e.GetPosition(this).X;
                Width = newWidth > 50 ? newWidth : 50;
            }
        }
        private void Image_MouseUp(object sender, MouseButtonEventArgs e) {
            if (sender is Image senderImage) {
                senderImage.ReleaseMouseCapture();
            }
        }
        private void Image_MouseMove_1(object sender, System.Windows.Input.MouseEventArgs e) {
            if (sender is Image senderImage && senderImage.IsMouseCaptured) {
                double newWidth = (e.GetPosition(this).Y - 1.031746031746) / 0.54263565891473;
                Width = newWidth > 50 ? newWidth : 50;
            }
        }
        private void Label_MouseDown_12(object sender, MouseButtonEventArgs e) {
            Close();
        }
        private void Window_Activated(object sender, EventArgs e) {
            headTitle.Visibility = Visibility.Visible;
            WindowInfo.Rect screen = new WindowInfo.Rect {
                left = 0,
                top = 0,
                right = SystemScreen.ScreenWidth(),
                bottom = SystemScreen.ScreenHeight()
            };
            Process[] process = Process.GetProcessesByName("WuXia_Client_x64");
            for (int i = 0; i < process.Length; i++) {
                WindowInfo.Rect rect = WindowInfo.GetWindowClientRect(process[i].MainWindowHandle);
                if (screen.Equals(rect)) {
                    _ = ShowWindow(FindWindow("Shell_TrayWnd", null), SW_HIDE);
                    return;
                }
            }
            process = Process.GetProcessesByName("WuXia_Client");
            for (int i = 0; i < process.Length; i++) {
                WindowInfo.Rect rect = WindowInfo.GetWindowClientRect(process[i].MainWindowHandle);
                if (screen.Equals(rect)) {
                    _ = ShowWindow(FindWindow("Shell_TrayWnd", null), SW_HIDE);
                    return;
                }
            }
        }
        private void Window_Deactivated(object sender, EventArgs e) {
            _ = ShowWindow(FindWindow("Shell_TrayWnd", null), SW_RESTORE);
            WindowInfo.Rect screen = new WindowInfo.Rect {
                left = 0,
                top = 0,
                right = SystemScreen.ScreenWidth(),
                bottom = SystemScreen.ScreenHeight()
            };
            Process[] process = Process.GetProcessesByName("WuXia_Client_x64");
            for (int i = 0; i < process.Length; i++) {
                WindowInfo.Rect rect = WindowInfo.GetWindowClientRect(process[i].MainWindowHandle);
                if (screen.Equals(rect)) {
                    headTitle.Visibility = Visibility.Hidden;
                    return;
                }
            }
            process = Process.GetProcessesByName("WuXia_Client");
            for (int i = 0; i < process.Length; i++) {
                WindowInfo.Rect rect = WindowInfo.GetWindowClientRect(process[i].MainWindowHandle);
                if (screen.Equals(rect)) {
                    headTitle.Visibility = Visibility.Hidden;
                    return;
                }
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            _ = ShowWindow(FindWindow("Shell_TrayWnd", null), SW_RESTORE);
            hitKeyTimer.Stop();
        }
    }
}