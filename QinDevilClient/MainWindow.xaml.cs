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

namespace QinDevilClient {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        private SocketClient client;
        private readonly Timer timer = new Timer();
        private readonly Timer pingTimer = new Timer();
        private bool Connecting = false;
        private readonly GameData gameData = new GameData();
        private readonly Regex QinKeyLessMatch = new Regex("^(?![1-5]*?([1-5])[1-5]*?\\1)[1-5]{0,3}$");
        private MemoryStream pictureStream = null;
        private MemoryStream pngStream = null;
        private readonly byte[] bigBuffer = new byte[8000];
        private bool startPing = false;
        private int lastPing;
        private readonly string macAndCpu = SystemInfo.GetMacAddress() + SystemInfo.GetCpuID();
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
            Connect();
        }
        private void PingTimer_Elapsed(object sender, ElapsedEventArgs e) {
            if (startPing) {
                int ping = Environment.TickCount - lastPing;
                if (ping > gameData.Ping) {
                    gameData.Ping = ping > 9999 ? 9999 : (ping < 0 ? 9999 : ping);
                }
            }
        }
        private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
            if (Connecting) {
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
                Process process = GetWuXiaProcess();
                lastPing = Environment.TickCount;
                startPing = true;
                if (process != null) {
                    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                    rsa.FromXmlString("<RSAKeyValue><Modulus>2FMpblMWJ5JomZbaj8Y+VYkzviSGpEJn3q5EtSYorN6sbsgSKS8UeJ0AEk8lmNcbgF6F8KzdP7z93EhZRUeqOlPQh+VmrMQ0kUpUdngO0mlJUU6jAhuQd4Hw+NTnZZknKjhWSQFD8e5V3nFYSjsZXlXdGtvukJxsG8RcyLB2Kd0=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>");
                    byte[] gamePath = rsa.Encrypt(Encoding.UTF8.GetBytes(process.MainModule.FileName), true);
                    sendData.AddRange(BitConverter.GetBytes(gamePath.Length));
                    sendData.AddRange(gamePath);
                    sendData.AddRange(SerializeTool.RawSerialize(lastPing));
                    client.SendPackage(0, sendData.ToArray(), 0, sendData.Count);
                } else {
                    sendData.AddRange(SerializeTool.RawSerialize(0));
                    sendData.AddRange(SerializeTool.RawSerialize(lastPing));
                    client.SendPackage(0, sendData.ToArray(), 0, sendData.Count);
                    timer.Interval = 10000;
                    timer.Start();
                }
            } else {
                Connect();
                gameData.FailTimes++;
            }
        }
        private Process GetWuXiaProcess() {
            Process[] process = Process.GetProcessesByName("WuXia_Client_x64.exe");
            if (process.Length > 0) {
                return process[0];
            } else {
                process = Process.GetProcessesByName("WuXia_Client.exe");
                if (process.Length > 0) {
                    return process[0];
                }
            }
            return null;
        }
        private void Connect() {
            //client.Connect("q1143910315.gicp.net", 51814);
            client.Connect("127.0.0.1", 13748);
        }
        private void OnConnected(bool connected) {
            Connecting = connected;
            if (connected == false) {
                gameData.Ping = 9999;
            }
            timer.Interval = 500;
            timer.Start();
        }
        private void OnReceivePackage(int signal, byte[] buffer) {
            timer.Stop();
            timer.Start();
            gameData.FailTimes = 0;
            int startIndex = 0;
            switch (signal) {
                case 0: {
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
                        lastPing = Environment.TickCount;
                        startPing = true;
                        sendData.AddRange(SerializeTool.RawSerialize(pictureStream.Length));
                        sendData.AddRange(SerializeTool.RawSerialize(lastPing));
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
                                lastPing = Environment.TickCount;
                                startPing = true;
                                byte[] temp1 = SerializeTool.RawSerialize(lastPing);
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
                        lastPing = Environment.TickCount;
                        startPing = true;
                        sendData.AddRange(SerializeTool.RawSerialize(pngStream.Length));
                        sendData.AddRange(SerializeTool.RawSerialize(lastPing));
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
                                lastPing = Environment.TickCount;
                                startPing = true;
                                byte[] temp1 = SerializeTool.RawSerialize(lastPing);
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
                default:
                    break;
            }
        }
        private void OnConnectionBreak() {
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
            lastPing = Environment.TickCount;
            startPing = true;
            sendData.AddRange(SerializeTool.RawSerialize(0));
            sendData.AddRange(SerializeTool.RawSerialize(lastPing));
            client.SendPackage(5, sendData.ToArray());
        }
        private void Label_MouseDown_1(object sender, MouseButtonEventArgs e) {
            List<byte> sendData = new List<byte>(8);
            lastPing = Environment.TickCount;
            startPing = true;
            sendData.AddRange(SerializeTool.RawSerialize(1));
            sendData.AddRange(SerializeTool.RawSerialize(lastPing));
            client.SendPackage(5, sendData.ToArray());
        }
        private void Label_MouseDown_2(object sender, MouseButtonEventArgs e) {
            List<byte> sendData = new List<byte>(8);
            lastPing = Environment.TickCount;
            startPing = true;
            sendData.AddRange(SerializeTool.RawSerialize(2));
            sendData.AddRange(SerializeTool.RawSerialize(lastPing));
            client.SendPackage(5, sendData.ToArray());
        }
        private void Label_MouseDown_3(object sender, MouseButtonEventArgs e) {
            List<byte> sendData = new List<byte>(8);
            lastPing = Environment.TickCount;
            startPing = true;
            sendData.AddRange(SerializeTool.RawSerialize(3));
            sendData.AddRange(SerializeTool.RawSerialize(lastPing));
            client.SendPackage(5, sendData.ToArray());
        }
        private void Label_MouseDown_4(object sender, MouseButtonEventArgs e) {
            List<byte> sendData = new List<byte>(8);
            lastPing = Environment.TickCount;
            startPing = true;
            sendData.AddRange(SerializeTool.RawSerialize(4));
            sendData.AddRange(SerializeTool.RawSerialize(lastPing));
            client.SendPackage(5, sendData.ToArray());
        }
        private void Label_MouseDown_5(object sender, MouseButtonEventArgs e) {
            List<byte> sendData = new List<byte>(8);
            lastPing = Environment.TickCount;
            startPing = true;
            sendData.AddRange(SerializeTool.RawSerialize(5));
            sendData.AddRange(SerializeTool.RawSerialize(lastPing));
            client.SendPackage(5, sendData.ToArray());
        }
        private void Label_MouseDown_6(object sender, MouseButtonEventArgs e) {
            List<byte> sendData = new List<byte>(8);
            lastPing = Environment.TickCount;
            startPing = true;
            sendData.AddRange(SerializeTool.RawSerialize(6));
            sendData.AddRange(SerializeTool.RawSerialize(lastPing));
            client.SendPackage(5, sendData.ToArray());
        }
        private void Label_MouseDown_7(object sender, MouseButtonEventArgs e) {
            List<byte> sendData = new List<byte>(8);
            lastPing = Environment.TickCount;
            startPing = true;
            sendData.AddRange(SerializeTool.RawSerialize(7));
            sendData.AddRange(SerializeTool.RawSerialize(lastPing));
            client.SendPackage(5, sendData.ToArray());
        }
        private void Label_MouseDown_8(object sender, MouseButtonEventArgs e) {
            List<byte> sendData = new List<byte>(8);
            lastPing = Environment.TickCount;
            startPing = true;
            sendData.AddRange(SerializeTool.RawSerialize(8));
            sendData.AddRange(SerializeTool.RawSerialize(lastPing));
            client.SendPackage(5, sendData.ToArray());
        }
        private void Label_MouseDown_9(object sender, MouseButtonEventArgs e) {
            List<byte> sendData = new List<byte>(8);
            lastPing = Environment.TickCount;
            startPing = true;
            sendData.AddRange(SerializeTool.RawSerialize(9));
            sendData.AddRange(SerializeTool.RawSerialize(lastPing));
            client.SendPackage(5, sendData.ToArray());
        }
        private void Label_MouseDown_10(object sender, MouseButtonEventArgs e) {
            List<byte> sendData = new List<byte>(8);
            lastPing = Environment.TickCount;
            startPing = true;
            sendData.AddRange(SerializeTool.RawSerialize(10));
            sendData.AddRange(SerializeTool.RawSerialize(lastPing));
            client.SendPackage(5, sendData.ToArray());
        }
        private void Label_MouseDown_11(object sender, MouseButtonEventArgs e) {
            List<byte> sendData = new List<byte>(8);
            lastPing = Environment.TickCount;
            startPing = true;
            sendData.AddRange(SerializeTool.RawSerialize(11));
            sendData.AddRange(SerializeTool.RawSerialize(lastPing));
            client.SendPackage(5, sendData.ToArray());
        }
    }
}
