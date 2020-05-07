﻿using QinDevilCommon;
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
#if service
        private readonly KeyboardHook hook = new KeyboardHook();
        private bool ctrlState;
#endif
        public MainWindow() {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e) {
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
        }
#if service
        private void KeyDownCallbak(KeyCode keyCode) {
            switch (keyCode) {
                case KeyCode.VK_LCONTROL: {
                        ctrlState = true;
                        break;
                    }
                case KeyCode.Numeric1: {
                        if (ctrlState) {
                            gameData.HitQinKeyAny += "1 ";
                            client.SendPackage(14, SerializeTool.RawSerializeForUTF8String(gameData.HitQinKeyAny));
                        }
                        break;
                    }
                case KeyCode.Numeric2: {
                        if (ctrlState) {
                            gameData.HitQinKeyAny += "2 ";
                            client.SendPackage(14, SerializeTool.RawSerializeForUTF8String(gameData.HitQinKeyAny));
                        }
                        break;
                    }
                case KeyCode.Numeric3: {
                        if (ctrlState) {
                            gameData.HitQinKeyAny += "3 ";
                            client.SendPackage(14, SerializeTool.RawSerializeForUTF8String(gameData.HitQinKeyAny));
                        }
                        break;
                    }
                case KeyCode.Numeric4: {
                        if (ctrlState) {
                            gameData.HitQinKeyAny += "4 ";
                            client.SendPackage(14, SerializeTool.RawSerializeForUTF8String(gameData.HitQinKeyAny));
                        }
                        break;
                    }
                case KeyCode.Numeric5: {
                        if (ctrlState) {
                            gameData.HitQinKeyAny += "5 ";
                            client.SendPackage(14, SerializeTool.RawSerializeForUTF8String(gameData.HitQinKeyAny));
                        }
                        break;
                    }
                case KeyCode.Numeric7: {
                        if (ctrlState) {
                            gameData.HitQinKeyAny = "";
                            client.SendPackage(15, null);
                        }
                        break;
                    }
                default:
                    break;
            }
        }
        private void KeyUpCallbak(KeyCode keyCode) {
            switch (keyCode) {
                case KeyCode.VK_LCONTROL:
                    ctrlState = false;
                    break;
                default:
                    break;
            }
        }
#endif
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
                                DeviceContext DC = new DeviceContext();
                                if (DC.GetDeviceContext(IntPtr.Zero)) {
                                    _ = DC.CacheRegion(new DeviceContext.Rect { left = startX, right = endX, top = startY, bottom = endY });
                                    AYUVColor color;
                                    for (int i = startX; i < endX; i++) {
                                        color = ARGBColor.FromInt(DC.GetPointColor(i, middleY)).ToAYUVColor();
                                        for (int j = 0; j < 5; j++) {
                                            if (i - gameData.FiveTone[j] < point.x && color.GetVariance(qinKeyColor[j]) < 25) {
                                                int matchTime = 1;
                                                for (int k = 0; k < 5; k++) {
                                                    color = ARGBColor.FromInt(DC.GetPointColor(i, middleY - k - 1)).ToAYUVColor();
                                                    if (color.GetVariance(qinKeyColor[j]) < 25) {
                                                        matchTime += 1;
                                                    } else {
                                                        break;
                                                    }
                                                }
                                                for (int k = 1; k < 5; k++) {
                                                    color = ARGBColor.FromInt(DC.GetPointColor(i, middleY + k)).ToAYUVColor();
                                                    if (color.GetVariance(qinKeyColor[j]) < 25) {
                                                        matchTime += 1;
                                                    } else {
                                                        break;
                                                    }
                                                }
                                                if (matchTime > 5) {
                                                    gameData.FiveTone[j] = i - point.x;
                                                    List<byte> sendData = new List<byte>(8);
                                                    sendData.AddRange(SerializeTool.RawSerialize(j));
                                                    sendData.AddRange(SerializeTool.RawSerialize(gameData.FiveTone[j]));
                                                    client.SendPackage(12, sendData.ToArray());
                                                }
                                            }
                                        }
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
        }
        private bool JudgeSitOn() {
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
        }
        private void DiscernTimer_Elapsed(object sender, ElapsedEventArgs e) {
            if (sendInfoSuccess) {
                bool ready = true;
                for (int i = 0; i < 5; i++) {
                    if (gameData.FiveTone[i] == 0) {
                        ready = false;
                        break;
                    }
                }
                if (ready) {
                    Process process = GetWuXiaProcess();
                    if (process != null) {
                        WindowInfo.Rect rect = WindowInfo.GetWindowClientRect(process.MainWindowHandle);
                        if (rect.bottom > 100 && rect.right > 100) {
                            WindowInfo.Point point = new WindowInfo.Point() {
                                x = 0,
                                y = 0
                            };
                            if (WindowInfo.GetScreenPointFromClientPoint(process.MainWindowHandle, ref point)) {
                                if (gameData.KillingIntentionStrip != 0) {
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
                                            }
                                        }
                                        if (success + fail == 5) {
                                            if (fail > 0 && fail < 4) {
                                                client.SendPackage(13, SerializeTool.RawSerializeForUTF8String(lessKey));
                                                return;
                                            }
                                        } else if (success + fail > 0) {
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
                                            if (discernTimers++ > 3) {
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    discernTimer.Start();
                }
            }
        }
        private void PingTimer_Elapsed(object sender, ElapsedEventArgs e) {
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
        }
        private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
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
        private void Connect() {
            startPing = false;
            timer.Stop();
            sendInfoSuccess = false;
#if DEBUG
            //client.Connect("q1143910315.gicp.net", 51814);
            client.Connect("127.0.0.1", 13748);
#else
            client.Connect("q1143910315.gicp.net", 51814);
#endif
        }
        private void OnConnected(bool connected) {
            startPing = false;
            //Debug.WriteLine("连接！" + (connected ? "成功！" : "失败!"));
            Connecting = connected;
            if (connected == false) {
                gameData.Ping = 9999;
                timer.Interval = 3000;
                timer.Start();
            } else {
                timer.Interval = 500;
                timer.Start();
            }
        }
        private void OnReceivePackage(int signal, byte[] buffer) {
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
                        for (int i = 0; i < 5; i++) {
                            gameData.FiveTone[i] = 99999;
                        }
                        break;
                    }
                default:
                    break;
            }
        }
        private void OnConnectionBreak() {
            //Debug.WriteLine("断开！");
            startPing = false;
            Connecting = false;
            gameData.Ping = 9999;
            timer.Interval = 500;
            timer.Start();
        }
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) {
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
        }
        private void TextBox_SourceUpdated(object sender, DataTransferEventArgs e) {
            client.SendPackage(1, SerializeTool.RawSerializeForUTF8String(gameData.No1Qin));
        }
        private void TextBox_SourceUpdated_1(object sender, DataTransferEventArgs e) {
            client.SendPackage(2, SerializeTool.RawSerializeForUTF8String(gameData.No2Qin));
        }
        private void TextBox_SourceUpdated_2(object sender, DataTransferEventArgs e) {
            client.SendPackage(3, SerializeTool.RawSerializeForUTF8String(gameData.No3Qin));
        }
        private void TextBox_SourceUpdated_3(object sender, DataTransferEventArgs e) {
            client.SendPackage(4, SerializeTool.RawSerializeForUTF8String(gameData.No4Qin));
        }
        private void Label_MouseDown(object sender, MouseButtonEventArgs e) {
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
        }
        private void Label_MouseDown_1(object sender, MouseButtonEventArgs e) {
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
        }
        private void Label_MouseDown_2(object sender, MouseButtonEventArgs e) {
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
        }
        private void Label_MouseDown_3(object sender, MouseButtonEventArgs e) {
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
        }
        private void Label_MouseDown_4(object sender, MouseButtonEventArgs e) {
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
        }
        private void Label_MouseDown_5(object sender, MouseButtonEventArgs e) {
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
        }
        private void Label_MouseDown_6(object sender, MouseButtonEventArgs e) {
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
        }
        private void Label_MouseDown_7(object sender, MouseButtonEventArgs e) {
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
        }
        private void Label_MouseDown_8(object sender, MouseButtonEventArgs e) {
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
        }
        private void Label_MouseDown_9(object sender, MouseButtonEventArgs e) {
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
        }
        private void Label_MouseDown_10(object sender, MouseButtonEventArgs e) {
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
        }
        private void Label_MouseDown_11(object sender, MouseButtonEventArgs e) {
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
        }
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Space) {
                e.Handled = true;
            }
        }
        private void Image_MouseDown(object sender, MouseButtonEventArgs e) {
            DragMove();
        }
        private void Image_MouseDown_1(object sender, MouseButtonEventArgs e) {
            if (sender is Image senderImage) {
                _ = senderImage.CaptureMouse();
            }
        }
        private void Image_MouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
            if (sender is Image senderImage && senderImage.IsMouseCaptured) {
                Width = e.GetPosition(this).X;
            }
        }
        private void Image_MouseUp(object sender, MouseButtonEventArgs e) {
            if (sender is Image senderImage) {
                senderImage.ReleaseMouseCapture();
            }
        }
        private void Image_MouseMove_1(object sender, System.Windows.Input.MouseEventArgs e) {
            if (sender is Image senderImage && senderImage.IsMouseCaptured) {
                Width = (e.GetPosition(this).Y - 1.031746031746) / 0.54263565891473;
            }
        }
        private void Label_MouseDown_12(object sender, MouseButtonEventArgs e) {
            Close();
        }
    }
}