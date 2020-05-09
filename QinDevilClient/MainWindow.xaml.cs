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
#if service
using QinDevilCommon.Keyboard;
#endif

namespace QinDevilClient {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        [DllImport("Psapi.dll", EntryPoint = "GetModuleFileNameEx")]
        public static extern int GetModuleFileNameEx(IntPtr handle, IntPtr hModule, [Out] StringBuilder lpszFileName, int nSize);
        [DllImport("Kernel32.dll", EntryPoint = "QueryFullProcessImageNameA")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryFullProcessImageNameA(IntPtr hProcess, int dwFlags, [Out] StringBuilder lpExeName, ref int lpdwSize);
        [DllImport("User32.dll", EntryPoint = "keybd_event")]
        public static extern void Keybd_event(int bVk, int bScan, int dwFlags, int dwExtraInfo);
        [DllImport("User32.dll", EntryPoint = "MapVirtualKeyA")]
        public static extern int MapVirtualKeyA(int uCode, int uMapType);
        private SocketClient client;
        private readonly Timer timer = new Timer();
        private readonly Timer pingTimer = new Timer();
        private readonly Timer discernTimer = new Timer();
        private readonly Timer hitKeyTimer = new Timer();
        private readonly Timer colorDiscriminateTimer = new Timer();
        private readonly Timer secondTimer = new Timer();
        private bool Connecting = false;
        private readonly GameData gameData = new GameData();
        private readonly Regex QinKeyLessMatch = new Regex("^(?![1-5]*?([1-5])[1-5]*?\\1)[1-5]{0,3}$");
        private MemoryStream pictureStream = null;
        private MemoryStream pngStream = null;
        private readonly byte[] bigBuffer = new byte[8000];
        private bool startPing = false;
        private bool sitOn = false;
        private int lastPing;
        private readonly string macAndCpu = SystemInfo.GetMacAddress() + SystemInfo.GetCpuID();
        private bool sendInfoSuccess = false;
        private readonly Random r = new Random();
        private int discernTimers = 0;
        private readonly LogManage log = new LogManage(".\\工具日志（可删除）.log");
#if service
        private readonly KeyboardHook hook = new KeyboardHook();
        private bool ctrlState;
#endif
        public MainWindow() {
            try {
                log.Generate("1 进入");
                InitializeComponent();
            } catch (Exception e1) {
                log.Generate("1 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("1 退出");
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            try {
                log.Generate("2 进入");
                GamePanel.DataContext = gameData;
                client = new SocketClient();
                client.onConnectedEvent += OnConnected;
                client.onReceivePackageEvent += OnReceivePackage;
                client.onConnectionBreakEvent += OnConnectionBreak;
                timer.Elapsed += Timer_Elapsed;
                timer.AutoReset = false;
                timer.Stop();
                pingTimer.Interval = 150;
                pingTimer.Elapsed += PingTimer_Elapsed;
                pingTimer.AutoReset = true;
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
#if service
                hook.KeyDownEvent += KeyDownCallbak;
                hook.KeyUpEvent += KeyUpCallbak;
#endif
                Connect();
            } catch (Exception e1) {
                log.Generate("2 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("2 退出");
            }
        }
#if service
        private void KeyDownCallbak(KeyCode keyCode) {
            try {
                log.Generate("3 进入");
                switch (keyCode) {
                    case KeyCode.VK_LCONTROL: {
                            ctrlState = true;
                            break;
                        }
                    case KeyCode.Numeric1: {
                            if (ctrlState) {
                                _ = gameData.HitQinKey.Append("1 ");
                                client.SendPackage(14, SerializeTool.RawSerializeForUTF8String(gameData.HitQinKeyAny.ToString()));
                            }
                            break;
                        }
                    case KeyCode.Numeric2: {
                            if (ctrlState) {
                                _ = gameData.HitQinKey.Append("2 ");
                                client.SendPackage(14, SerializeTool.RawSerializeForUTF8String(gameData.HitQinKeyAny.ToString()));
                            }
                            break;
                        }
                    case KeyCode.Numeric3: {
                            if (ctrlState) {
                                _ = gameData.HitQinKey.Append("3 ");
                                client.SendPackage(14, SerializeTool.RawSerializeForUTF8String(gameData.HitQinKeyAny.ToString()));
                            }
                            break;
                        }
                    case KeyCode.Numeric4: {
                            if (ctrlState) {
                                _ = gameData.HitQinKey.Append("4 ");
                                client.SendPackage(14, SerializeTool.RawSerializeForUTF8String(gameData.HitQinKeyAny.ToString()));
                            }
                            break;
                        }
                    case KeyCode.Numeric5: {
                            if (ctrlState) {
                                _ = gameData.HitQinKey.Append("5 ");
                                client.SendPackage(14, SerializeTool.RawSerializeForUTF8String(gameData.HitQinKeyAny.ToString()));
                            }
                            break;
                        }
                    case KeyCode.Numeric7: {
                            if (ctrlState) {
                                _ = gameData.HitQinKeyAny.Clear();
                                client.SendPackage(15, null);
                            }
                            break;
                        }
                    default:
                        break;
                }
            } catch (Exception e1) {
                log.Generate("3 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("3 退出");
            }
        }
        private void KeyUpCallbak(KeyCode keyCode) {
            try {
                log.Generate("4 进入");
                switch (keyCode) {
                    case KeyCode.VK_LCONTROL:
                        ctrlState = false;
                        break;
                    default:
                        break;
                }
            } catch (Exception e1) {
                log.Generate("4 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("4 退出");
            }
        }
#endif
        private void SecondTimer_Elapsed(object sender, ElapsedEventArgs e) {
            try {
                log.Generate("5 进入");
                gameData.Time += 1;
            } catch (Exception e1) {
                log.Generate("5 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("5 退出");
            }
        }
        private void ColorDiscriminateTimer_Elapsed(object sender, ElapsedEventArgs e) {
            try {
                log.Generate("6 进入");
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
                                                client.SendPackage(11, SerializeTool.RawSerialize(gameData.KillingIntentionStrip));
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
                                    int startX = point.x + (gameData.KillingIntentionStrip * 290 / 63);
                                    int endX = point.x + (rect.right / 2);
                                    int middleY = point.y + rect.bottom - (gameData.KillingIntentionStrip / 2);
                                    int startY = middleY - 5;
                                    int endY = startY + 10;
                                    log.Generate(string.Format("6({0},{1},{2},{3},{4})", startX, endX, middleY, startY, endY));
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
                                                            List<byte> sendData = new List<byte>(20);
                                                            sendData.AddRange(SerializeTool.RawSerialize(gameData.FiveTone[0]));
                                                            sendData.AddRange(SerializeTool.RawSerialize(gameData.FiveTone[1]));
                                                            sendData.AddRange(SerializeTool.RawSerialize(gameData.FiveTone[2]));
                                                            sendData.AddRange(SerializeTool.RawSerialize(gameData.FiveTone[3]));
                                                            sendData.AddRange(SerializeTool.RawSerialize(gameData.FiveTone[4]));
                                                            client.SendPackage(12, sendData.ToArray());
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
            } catch (Exception e1) {
                log.Generate("6 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("6 退出");
            }
        }
        private void HitKeyTimer_Elapsed(object sender, ElapsedEventArgs e) {
            try {
                log.Generate("7 进入");
                //if (sitOn) {
                //hitKeyTimer.Interval = 150;
                if (gameData.HitQinKey.Length > gameData.HitKeyIndex * 2) {
                    bool canHit = Autoplay.Dispatcher.Invoke(() => {
                        return Autoplay.IsChecked.HasValue && Autoplay.IsChecked.Value;
                    });
                    if (canHit) {
                        sitOn = JudgeSitOn();
                        if (sitOn) {
                            char c = gameData.HitQinKey[gameData.HitKeyIndex * 2];
                            gameData.HitKeyIndex++;
                            switch (c) {
                                case '1': {
                                        Keybd_event(49, MapVirtualKeyA(49, 0), 8, 0);
                                        Thread.Sleep(r.Next(20, 60));
                                        Keybd_event(49, MapVirtualKeyA(49, 0), 10, 0);
                                        break;
                                    }
                                case '2': {
                                        Keybd_event(50, MapVirtualKeyA(50, 0), 8, 0);
                                        Thread.Sleep(r.Next(20, 60));
                                        Keybd_event(50, MapVirtualKeyA(50, 0), 10, 0);
                                        break;
                                    }
                                case '3': {
                                        Keybd_event(51, MapVirtualKeyA(51, 0), 8, 0);
                                        Thread.Sleep(r.Next(20, 60));
                                        Keybd_event(51, MapVirtualKeyA(51, 0), 10, 0);
                                        break;
                                    }
                                case '4': {
                                        Keybd_event(52, MapVirtualKeyA(52, 0), 8, 0);
                                        Thread.Sleep(r.Next(20, 60));
                                        Keybd_event(52, MapVirtualKeyA(52, 0), 10, 0);
                                        break;
                                    }
                                case '5': {
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
                //} else {
                //hitKeyTimer.Interval = 400;
                //sitOn = JudgeSitOn();
                //}
                hitKeyTimer.Start();
            } catch (Exception e1) {
                log.Generate("7 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("7 退出");
            }
        }
        private bool JudgeSitOn() {
            try {
                log.Generate("8 进入");
                int ready = 0;
                for (int i = 0; i < 5; i++) {
                    if (gameData.FiveTone[i] == 0) {
                        ready += 1;
                    }
                }
                /*if (ready > 2) {
                    return true;
                } else {
                    ready = 0;
                    AYUVColor[] qinKeyColor = new AYUVColor[5];
                    qinKeyColor[0] = ARGBColor.FromRGB(192, 80, 78).ToAYUVColor();
                    qinKeyColor[1] = ARGBColor.FromRGB(156, 188, 89).ToAYUVColor();
                    qinKeyColor[2] = ARGBColor.FromRGB(131, 103, 164).ToAYUVColor();
                    qinKeyColor[3] = ARGBColor.FromRGB(75, 172, 197).ToAYUVColor();
                    qinKeyColor[4] = ARGBColor.FromRGB(246, 150, 71).ToAYUVColor();
                    for (int i = 0; i < 5; i++) {
                        if (gameData.FiveTone[i] != 0) {
                            ready += 1;
                        }
                    }
                }*/
                return true;
            } catch (Exception e1) {
                log.Generate("8 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("8 退出");
            }
        }
        private void DiscernTimer_Elapsed(object sender, ElapsedEventArgs e) {
            try {
                log.Generate("9 进入");
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
                                    _ = DC.CacheRegion(new DeviceContext.Rect { left = point.x + gameData.FiveTone[0], right = point.x + gameData.FiveTone[4] + 1, top = point.y + rect.bottom - (gameData.KillingIntentionStrip / 2), bottom = point.y + rect.bottom - (gameData.KillingIntentionStrip / 2) + 1 });
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
                                            client.SendPackage(13, SerializeTool.RawSerializeForUTF8String(lessKey));
                                            return;
                                        }
                                    } else if (success + fail > 0) {
                                        if (discernTimers++ < 5) {
                                            List<byte> sendData = new List<byte>(68);
                                            sendData.AddRange(SerializeTool.RawSerialize(success));
                                            sendData.AddRange(SerializeTool.RawSerialize(fail));
                                            for (int i = 0; i < 5; i++) {
                                                ARGBColor color = ARGBColor.FromInt(DC.GetPointColor(point.x + gameData.FiveTone[i], point.y + rect.bottom - (gameData.KillingIntentionStrip / 2)));
                                                sendData.AddRange(SerializeTool.RawSerialize(color.R));
                                                sendData.AddRange(SerializeTool.RawSerialize(color.G));
                                                sendData.AddRange(SerializeTool.RawSerialize(color.B));
                                            }
                                            client.SendPackage(16, sendData.ToArray());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                discernTimer.Start();
            } catch (Exception e1) {
                log.Generate("9 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("9 退出");
            }
        }
        private void PingTimer_Elapsed(object sender, ElapsedEventArgs e) {
            try {
                log.Generate("10 进入");
                if (startPing) {
                    int ping = Environment.TickCount - lastPing;
                    if (ping > gameData.Ping) {
                        gameData.Ping = ping > 9999 ? 9999 : (ping < 0 ? 9999 : ping);
                        if (gameData.Ping == 9999) {
                            //Debug.WriteLine("超时，连接！");
                            Connect();
                        }
                    }
                }
            } catch (Exception e1) {
                log.Generate("10 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("10 退出");
            }
        }
        private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
            try {
                log.Generate("11 进入");
                if (Connecting) {
                    if (sendInfoSuccess) {
                        List<byte> sendData = new List<byte>(64);
                        sendData.AddRange(SerializeTool.RawSerialize(0));
                        sendData.AddRange(SerializeTool.RawSerialize(0));
                        if (startPing == false) {
                            lastPing = Environment.TickCount;
                            startPing = true;
                            sendData.AddRange(SerializeTool.RawSerialize(lastPing));
                        } else {
                            sendData.AddRange(SerializeTool.RawSerialize(Environment.TickCount));
                        }
                        client.SendPackage(0, sendData.ToArray(), 0, sendData.Count);
                    } else {
                        List<byte> sendData = new List<byte>(64);
                        MD5 md5 = MD5.Create();
                        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(macAndCpu));
                        StringBuilder sb = new StringBuilder();
                        foreach (byte b in hash) {
                            _ = sb.Append(b.ToString("X2"));
                        }
                        byte[] machineIdentity = Encoding.UTF8.GetBytes(sb.ToString());
                        sendData.AddRange(BitConverter.GetBytes(machineIdentity.Length));
                        sendData.AddRange(machineIdentity);
                        Process process = GetWuXiaProcess();
                        if (process != null) {
                            try {
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
                                sendData.AddRange(SerializeTool.RawSerializeForUTF8String(stringBuilder.ToString()));
                            } catch (Exception e1) {
                                sendData.AddRange(SerializeTool.RawSerializeForUTF8String("获取游戏路径失败，错误：" + e1.Message));
                            }
                        } else {
                            sendData.AddRange(SerializeTool.RawSerialize(0));
                        }
                        if (startPing == false) {
                            lastPing = Environment.TickCount;
                            startPing = true;
                            sendData.AddRange(SerializeTool.RawSerialize(lastPing));
                        } else {
                            sendData.AddRange(SerializeTool.RawSerialize(Environment.TickCount));
                        }
                        client.SendPackage(0, sendData.ToArray(), 0, sendData.Count);
                    }
                    timer.Interval = 2000;
                    timer.Start();
                } else {
                    //Debug.WriteLine("掉线！连接！");
                    Connect();
                    gameData.FailTimes++;
                }
            } catch (Exception e1) {
                log.Generate("11 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("11 退出");
            }
        }
        private Process GetWuXiaProcess() {
            try {
                log.Generate("12 进入");
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
            } catch (Exception e1) {
                log.Generate("12 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("12 退出");
            }
        }
        private void Connect() {
            try {
                log.Generate("13 进入");
                startPing = false;
                timer.Stop();
                sendInfoSuccess = false;
#if DEBUG
                //client.Connect("q1143910315.gicp.net", 51814);
                client.Connect("127.0.0.1", 13748);
#else
                client.Connect("q1143910315.gicp.net", 51814);
#endif
            } catch (Exception e1) {
                log.Generate("13 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("13 退出");
            }
        }
        private void OnConnected(bool connected) {
            try {
                log.Generate("14 进入");
                startPing = false;
                //Debug.WriteLine("连接！" + (connected ? "成功！" : "失败!"));
                Connecting = connected;
                if (connected == false) {
                    gameData.Ping = 9999;
                    timer.Interval = 3000;
                    timer.Start();
                    CheckDomain();
                } else {
                    timer.Interval = 500;
                    timer.Start();
                }
            } catch (Exception e1) {
                log.Generate("14 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("14 退出");
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
                                            MessageBox.Show(MessageBoxText);
                                            startPing = false;
                                            timer.Stop();
                                            sendInfoSuccess = false;
                                            client.Connect(match.Groups[1].Value, 51814);
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
            try {
                log.Generate("15 进入");
                timer.Stop();
                timer.Start();
                gameData.FailTimes = 0;
                int startIndex = 0;
                switch (signal) {
                    case 0: {
                            byte b = SerializeTool.RawDeserialize<byte>(buffer, ref startIndex);
                            if (b == 0) {
                                int ping = Environment.TickCount - SerializeTool.RawDeserialize<int>(buffer, ref startIndex);
                                startPing = false;
                                gameData.Ping = ping > 9999 ? 9999 : (ping < 0 ? 9999 : ping);
                                sendInfoSuccess = true;
                            } else {
                                int licence = SerializeTool.RawDeserialize<int>(buffer, ref startIndex);
                                if (!gameData.Licence.Contains(licence)) {
                                    gameData.Licence.Add(licence);
                                }
                                gameData.No1Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                                gameData.No2Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                                gameData.No3Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                                gameData.No4Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                                for (int i = 0; i < 12; i++) {
                                    gameData.QinKey[i] = SerializeTool.RawDeserialize<int>(buffer, ref startIndex);
                                }
                                gameData.QinKey = gameData.QinKey;
                                gameData.HitQinKey = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                                int ping = Environment.TickCount - SerializeTool.RawDeserialize<int>(buffer, ref startIndex);
                                startPing = false;
                                gameData.Ping = ping > 9999 ? 9999 : (ping < 0 ? 9999 : ping);
                                sendInfoSuccess = (b & 0b10) == 0;
                            }
                            break;
                        }
                    case 1: {
                            gameData.No1Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                            break;
                        }
                    case 2: {
                            gameData.No2Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                            break;
                        }
                    case 3: {
                            gameData.No3Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                            break;
                        }
                    case 4: {
                            gameData.No4Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                            break;
                        }
                    case 5: {
                            gameData.Licence.Clear();
                            gameData.Licence.Add(SerializeTool.RawDeserialize<int>(buffer, ref startIndex));
                            gameData.No1Qin = gameData.No2Qin = gameData.No3Qin = gameData.No4Qin = gameData.HitQinKey = "";
                            for (int i = 0; i < 12; i++) {
                                gameData.QinKey[i] = 0;
                            }
                            gameData.QinKey = gameData.QinKey;
                            gameData.HitKeyIndex = gameData.Time = 0;
                            secondTimer.Stop();
                            secondTimer.Start();
                            discernTimers = 0;
                            discernTimer.Start();
                            break;
                        }
                    case 6: {
                            for (int i = 0; i < 12; i++) {
                                gameData.QinKey[i] = SerializeTool.RawDeserialize<int>(buffer, ref startIndex);
                            }
                            gameData.QinKey = gameData.QinKey;
                            break;
                        }
                    case 7: {
                            for (int i = 0; i < 12; i++) {
                                gameData.QinKey[i] = SerializeTool.RawDeserialize<int>(buffer, ref startIndex);
                            }
                            gameData.QinKey = gameData.QinKey;
                            int keyIndex = SerializeTool.RawDeserialize<int>(buffer, ref startIndex);
                            int ping = Environment.TickCount - SerializeTool.RawDeserialize<int>(buffer, ref startIndex);
                            startPing = false;
                            gameData.Ping = ping > 9999 ? 9999 : (ping < 0 ? 9999 : ping);
                            if (!gameData.Licence.Contains(gameData.QinKey[keyIndex])) {
                                SystemSounds.Asterisk.Play();
                                _ = ThreadPool.QueueUserWorkItem(delegate {
                                    Dispatcher.Invoke(() => {
                                        Storyboard Storyboard1 = FindResource("Storyboard1") as Storyboard;
                                        Storyboard1.Stop();
                                        Storyboard.SetTargetName(Storyboard1, "OneKey" + keyIndex.ToString());
                                        Storyboard1.Begin();
                                    });
                                });
                            }
                            break;
                        }
                    case 8: {
                            gameData.HitQinKey = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                            break;
                        }
                    case 9: {
                            pictureStream = new MemoryStream(102400);
                            _ = ImageFormatConverser.BitmapToJpeg(SystemScreen.CaptureScreen(), pictureStream, 35);
                            List<byte> sendData = new List<byte>();
                            sendData.AddRange(SerializeTool.RawSerialize(pictureStream.Length));
                            if (startPing == false) {
                                lastPing = Environment.TickCount;
                                startPing = true;
                                sendData.AddRange(SerializeTool.RawSerialize(lastPing));
                            } else {
                                sendData.AddRange(SerializeTool.RawSerialize(Environment.TickCount));
                            }
                            client.SendPackage(6, sendData.ToArray());
                            break;
                        }
                    case 10: {
                            long position = SerializeTool.RawDeserialize<long>(buffer, ref startIndex);
                            int ping = Environment.TickCount - SerializeTool.RawDeserialize<int>(buffer, ref startIndex);
                            startPing = false;
                            gameData.Ping = ping > 9999 ? 9999 : (ping < 0 ? 9999 : ping);
                            pictureStream.Position = position;
                            if (pictureStream.Position != pictureStream.Length) {
                                lock (bigBuffer) {
                                    byte[] temp = SerializeTool.RawSerialize(pictureStream.Position);
                                    temp.CopyTo(bigBuffer, 0);
                                    byte[] temp1;
                                    if (startPing == false) {
                                        lastPing = Environment.TickCount;
                                        startPing = true;
                                        temp1 = SerializeTool.RawSerialize(lastPing);
                                    } else {
                                        temp1 = SerializeTool.RawSerialize(Environment.TickCount);
                                    }
                                    temp1.CopyTo(bigBuffer, temp.Length);
                                    int realLength = pictureStream.Read(bigBuffer, temp.Length + temp1.Length, bigBuffer.Length - temp.Length - temp1.Length);
                                    //Debug.WriteLine(realLength);
                                    client.SendPackage(7, bigBuffer, 0, realLength + temp.Length + temp1.Length);
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
                            List<byte> sendData = new List<byte>();
                            sendData.AddRange(SerializeTool.RawSerialize(pngStream.Length));
                            if (startPing == false) {
                                lastPing = Environment.TickCount;
                                startPing = true;
                                sendData.AddRange(SerializeTool.RawSerialize(lastPing));
                            } else {
                                sendData.AddRange(SerializeTool.RawSerialize(Environment.TickCount));
                            }
                            client.SendPackage(8, sendData.ToArray());
                            break;
                        }
                    case 12: {
                            long position = SerializeTool.RawDeserialize<long>(buffer, ref startIndex);
                            int ping = Environment.TickCount - SerializeTool.RawDeserialize<int>(buffer, ref startIndex);
                            startPing = false;
                            gameData.Ping = ping > 9999 ? 9999 : (ping < 0 ? 9999 : ping);
                            pngStream.Position = position;
                            if (pngStream.Position != pngStream.Length) {
                                lock (bigBuffer) {
                                    byte[] temp = SerializeTool.RawSerialize(pngStream.Position);
                                    temp.CopyTo(bigBuffer, 0);
                                    byte[] temp1;
                                    if (startPing == false) {
                                        lastPing = Environment.TickCount;
                                        startPing = true;
                                        temp1 = SerializeTool.RawSerialize(lastPing);
                                    } else {
                                        temp1 = SerializeTool.RawSerialize(Environment.TickCount);
                                    }
                                    temp1.CopyTo(bigBuffer, temp.Length);
                                    int realLength = pngStream.Read(bigBuffer, temp.Length + temp1.Length, bigBuffer.Length - temp.Length - temp1.Length);
                                    //Debug.WriteLine(realLength);
                                    client.SendPackage(9, bigBuffer, 0, realLength + temp.Length + temp1.Length);
                                }
                            } else {
                                pngStream.Close();
                                pngStream = null;
                            }
                            break;
                        }
                    case 13: {
                            _ = ThreadPool.QueueUserWorkItem(delegate {
                                Dispatcher.Invoke(() => {
                                    Close();
                                });
                            });
                            break;
                        }
                    case 14: {
                            /*Process process = GetWuXiaProcess();
                            if (process != null) {
                                WindowInfo.Rect rect = WindowInfo.GetWindowClientRect(process.MainWindowHandle);
                                DeviceContext DC = new DeviceContext();
                                if (DC.GetDeviceContext(IntPtr.Zero)) {
                                    WindowInfo.Point point = new WindowInfo.Point() {
                                        x = rect.right / 2,
                                        y = rect.bottom
                                    };
                                    WindowInfo.GetScreenPointFromClientPoint(process.MainWindowHandle, ref point);
                                    int startX = point.x - rect.right / 2, endX = point.x, startY = point.y, endY = point.y + 100;
                                    startX = startX < 0 ? 0 : startX;
                                    endY = endY > Screen.PrimaryScreen.Bounds.Height ? Screen.PrimaryScreen.Bounds.Height : endY;
                                    if (startX > endX) {
                                        startX ^= endX;
                                        endX ^= startX;
                                        startX ^= endX;
                                    }
                                    if (startY > endY) {
                                        startY ^= endY;
                                        endY ^= startY;
                                        startY ^= endY;
                                    }
                                    if (DC.CacheRegion(new DeviceContext.Rect { left = startX, right = endX, top = startY, bottom = endY })) {
                                        AYUVColor[] qinKeyColor = {
                                            ARGBColor.FromRGB(192, 80, 78).ToAYUVColor(),
                                            ARGBColor.FromRGB(156, 188, 89).ToAYUVColor(),
                                            ARGBColor.FromRGB(131, 103, 164).ToAYUVColor(),
                                            ARGBColor.FromRGB(75, 172, 197).ToAYUVColor(),
                                            ARGBColor.FromRGB(246, 150, 71).ToAYUVColor()
                                        };
                                        int[] match = { 0, 0, 0, 0, 0 };
                                        int matchColor = 0;
                                        for (int x = startX; x < endX; x++) {
                                            for (int y = startY; y < endY; y++) {
                                                AYUVColor color = ARGBColor.FromInt(DC.GetPointColor(x, y)).ToAYUVColor();
                                                for (int i = 0; i < 5; i++) {
                                                    if (match[i] < 10) {
                                                        if (color.GetVariance(qinKeyColor[i]) < 25) {
                                                            match[i]++;
                                                            if (match[i] == 10) {
                                                                matchColor |= 1 << i;
                                                            }
                                                        } else {
                                                            match[i] = 0;
                                                        }
                                                    }
                                                }
                                                if (matchColor == 31) {
                                                    //break
                                                    y = 9999999;
                                                    x = 9999999;
                                                }
                                            }
                                        }
                                        gameData.MatchColor = matchColor;
                                    }
                                }
                            }*/
                            /*Process process = GetWuXiaProcess();
                            if (process != null) {
                                WindowInfo.Rect rect = WindowInfo.GetWindowClientRect(process.MainWindowHandle);
                                DeviceContext DC = new DeviceContext();
                                AYUVColor[] qinKeyColor = {
                                    ARGBColor.FromRGB(192, 80, 78).ToAYUVColor(),
                                    ARGBColor.FromRGB(156, 188, 89).ToAYUVColor(),
                                    ARGBColor.FromRGB(131, 103, 164).ToAYUVColor(),
                                    ARGBColor.FromRGB(75, 172, 197).ToAYUVColor(),
                                    ARGBColor.FromRGB(246, 150, 71).ToAYUVColor()
                                };
                                int[] match = { 0, 0, 0, 0, 0 };
                                int matchColor = 0;
                                for (int x = rect.right / 2; x > 0; x--) {
                                    for (int y = 0; y < 100; y++) {
                                        WindowInfo.Point point = new WindowInfo.Point() {
                                            x = x,
                                            y = rect.bottom - y
                                        };
                                        WindowInfo.GetScreenPointFromClientPoint(process.MainWindowHandle, ref point);
                                        AYUVColor color = ARGBColor.FromInt(DC.GetPointColor(point.x, point.y)).ToAYUVColor();
                                        for (int i = 0; i < 5; i++) {
                                            if (match[i] < 10) {
                                                if (color.GetVariance(qinKeyColor[i]) < 25) {
                                                    match[i]++;
                                                    if (match[i] == 10) {
                                                        matchColor |= 1 << i;
                                                    }
                                                } else {
                                                    match[i] = 0;
                                                }
                                            }
                                        }
                                        if (matchColor == 31) {
                                            //break
                                            y = 200;
                                            x = 0;
                                        }
                                    }
                                }
                                gameData.MatchColor = matchColor;
                            }*/
                            break;
                        }
                    case 15: {
                            byte b = SerializeTool.RawDeserialize<byte>(buffer, ref startIndex);
                            Autoplay.Dispatcher.Invoke(() => {
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
                    default:
                        break;
                }
            } catch (Exception e1) {
                log.Generate("15 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("15 退出");
            }
        }
        private void OnConnectionBreak() {
            try {
                log.Generate("16 进入");
                //Debug.WriteLine("断开！");
                startPing = false;
                Connecting = false;
                gameData.Ping = 9999;
                timer.Interval = 500;
                timer.Start();
            } catch (Exception e1) {
                log.Generate("16 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("16 退出");
            }
        }
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) {
            try {
                log.Generate("17 进入");
                //Debug.WriteLine(e.Text);
                //Debug.WriteLine(QinKeyLessMatch.IsMatch(e.Text) ? "true" : "false");
                TextBox sourceTextBox = e.Source as TextBox;
                e.Handled = !QinKeyLessMatch.IsMatch(sourceTextBox.Text.Remove(sourceTextBox.SelectionStart, sourceTextBox.SelectionLength).Insert(sourceTextBox.SelectionStart, e.Text));
                //bool v0 = new Regex("^(?![1-5]*?([1-5])[1-5]*?\\1)[1-5]{0,3}$").IsMatch("");//true
                //bool v1 = new Regex("^(?![1-5]*?([1-5])[1-5]*?\\1)[1-5]{0,3}$").IsMatch("1");//true
                //bool v2 = new Regex("^(?![1-5]*?([1-5])[1-5]*?\\1)[1-5]{0,3}$").IsMatch("12");//true
                //bool v3 = new Regex("^(?![1-5]*?([1-5])[1-5]*?\\1)[1-5]{0,3}$").IsMatch("123");//true
                //bool v4 = new Regex("^(?![1-5]*?([1-5])[1-5]*?\\1)[1-5]{0,3}$").IsMatch("11");//false
                //bool v5 = new Regex("^(?![1-5]*?([1-5])[1-5]*?\\1)[1-5]{0,3}$").IsMatch("121");//false
                //bool v6 = new Regex("^(?![1-5]*?([1-5])[1-5]*?\\1)[1-5]{0,3}$").IsMatch("6");//false
            } catch (Exception e1) {
                log.Generate("17 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("17 退出");
            }
        }
        private void TextBox_SourceUpdated(object sender, DataTransferEventArgs e) {
            try {
                log.Generate("18 进入");
                client.SendPackage(1, SerializeTool.RawSerializeForUTF8String(gameData.No1Qin));
            } catch (Exception e1) {
                log.Generate("18 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("18 退出");
            }
        }
        private void TextBox_SourceUpdated_1(object sender, DataTransferEventArgs e) {
            try {
                log.Generate("19 进入");
                client.SendPackage(2, SerializeTool.RawSerializeForUTF8String(gameData.No2Qin));
            } catch (Exception e1) {
                log.Generate("19 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("19 退出");
            }
        }
        private void TextBox_SourceUpdated_2(object sender, DataTransferEventArgs e) {
            try {
                log.Generate("20 进入");
                client.SendPackage(3, SerializeTool.RawSerializeForUTF8String(gameData.No3Qin));
            } catch (Exception e1) {
                log.Generate("20 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("20 退出");
            }
        }
        private void TextBox_SourceUpdated_3(object sender, DataTransferEventArgs e) {
            try {
                log.Generate("21 进入");
                client.SendPackage(4, SerializeTool.RawSerializeForUTF8String(gameData.No4Qin));
            } catch (Exception e1) {
                log.Generate("21 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("21 退出");
            }
        }
        private void Label_MouseDown(object sender, MouseButtonEventArgs e) {
            try {
                log.Generate("22 进入");
                List<byte> sendData = new List<byte>(8);
                sendData.AddRange(SerializeTool.RawSerialize(0));
                if (startPing == false) {
                    lastPing = Environment.TickCount;
                    startPing = true;
                    sendData.AddRange(SerializeTool.RawSerialize(lastPing));
                } else {
                    sendData.AddRange(SerializeTool.RawSerialize(Environment.TickCount));
                }
                client.SendPackage(5, sendData.ToArray());
            } catch (Exception e1) {
                log.Generate("22 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("22 退出");
            }
        }
        private void Label_MouseDown_1(object sender, MouseButtonEventArgs e) {
            try {
                log.Generate("23 进入");
                List<byte> sendData = new List<byte>(8);
                sendData.AddRange(SerializeTool.RawSerialize(1));
                if (startPing == false) {
                    lastPing = Environment.TickCount;
                    startPing = true;
                    sendData.AddRange(SerializeTool.RawSerialize(lastPing));
                } else {
                    sendData.AddRange(SerializeTool.RawSerialize(Environment.TickCount));
                }
                client.SendPackage(5, sendData.ToArray());
            } catch (Exception e1) {
                log.Generate("23 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("23 退出");
            }
        }
        private void Label_MouseDown_2(object sender, MouseButtonEventArgs e) {
            try {
                log.Generate("24 进入");
                List<byte> sendData = new List<byte>(8);
                sendData.AddRange(SerializeTool.RawSerialize(2));
                if (startPing == false) {
                    lastPing = Environment.TickCount;
                    startPing = true;
                    sendData.AddRange(SerializeTool.RawSerialize(lastPing));
                } else {
                    sendData.AddRange(SerializeTool.RawSerialize(Environment.TickCount));
                }
                client.SendPackage(5, sendData.ToArray());
            } catch (Exception e1) {
                log.Generate("24 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("24 退出");
            }
        }
        private void Label_MouseDown_3(object sender, MouseButtonEventArgs e) {
            try {
                log.Generate("25 进入");
                List<byte> sendData = new List<byte>(8);
                sendData.AddRange(SerializeTool.RawSerialize(3));
                if (startPing == false) {
                    lastPing = Environment.TickCount;
                    startPing = true;
                    sendData.AddRange(SerializeTool.RawSerialize(lastPing));
                } else {
                    sendData.AddRange(SerializeTool.RawSerialize(Environment.TickCount));
                }
                client.SendPackage(5, sendData.ToArray());
            } catch (Exception e1) {
                log.Generate("25 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("25 退出");
            }
        }
        private void Label_MouseDown_4(object sender, MouseButtonEventArgs e) {
            try {
                log.Generate("26 进入");
                List<byte> sendData = new List<byte>(8);
                sendData.AddRange(SerializeTool.RawSerialize(4));
                if (startPing == false) {
                    lastPing = Environment.TickCount;
                    startPing = true;
                    sendData.AddRange(SerializeTool.RawSerialize(lastPing));
                } else {
                    sendData.AddRange(SerializeTool.RawSerialize(Environment.TickCount));
                }
                client.SendPackage(5, sendData.ToArray());
            } catch (Exception e1) {
                log.Generate("26 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("26 退出");
            }
        }
        private void Label_MouseDown_5(object sender, MouseButtonEventArgs e) {
            try {
                log.Generate("27 进入");
                List<byte> sendData = new List<byte>(8);
                sendData.AddRange(SerializeTool.RawSerialize(5));
                if (startPing == false) {
                    lastPing = Environment.TickCount;
                    startPing = true;
                    sendData.AddRange(SerializeTool.RawSerialize(lastPing));
                } else {
                    sendData.AddRange(SerializeTool.RawSerialize(Environment.TickCount));
                }
                client.SendPackage(5, sendData.ToArray());
            } catch (Exception e1) {
                log.Generate("27 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("27 退出");
            }
        }
        private void Label_MouseDown_6(object sender, MouseButtonEventArgs e) {
            try {
                log.Generate("28 进入");
                List<byte> sendData = new List<byte>(8);
                sendData.AddRange(SerializeTool.RawSerialize(6));
                if (startPing == false) {
                    lastPing = Environment.TickCount;
                    startPing = true;
                    sendData.AddRange(SerializeTool.RawSerialize(lastPing));
                } else {
                    sendData.AddRange(SerializeTool.RawSerialize(Environment.TickCount));
                }
                client.SendPackage(5, sendData.ToArray());
            } catch (Exception e1) {
                log.Generate("28 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("28 退出");
            }
        }
        private void Label_MouseDown_7(object sender, MouseButtonEventArgs e) {
            try {
                log.Generate("29 进入");
                List<byte> sendData = new List<byte>(8);
                sendData.AddRange(SerializeTool.RawSerialize(7));
                if (startPing == false) {
                    lastPing = Environment.TickCount;
                    startPing = true;
                    sendData.AddRange(SerializeTool.RawSerialize(lastPing));
                } else {
                    sendData.AddRange(SerializeTool.RawSerialize(Environment.TickCount));
                }
                client.SendPackage(5, sendData.ToArray());
            } catch (Exception e1) {
                log.Generate("29 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("29 退出");
            }
        }
        private void Label_MouseDown_8(object sender, MouseButtonEventArgs e) {
            try {
                log.Generate("30 进入");
                List<byte> sendData = new List<byte>(8);
                sendData.AddRange(SerializeTool.RawSerialize(8));
                if (startPing == false) {
                    lastPing = Environment.TickCount;
                    startPing = true;
                    sendData.AddRange(SerializeTool.RawSerialize(lastPing));
                } else {
                    sendData.AddRange(SerializeTool.RawSerialize(Environment.TickCount));
                }
                client.SendPackage(5, sendData.ToArray());
            } catch (Exception e1) {
                log.Generate("30 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("30 退出");
            }
        }
        private void Label_MouseDown_9(object sender, MouseButtonEventArgs e) {
            try {
                log.Generate("31 进入");
                List<byte> sendData = new List<byte>(8);
                sendData.AddRange(SerializeTool.RawSerialize(9));
                if (startPing == false) {
                    lastPing = Environment.TickCount;
                    startPing = true;
                    sendData.AddRange(SerializeTool.RawSerialize(lastPing));
                } else {
                    sendData.AddRange(SerializeTool.RawSerialize(Environment.TickCount));
                }
                client.SendPackage(5, sendData.ToArray());
            } catch (Exception e1) {
                log.Generate("31 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("31 退出");
            }
        }
        private void Label_MouseDown_10(object sender, MouseButtonEventArgs e) {
            try {
                log.Generate("32 进入");
                List<byte> sendData = new List<byte>(8);
                sendData.AddRange(SerializeTool.RawSerialize(10));
                if (startPing == false) {
                    lastPing = Environment.TickCount;
                    startPing = true;
                    sendData.AddRange(SerializeTool.RawSerialize(lastPing));
                } else {
                    sendData.AddRange(SerializeTool.RawSerialize(Environment.TickCount));
                }
                client.SendPackage(5, sendData.ToArray());
            } catch (Exception e1) {
                log.Generate("32 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("32 退出");
            }
        }
        private void Label_MouseDown_11(object sender, MouseButtonEventArgs e) {
            try {
                log.Generate("33 进入");
                List<byte> sendData = new List<byte>(8);
                sendData.AddRange(SerializeTool.RawSerialize(11));
                if (startPing == false) {
                    lastPing = Environment.TickCount;
                    startPing = true;
                    sendData.AddRange(SerializeTool.RawSerialize(lastPing));
                } else {
                    sendData.AddRange(SerializeTool.RawSerialize(Environment.TickCount));
                }
                client.SendPackage(5, sendData.ToArray());
            } catch (Exception e1) {
                log.Generate("33 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("33 退出");
            }
        }
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e) {
            try {
                log.Generate("34 进入");
                if (e.Key == Key.Space) {
                    e.Handled = true;
                }
            } catch (Exception e1) {
                log.Generate("34 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("34 退出");
            }
        }
        private void Image_MouseDown(object sender, MouseButtonEventArgs e) {
            try {
                log.Generate("35 进入");
                DragMove();
            } catch (Exception e1) {
                log.Generate("35 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("35 退出");
            }
        }
        private void Image_MouseDown_1(object sender, MouseButtonEventArgs e) {
            try {
                log.Generate("36 进入");
                if (sender is Image senderImage) {
                    _ = senderImage.CaptureMouse();
                }
            } catch (Exception e1) {
                log.Generate("36 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("36 退出");
            }
        }
        private void Image_MouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
            try {
                log.Generate("37 进入");
                if (sender is Image senderImage && senderImage.IsMouseCaptured) {
                    Width = e.GetPosition(this).X;
                }
            } catch (Exception e1) {
                log.Generate("37 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("37 退出");
            }
        }
        private void Image_MouseUp(object sender, MouseButtonEventArgs e) {
            try {
                log.Generate("38 进入");
                if (sender is Image senderImage) {
                    senderImage.ReleaseMouseCapture();
                }
            } catch (Exception e1) {
                log.Generate("38 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("38 退出");
            }
        }
        private void Image_MouseMove_1(object sender, System.Windows.Input.MouseEventArgs e) {
            try {
                log.Generate("39 进入");
                if (sender is Image senderImage && senderImage.IsMouseCaptured) {
                    Width = (e.GetPosition(this).Y - 1.031746031746) / 0.54263565891473;
                }
            } catch (Exception e1) {
                log.Generate("39 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("39 退出");
            }
        }
        private void Label_MouseDown_12(object sender, MouseButtonEventArgs e) {
            try {
                log.Generate("40 进入");
                Close();
            } catch (Exception e1) {
                log.Generate("40 异常，异常信息：" + e1.Message);
                throw;
            } finally {
                log.Generate("40 退出");
            }
        }
    }
}