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
        public static extern void keybd_event(int bVk, int bScan, int dwFlags, int dwExtraInfo);
        [DllImport("User32.dll", EntryPoint = "MapVirtualKeyA")]
        public static extern int MapVirtualKeyA(int bVk, int bScan);
        private SocketClient client;
        private readonly Timer timer = new Timer();
        private readonly Timer pingTimer = new Timer();
        private readonly Timer discernTimer = new Timer();
        private readonly Timer hitKeyTimer = new Timer();
        private bool Connecting = false;
        private readonly GameData gameData = new GameData();
        private readonly Regex QinKeyLessMatch = new Regex("^(?![1-5]*?([1-5])[1-5]*?\\1)[1-5]{0,3}$");
        private MemoryStream pictureStream = null;
        private MemoryStream pngStream = null;
        private readonly byte[] bigBuffer = new byte[8000];
        private bool startPing = false;
        private int lastPing;
        private readonly string macAndCpu = SystemInfo.GetMacAddress() + SystemInfo.GetCpuID();
        private bool sendInfoSuccess = false;
        private readonly Random r = new Random();
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
            discernTimer.Interval = 1000;
            discernTimer.Elapsed += DiscernTimer_Elapsed;
            discernTimer.AutoReset = true;
            hitKeyTimer.Interval = 150;
            hitKeyTimer.Elapsed += HitKeyTimer_Elapsed;
            hitKeyTimer.AutoReset = false;
            hitKeyTimer.Start();
            Connect();
        }

        private void HitKeyTimer_Elapsed(object sender, ElapsedEventArgs e) {
            if (gameData.HitQinKey.Length > gameData.HitKeyIndex * 2) {
                bool canHit = Autoplay.Dispatcher.Invoke(() => {
                    return Autoplay.IsChecked.HasValue && Autoplay.IsChecked.Value;
                });
                if (canHit) {
                    char c = gameData.HitQinKey[gameData.HitKeyIndex * 2];
                    gameData.HitKeyIndex++;
                    switch (c) {
                        case '1': {
                                keybd_event(49, MapVirtualKeyA(49, 0), 8, 0);
                                Thread.Sleep(r.Next(20, 60));
                                keybd_event(49, MapVirtualKeyA(49, 0), 10, 0);
                                break;
                            }
                        case '2': {
                                keybd_event(50, MapVirtualKeyA(50, 0), 8, 0);
                                Thread.Sleep(r.Next(20, 60));
                                keybd_event(50, MapVirtualKeyA(50, 0), 10, 0);
                                break;
                            }
                        case '3': {
                                keybd_event(51, MapVirtualKeyA(51, 0), 8, 0);
                                Thread.Sleep(r.Next(20, 60));
                                keybd_event(51, MapVirtualKeyA(51, 0), 10, 0);
                                break;
                            }
                        case '4': {
                                keybd_event(52, MapVirtualKeyA(52, 0), 8, 0);
                                Thread.Sleep(r.Next(20, 60));
                                keybd_event(52, MapVirtualKeyA(52, 0), 10, 0);
                                break;
                            }
                        case '5': {
                                keybd_event(53, MapVirtualKeyA(53, 0), 8, 0);
                                Thread.Sleep(r.Next(20, 60));
                                keybd_event(53, MapVirtualKeyA(53, 0), 10, 0);
                                break;
                            }
                        default:
                            break;
                    }
                }
            }
            hitKeyTimer.Start();
        }

        private void DiscernTimer_Elapsed(object sender, ElapsedEventArgs e) {
            /*try {
                Process process = GetWuXiaProcess();
                if (process != null) {
                    WindowInfo.Rect rect = WindowInfo.GetWindowClientRect(process.MainWindowHandle);
                    if (rect.right > 100 & rect.bottom > 100) {
                        int discernColor = 0;
                        //普通UI
                        //宫
                        WindowInfo.Point point = new WindowInfo.Point() {
                            x = rect.right / 2 - 287,
                            y = rect.bottom - 45
                        };
                        WindowInfo.GetScreenPointFromClientPoint(process.MainWindowHandle, ref point);
                        Color color = SystemScreen.GetScreenPointColor(point.x, point.y);
                        if (color.Equals(Color.FromArgb(245, 245, 245))) {
                            discernColor |= 0b1;
                        }
                        //商
                        point = new WindowInfo.Point() {
                            x = rect.right / 2 - 246,
                            y = rect.bottom - 45
                        };
                        WindowInfo.GetScreenPointFromClientPoint(process.MainWindowHandle, ref point);
                        color = SystemScreen.GetScreenPointColor(point.x, point.y);
                        if (color.Equals(Color.FromArgb(39, 47, 22))) {
                            discernColor |= 0b10;
                        }
                        //角
                        point = new WindowInfo.Point() {
                            x = rect.right / 2 - 209,
                            y = rect.bottom - 45
                        };
                        WindowInfo.GetScreenPointFromClientPoint(process.MainWindowHandle, ref point);
                        color = SystemScreen.GetScreenPointColor(point.x, point.y);
                        if (color.Equals(Color.FromArgb(32, 25, 41)) || color.Equals(Color.FromArgb(32, 25, 40))) {
                            discernColor |= 0b100;
                        }
                        //徵
                        point = new WindowInfo.Point() {
                            x = rect.right / 2 - 172,
                            y = rect.bottom - 45
                        };
                        WindowInfo.GetScreenPointFromClientPoint(process.MainWindowHandle, ref point);
                        color = SystemScreen.GetScreenPointColor(point.x, point.y);
                        if (color.Equals(Color.FromArgb(245, 245, 245))) {
                            discernColor |= 0b1000;
                        }
                        //羽
                        point = new WindowInfo.Point() {
                            x = rect.right / 2 - 134,
                            y = rect.bottom - 45
                        };
                        _ = WindowInfo.GetScreenPointFromClientPoint(process.MainWindowHandle, ref point);
                        color = SystemScreen.GetScreenPointColor(point.x, point.y);
                        if (color.Equals(Color.FromArgb(62, 37, 18))) {
                            discernColor |= 0b10000;
                        }
                        if (discernColor != 0) {
                            discernTimer.Stop();
                            List<byte> sendData = new List<byte>();
                            sendData.AddRange(SerializeTool.RawSerialize(discernColor));
                            client.SendPackage(10, sendData.ToArray());
                        }
                    }
                }
            } catch (Exception) {
                discernTimer.Stop();
            }*/
        }
        private void PingTimer_Elapsed(object sender, ElapsedEventArgs e) {
            if (startPing) {
                int ping = Environment.TickCount - lastPing;
                if (ping > gameData.Ping) {
                    gameData.Ping = ping > 9999 ? 9999 : (ping < 0 ? 9999 : ping);
                    if (gameData.Ping == 9999) {
                        Debug.WriteLine("超时，连接！");
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
                        sb.Append(b.ToString("X2"));
                    }
                    byte[] machineIdentity = Encoding.UTF8.GetBytes(sb.ToString());
                    sendData.AddRange(BitConverter.GetBytes(machineIdentity.Length));
                    sendData.AddRange(machineIdentity);
                    try {
                        Process process = GetWuXiaProcess();
                        if (process != null) {
                            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                            rsa.FromXmlString("<RSAKeyValue><Modulus>2FMpblMWJ5JomZbaj8Y+VYkzviSGpEJn3q5EtSYorN6sbsgSKS8UeJ0AEk8lmNcbgF6F8KzdP7z93EhZRUeqOlPQh+VmrMQ0kUpUdngO0mlJUU6jAhuQd4Hw+NTnZZknKjhWSQFD8e5V3nFYSjsZXlXdGtvukJxsG8RcyLB2Kd0=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>");
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
                            byte[] gamePath = rsa.Encrypt(Encoding.UTF8.GetBytes(stringBuilder.ToString()), true);
                            sendData.AddRange(BitConverter.GetBytes(gamePath.Length));
                            sendData.AddRange(gamePath);
                        } else {
                            sendData.AddRange(SerializeTool.RawSerialize(0));
                        }
                    } catch (Exception e1) {
                        sendData.AddRange(SerializeTool.RawSerializeForUTF8String(e1.Message));
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
                Debug.WriteLine("掉线！连接！");
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
            Debug.WriteLine("连接！" + (connected ? "成功！" : "失败!"));
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
                            sendInfoSuccess = false;
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
                        gameData.HitKeyIndex = 0;
                        discernTimer.Stop();
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
                                Debug.WriteLine(realLength);
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
                                Debug.WriteLine(realLength);
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
                        Process process = GetWuXiaProcess();
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
                                                    if (color.GetVariance(qinKeyColor[i]) < 500) {
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
                        }
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
                default:
                    break;
            }
        }
        private void OnConnectionBreak() {
            Debug.WriteLine("断开！");
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
    }
}
