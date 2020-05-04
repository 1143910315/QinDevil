using QinDevilCommon.Data;
using QinDevilCommon;
using QinDevilCommon.Keyboard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.IO;
using QinDevilCommon.FileIO;
using System.Windows.Forms;
using ListViewItem = System.Windows.Controls.ListViewItem;
using QinDevilCommon.ColorClass;
using MenuItem = System.Windows.Controls.MenuItem;

namespace QinDevilServer {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        private SocketServer server;
        private readonly GameData gameData = new GameData();
        private readonly KeyboardHook hook = new KeyboardHook();
        private readonly IniFile iniFile = new IniFile(AppDomain.CurrentDomain.BaseDirectory + "用户记录.ini");
        private bool ctrlState;
        private readonly ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
        private UserInfo menuUser;
        public MainWindow() {
            InitializeComponent();
            ARGBColor aRGBColor = ARGBColor.FromRGB(247, 150, 71);
            ARGBColor aRGBColor1 = ARGBColor.FromRGB(246, 145, 70);
            AYUVColor aYUVColor = aRGBColor.ToAYUVColor();
            Debug.WriteLine(aYUVColor.GetVariance(aRGBColor1.ToAYUVColor()));//25
            contextMenuStrip.Items.Add("请求所有玩家屏幕截图（JPG）").Click += PrintScreenAll_Click;
            contextMenuStrip.Items.Add("请求当前玩家截图（JPG）").Click += PrintScreen_Click;
            contextMenuStrip.Items.Add("请求当前玩家截图（PNG）").Click += PrintScreenHighQuality_Click;
            contextMenuStrip.Items.Add("查看当前玩家截图").Click += PictureViewer_Click;
            contextMenuStrip.Items.Add("清除当前玩家游戏路径").Click += ClearGamePath_Click;
            contextMenuStrip.Items.Add("扫描当前玩家缺弦").Click += Scanning_Click;
            contextMenuStrip.Items.Add("判断当前玩家杀意条").Click += KillingIntentionStrip_Click;
            contextMenuStrip.Items.Add("断开当前玩家").Click += CloseClient_Click;
        }
        private void KillingIntentionStrip_Click(object sender, EventArgs e) {
            if (menuUser != null) {
                server.SendPackage(menuUser.Id, 16, null);
            }
        }
        private void CloseClient_Click(object sender, EventArgs e) {
            if (menuUser != null) {
                server.SendPackage(menuUser.Id, 13, null);
                server.CloseClient(menuUser.Id);
            }
        }
        private void Scanning_Click(object sender, EventArgs e) {
            if (menuUser != null) {
                server.SendPackage(menuUser.Id, 14, null);
            }
        }
        private void ClearGamePath_Click(object sender, EventArgs e) {
            if (menuUser != null) {
                menuUser.GamePath = "";
            }
        }
        private void PictureViewer_Click(object sender, EventArgs e) {
            if (menuUser != null) {
                _ = new PictureViewerWindow(menuUser);
            }
        }
        private void PrintScreenAll_Click(object sender, RoutedEventArgs e) {
            for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                server.SendPackage(gameData.ClientInfo.Get(i).Id, 9, null);
            }
        }
        private void AllowAutoPlay_Click(object sender, RoutedEventArgs e) {
            if (sender is MenuItem menuItem) {
                //menuItem.IsChecked = true; 
                byte[] sendData = menuItem.IsChecked ? SerializeTool.RawSerialize((byte)1) : SerializeTool.RawSerialize((byte)0);
                for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                    server.SendPackage(gameData.ClientInfo.Get(i).Id, 15, sendData);
                }
            }
        }
        private void AllowAutoLessKey_Click(object sender, RoutedEventArgs e) {
            gameData.AutoLessKey = !gameData.AutoLessKey;
        }
        private void PrintScreenAll_Click(object sender, EventArgs e) {
            for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                server.SendPackage(gameData.ClientInfo.Get(i).Id, 9, null);
            }
        }
        private void PrintScreenHighQuality_Click(object sender, EventArgs e) {
            if (menuUser != null) {
                server.SendPackage(menuUser.Id, 11, null);
            }
        }
        private void PrintScreen_Click(object sender, EventArgs e) {
            if (menuUser != null) {
                server.SendPackage(menuUser.Id, 9, null);
            }
        }
        private void LogViewer_Click(object sender, RoutedEventArgs e) {
            new LogViewerWindow(new List<LogDetail>(gameData.Log.ToArray())).Show();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            server = new SocketServer(13748) {
                onAcceptSuccessEvent = OnAcceptSuccess,
                onReceiveOriginalDataEvent = OnReceiveOriginalData,
                onReceivePackageEvent = OnReceivePackage,
                onLeaveEvent = OnLeave
            };
            GamePanel.DataContext = gameData;
            hook.KeyDownEvent += KeyDownCallbak;
            hook.KeyUpEvent += KeyUpCallbak;
        }
        private void KeyDownCallbak(KeyCode keyCode) {
            switch (keyCode) {
                case KeyCode.VK_LCONTROL: {
                        ctrlState = true;
                        break;
                    }
                case KeyCode.Numeric1: {
                        gameData.Log.InsertAfter(-1, new LogDetail() {
                            Content = "数字键 1被按下，" + (ctrlState ? "按下了ctrl。" : "没按ctrl。"),
                            Time = Environment.TickCount
                        });
                        if (ctrlState) {
                            gameData.HitQinKey += "1 ";
                            byte[] sendData = SerializeTool.RawSerializeForUTF8String(gameData.HitQinKey);
                            for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                                UserInfo tempUserInfo = gameData.ClientInfo.Get(i);
                                server.SendPackage(tempUserInfo.Id, 8, sendData);
                            }
                        }
                        break;
                    }
                case KeyCode.Numeric2: {
                        gameData.Log.InsertAfter(-1, new LogDetail() {
                            Content = "数字键 2被按下，" + (ctrlState ? "按下了ctrl。" : "没按ctrl。"),
                            Time = Environment.TickCount
                        });
                        if (ctrlState) {
                            gameData.HitQinKey += "2 ";
                            byte[] sendData = SerializeTool.RawSerializeForUTF8String(gameData.HitQinKey);
                            for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                                UserInfo tempUserInfo = gameData.ClientInfo.Get(i);
                                server.SendPackage(tempUserInfo.Id, 8, sendData);
                            }
                        }
                        break;
                    }
                case KeyCode.Numeric3: {
                        gameData.Log.InsertAfter(-1, new LogDetail() {
                            Content = "数字键 3被按下，" + (ctrlState ? "按下了ctrl。" : "没按ctrl。"),
                            Time = Environment.TickCount
                        });
                        if (ctrlState) {
                            gameData.HitQinKey += "3 ";
                            byte[] sendData = SerializeTool.RawSerializeForUTF8String(gameData.HitQinKey);
                            for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                                UserInfo tempUserInfo = gameData.ClientInfo.Get(i);
                                server.SendPackage(tempUserInfo.Id, 8, sendData);
                            }
                        }
                        break;
                    }
                case KeyCode.Numeric4: {
                        gameData.Log.InsertAfter(-1, new LogDetail() {
                            Content = "数字键 4被按下，" + (ctrlState ? "按下了ctrl。" : "没按ctrl。"),
                            Time = Environment.TickCount
                        });
                        if (ctrlState) {
                            gameData.HitQinKey += "4 ";
                            byte[] sendData = SerializeTool.RawSerializeForUTF8String(gameData.HitQinKey);
                            for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                                UserInfo tempUserInfo = gameData.ClientInfo.Get(i);
                                server.SendPackage(tempUserInfo.Id, 8, sendData);
                            }
                        }
                        break;
                    }
                case KeyCode.Numeric5: {
                        gameData.Log.InsertAfter(-1, new LogDetail() {
                            Content = "数字键 5被按下，" + (ctrlState ? "按下了ctrl。" : "没按ctrl。"),
                            Time = Environment.TickCount
                        });
                        if (ctrlState) {
                            gameData.HitQinKey += "5 ";
                            byte[] sendData = SerializeTool.RawSerializeForUTF8String(gameData.HitQinKey);
                            for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                                UserInfo tempUserInfo = gameData.ClientInfo.Get(i);
                                server.SendPackage(tempUserInfo.Id, 8, sendData);
                            }
                        }
                        break;
                    }
                case KeyCode.Numeric7: {
                        if (ctrlState) {
                            gameData.Log.InsertAfter(-1, new LogDetail() {
                                Content = "补弦清屏---------------------",
                                Time = Environment.TickCount
                            });
                            gameData.No1Qin = gameData.No2Qin = gameData.No3Qin = gameData.No4Qin = gameData.HitQinKey = "";
                            for (int i = 0; i < 12; i++) {
                                gameData.QinKey[i] = 0;
                            }
                            for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                                UserInfo tempUserInfo = gameData.ClientInfo.Get(i);
                                server.SendPackage(tempUserInfo.Id, 5, SerializeTool.RawSerialize(tempUserInfo.Id));
                            }
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
        private object OnAcceptSuccess(int id) {
            gameData.Log.InsertAfter(-1, new LogDetail() {
                Content = "客户 " + id.ToString() + "进入。",
                Time = Environment.TickCount
            });
            UserInfo userInfo = new UserInfo() {
                Id = id,
                LastReceiveTime = DateTime.Now
            };
            _ = ThreadPool.QueueUserWorkItem(delegate {
                _ = Dispatcher.Invoke(new Action(() => gameData.ClientInfo.InsertAfter(-1, userInfo)), null);
            });
            return userInfo;
        }
        private bool OnReceiveOriginalData(int id, byte[] buffer, int offest, int count, object userToken) {
            return false;
        }
        private void OnReceivePackage(int id, int signal, byte[] buffer, object userToken) {
            UserInfo userInfo = userToken as UserInfo;
            if (userInfo == null) {
                for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                    if (gameData.ClientInfo.Get(i).Id == id) {
                        userInfo = gameData.ClientInfo.Get(i);
                        break;
                    }
                }
            }
            userInfo.LastReceiveTime = DateTime.Now;
            int startIndex = 0;
            switch (signal) {
                case 0: {
                        byte b = 0;
                        if (userInfo.MachineIdentity.Length == 0 || userInfo.GamePath.Length == 0) {
                            b = 0b1;
                        }
                        int length = BitConverter.ToInt32(buffer, 0);
                        if (length > 0) {
                            string machineIdentity = Encoding.UTF8.GetString(buffer, 4, length);
                            if (!userInfo.MachineIdentity.Equals(machineIdentity)) {
                                userInfo.MachineIdentity = machineIdentity;
                                userInfo.Remark = iniFile.IniReadValue(userInfo.MachineIdentity, "用户备注");
                                iniFile.IniWriteValue(userInfo.MachineIdentity, "用户备注", userInfo.Remark);
                            }
                        }
                        int ciphertextLength = BitConverter.ToInt32(buffer, 4 + length);
                        if (ciphertextLength > 0) {
                            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                            rsa.FromXmlString("<RSAKeyValue><Modulus>2FMpblMWJ5JomZbaj8Y+VYkzviSGpEJn3q5EtSYorN6sbsgSKS8UeJ0AEk8lmNcbgF6F8KzdP7z93EhZRUeqOlPQh+VmrMQ0kUpUdngO0mlJUU6jAhuQd4Hw+NTnZZknKjhWSQFD8e5V3nFYSjsZXlXdGtvukJxsG8RcyLB2Kd0=</Modulus><Exponent>AQAB</Exponent><P>41r456T359znQHgvztfTEZ5xe1Kz6xgkSDC1IpwMDY+dGCd00vhzp0eD6rvRMJQpkVxu+JmVvqFmL8oCX/ybrw==</P><Q>85RqfNiyKhhpeg4XPwRlFSwjNO+I2u3Wc4Qg7JIw9vVnuMsIPeYu0FtNuUu2KrAaANqQ2w2xmR56Cf4WG9q6Mw==</Q><DP>1R8gGDUiVm1TMbH4TtMt/mQiSNJb6dM1n55ZDdptygCH6G6EKofQEk0Nserhy8H3vVWCiPOf1ZUCb2XUGBp57Q==</DP><DQ>fX1lL1Tk1VMmZD+GMm0tNq86pDcUJtaJuZHE9JyMpW7hNQ9E+77vN2EStfgPrgZ0HyR7pJ91dBGDhkplUYxqiQ==</DQ><InverseQ>efPy4erlPKo9yMoUfQvcbCqRUPeSgt0hTCb880Oh+sjd5ILz9kn6lxOUkHVBc7xFTvQKu6XL15vppUtncaIt3Q==</InverseQ><D>skefPoAVIxnDQMkVaSYtWxsO3KaHnDnqFpgyocRIA2gkXcxfQze8vEZPt8coqhSlYp8D7bzZPl1ILlIl2DXesF0iw43F+uXhwRiDCYqRF5azkklIYXlE/93z04h2N15/XyodEgcVx26A040mAOStYTha3a5t6ZFCDHQCZFGJkBU=</D></RSAKeyValue>");
                            byte[] temp = new byte[ciphertextLength];
                            for (int i = 0; i < ciphertextLength; i++) {
                                temp[i] = buffer[8 + length + i];
                            }
                            byte[] plaintext = rsa.Decrypt(temp, true);
                            userInfo.GamePath = Encoding.UTF8.GetString(plaintext);
                            iniFile.IniWriteValue(userInfo.MachineIdentity, "游戏路径", userInfo.GamePath);
                        }
                        int ping = BitConverter.ToInt32(buffer, 8 + length + ciphertextLength);
                        List<byte> sendData = new List<byte>();
                        if (userInfo.MachineIdentity.Length == 0 || userInfo.GamePath.Length == 0) {
                            b |= 0b10;
                        }
                        sendData.AddRange(SerializeTool.RawSerialize(b));
                        if (b == 0) {
                            sendData.AddRange(SerializeTool.RawSerialize(ping));
                            server.SendPackage(id, 0, sendData.ToArray(), 0, sendData.Count);
                        } else {
                            sendData.AddRange(SerializeTool.RawSerialize(userInfo.Id));
                            sendData.AddRange(SerializeTool.RawSerializeForUTF8String(gameData.No1Qin));
                            sendData.AddRange(SerializeTool.RawSerializeForUTF8String(gameData.No2Qin));
                            sendData.AddRange(SerializeTool.RawSerializeForUTF8String(gameData.No3Qin));
                            sendData.AddRange(SerializeTool.RawSerializeForUTF8String(gameData.No4Qin));
                            for (int i = 0; i < gameData.QinKey.Count; i++) {
                                sendData.AddRange(SerializeTool.RawSerialize(gameData.QinKey[i]));
                            }
                            sendData.AddRange(SerializeTool.RawSerializeForUTF8String(gameData.HitQinKey));
                            sendData.AddRange(SerializeTool.RawSerialize(ping));
                            server.SendPackage(id, 0, sendData.ToArray(), 0, sendData.Count);
                        }
                        break;
                    }
                case 1: {
                        gameData.No1Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                        byte[] QinStrByte = SerializeTool.RawSerializeForUTF8String(gameData.No1Qin);
                        for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                            UserInfo tempUserInfo = gameData.ClientInfo.Get(i);
                            if (tempUserInfo.Id != userInfo.Id) {
                                server.SendPackage(tempUserInfo.Id, 1, QinStrByte);
                            }
                        }
                        gameData.Log.InsertAfter(-1, new LogDetail() {
                            Content = userInfo.Remark + " 修改一号琴缺弦为：" + gameData.No1Qin,
                            Time = Environment.TickCount
                        });
                        break;
                    }
                case 2: {
                        gameData.No2Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                        byte[] QinStrByte = SerializeTool.RawSerializeForUTF8String(gameData.No2Qin);
                        for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                            UserInfo tempUserInfo = gameData.ClientInfo.Get(i);
                            if (tempUserInfo.Id != userInfo.Id) {
                                server.SendPackage(tempUserInfo.Id, 2, QinStrByte);
                            }
                        }
                        gameData.Log.InsertAfter(-1, new LogDetail() {
                            Content = userInfo.Remark + " 修改二号琴缺弦为：" + gameData.No2Qin,
                            Time = Environment.TickCount
                        });
                        break;
                    }
                case 3: {
                        gameData.No3Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                        byte[] QinStrByte = SerializeTool.RawSerializeForUTF8String(gameData.No3Qin);
                        for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                            UserInfo tempUserInfo = gameData.ClientInfo.Get(i);
                            if (tempUserInfo.Id != userInfo.Id) {
                                server.SendPackage(tempUserInfo.Id, 3, QinStrByte);
                            }
                        }
                        gameData.Log.InsertAfter(-1, new LogDetail() {
                            Content = userInfo.Remark + " 修改三号琴缺弦为：" + gameData.No3Qin,
                            Time = Environment.TickCount
                        });
                        break;
                    }
                case 4: {
                        gameData.No4Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                        byte[] QinStrByte = SerializeTool.RawSerializeForUTF8String(gameData.No4Qin);
                        for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                            UserInfo tempUserInfo = gameData.ClientInfo.Get(i);
                            if (tempUserInfo.Id != userInfo.Id) {
                                server.SendPackage(tempUserInfo.Id, 4, QinStrByte);
                            }
                        }
                        gameData.Log.InsertAfter(-1, new LogDetail() {
                            Content = userInfo.Remark + " 修改四号琴缺弦为：" + gameData.No4Qin,
                            Time = Environment.TickCount
                        });
                        break;
                    }
                case 5: {
                        List<byte> sendData = new List<byte>(52);
                        int keyIndex = SerializeTool.RawDeserialize<int>(buffer, ref startIndex);
                        int ping = SerializeTool.RawDeserialize<int>(buffer, ref startIndex);
                        for (int i = 0; i < gameData.QinKey.Count; i++) {
                            if (i != keyIndex) {
                                if (gameData.QinKey[i] == userInfo.Id) {
                                    gameData.QinKey[i] = 0;
                                }
                            } else {
                                if (gameData.QinKey[i] == 0) {
                                    gameData.QinKey[i] = userInfo.Id;
                                    gameData.Log.InsertAfter(-1, new LogDetail() {
                                        Content = userInfo.Remark + " 补 " + keyIndex.ToString() + " 琴弦。",
                                        Time = Environment.TickCount
                                    });
                                } else if (gameData.QinKey[i] == userInfo.Id) {
                                    gameData.QinKey[i] = 0;
                                    gameData.Log.InsertAfter(-1, new LogDetail() {
                                        Content = userInfo.Remark + " 放弃补 " + keyIndex.ToString() + " 琴弦。",
                                        Time = Environment.TickCount
                                    });
                                } else {
                                    gameData.Log.InsertAfter(-1, new LogDetail() {
                                        Content = userInfo.Remark + " 尝试补 " + keyIndex.ToString() + " 琴弦但冲突。",
                                        Time = Environment.TickCount
                                    });
                                }
                            }
                            sendData.AddRange(SerializeTool.RawSerialize(gameData.QinKey[i]));
                        }
                        sendData.AddRange(SerializeTool.RawSerialize(keyIndex));
                        sendData.AddRange(SerializeTool.RawSerialize(ping));
                        byte[] data = sendData.ToArray();
                        for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                            UserInfo tempUserInfo = gameData.ClientInfo.Get(i);
                            if (tempUserInfo.Id != userInfo.Id) {
                                server.SendPackage(tempUserInfo.Id, 6, data, 0, 48);
                            } else {
                                server.SendPackage(tempUserInfo.Id, 7, data);
                            }
                        }
                        break;
                    }
                case 6: {
                        if (!Directory.Exists(@".\Pic")) {
                            Directory.CreateDirectory(@".\Pic");
                        }
                        userInfo.PicPath = string.Format(".\\Pic\\{0}-{1}-{2}-{3}.jpg", DateTime.Now.ToString("yyyy年MM月dd日HH时mm分ss秒"), userInfo.MachineIdentity, Environment.TickCount, new Random().Next());
                        userInfo.PicPathStream = File.Create(userInfo.PicPath);
                        long fileMaxLength = SerializeTool.RawDeserialize<long>(buffer, ref startIndex);
                        int ping = SerializeTool.RawDeserialize<int>(buffer, ref startIndex);
                        userInfo.PicPathStream.SetLength(fileMaxLength);
                        List<byte> sendData = new List<byte>(12);
                        sendData.AddRange(SerializeTool.RawSerialize(userInfo.PicPathStream.Position));
                        sendData.AddRange(SerializeTool.RawSerialize(ping));
                        server.SendPackage(userInfo.Id, 10, sendData.ToArray());
                        break;
                    }
                case 7: {
                        long position = SerializeTool.RawDeserialize<long>(buffer, ref startIndex);
                        int ping = SerializeTool.RawDeserialize<int>(buffer, ref startIndex);
                        if (position == userInfo.PicPathStream.Position) {
                            int count = buffer.Length - startIndex;
                            List<byte> sendData = new List<byte>(12);
                            sendData.AddRange(SerializeTool.RawSerialize(position + count));
                            sendData.AddRange(SerializeTool.RawSerialize(ping));
                            server.SendPackage(userInfo.Id, 10, sendData.ToArray());
                            userInfo.PicPathStream.Write(buffer, startIndex, count);
                            if (userInfo.PicPathStream.Position == userInfo.PicPathStream.Length) {
                                userInfo.PicPathStream.Close();
                                userInfo.PicPathStream = null;
                            }
                        } else {
                            Debug.WriteLine("丢弃了数据包！");
                        }
                        break;
                    }
                case 8: {
                        if (!Directory.Exists(@".\Png")) {
                            Directory.CreateDirectory(@".\Png");
                        }
                        userInfo.PngPath = string.Format(".\\Png\\{0}-{1}-{2}-{3}.png", DateTime.Now.ToString("yyyy年MM月dd日HH时mm分ss秒"), userInfo.MachineIdentity, Environment.TickCount, new Random().Next());
                        //userInfo.BmpPath = string.Format(".\\Bmp\\{0}-{1}-{2}-{3}.gzip", DateTime.Now.ToString("yyyy年MM月dd日HH时mm分ss秒"), userInfo.MachineIdentity, Environment.TickCount, new Random().Next());
                        //userInfo.BmpPath = string.Format(".\\Bmp\\{0}-{1}-{2}-{3}.bmp", DateTime.Now.ToString("yyyy年MM月dd日HH时mm分ss秒"), userInfo.MachineIdentity, Environment.TickCount, new Random().Next());
                        userInfo.PngPathStream = File.Create(userInfo.PngPath);
                        long fileMaxLength = SerializeTool.RawDeserialize<long>(buffer, ref startIndex);
                        int ping = SerializeTool.RawDeserialize<int>(buffer, ref startIndex);
                        userInfo.PngPathStream.SetLength(fileMaxLength);
                        List<byte> sendData = new List<byte>(12);
                        sendData.AddRange(SerializeTool.RawSerialize(userInfo.PngPathStream.Position));
                        sendData.AddRange(SerializeTool.RawSerialize(ping));
                        server.SendPackage(userInfo.Id, 12, sendData.ToArray());
                        break;
                    }
                case 9: {
                        long position = SerializeTool.RawDeserialize<long>(buffer, ref startIndex);
                        int ping = SerializeTool.RawDeserialize<int>(buffer, ref startIndex);
                        if (position == userInfo.PngPathStream.Position) {
                            int count = buffer.Length - startIndex;
                            List<byte> sendData = new List<byte>(12);
                            sendData.AddRange(SerializeTool.RawSerialize(position + count));
                            sendData.AddRange(SerializeTool.RawSerialize(ping));
                            server.SendPackage(userInfo.Id, 12, sendData.ToArray());
                            userInfo.PngPathStream.Write(buffer, startIndex, count);
                            if (userInfo.PngPathStream.Position == userInfo.PngPathStream.Length) {
                                userInfo.PngPathStream.Close();
                                userInfo.PngPathStream = null;
                            }
                        } else {
                            Debug.WriteLine("丢弃了数据包！");
                        }
                        break;
                    }
                case 10: {
                        int color = SerializeTool.RawDeserialize<int>(buffer, ref startIndex);
                        _ = ThreadPool.QueueUserWorkItem(delegate {
                            _ = Dispatcher.Invoke(new Action(() => {
                                colorTextBox.Text += userInfo.Id.ToString() + ":" + color.ToString() + "|";
                            }), null);
                        });
                        break;
                    }
                case 11: {
                        userInfo.KillingIntentionStrip = SerializeTool.RawDeserialize<int>(buffer, ref startIndex);
                        iniFile.IniWriteValue(userInfo.MachineIdentity, "杀意高度", userInfo.KillingIntentionStrip.ToString());
                        break;
                    }
                case 12: {
                        int index = SerializeTool.RawDeserialize<int>(buffer, ref startIndex);
                        int position = SerializeTool.RawDeserialize<int>(buffer, ref startIndex);
                        userInfo.FiveTone[index] = position;
                        userInfo.FiveTone = userInfo.FiveTone;
                        switch (index) {
                            case 0:
                                iniFile.IniWriteValue(userInfo.MachineIdentity, "宫", position.ToString());
                                break;
                            case 1:
                                iniFile.IniWriteValue(userInfo.MachineIdentity, "商", position.ToString());
                                break;
                            case 2:
                                iniFile.IniWriteValue(userInfo.MachineIdentity, "角", position.ToString());
                                break;
                            case 3:
                                iniFile.IniWriteValue(userInfo.MachineIdentity, "徵", position.ToString());
                                break;
                            case 4:
                                iniFile.IniWriteValue(userInfo.MachineIdentity, "羽", position.ToString());
                                break;
                            default:
                                break;
                        }
                        break;
                    }
                case 13: {
                        string lessKey = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                        gameData.Log.InsertAfter(-1, new LogDetail() {
                            Content = userInfo.Remark + " 推测缺 " + lessKey,
                            Time = Environment.TickCount
                        });
                        break;
                    }
                case 14: {
                        gameData.HitQinKey = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                        for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                            UserInfo tempUserInfo = gameData.ClientInfo.Get(i);
                            server.SendPackage(tempUserInfo.Id, 8, buffer);
                        }
                        break;
                    }
                case 15: {
                        gameData.No1Qin = gameData.No2Qin = gameData.No3Qin = gameData.No4Qin = gameData.HitQinKey = "";
                        for (int i = 0; i < 12; i++) {
                            gameData.QinKey[i] = 0;
                        }
                        for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                            UserInfo tempUserInfo = gameData.ClientInfo.Get(i);
                            server.SendPackage(tempUserInfo.Id, 5, SerializeTool.RawSerialize(tempUserInfo.Id));
                        }
                        break;
                    }
                case 16: {
                        string lessKey = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                        string lastLessKey = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                        gameData.Log.InsertAfter(-1, new LogDetail() {
                            Content = userInfo.Remark + " 推测缺 " + lastLessKey + " ，但经过核对后缺 " + lessKey,
                            Time = Environment.TickCount
                        });
                        break;
                    }
                default: {
                        Debug.WriteLine("error");
                        break;
                    }
            }
        }
        private void OnLeave(int id, object userToken) {
            Debug.WriteLine(id.ToString());
            _ = ThreadPool.QueueUserWorkItem(delegate {
                Dispatcher.Invoke(() => {
                    for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                        if (gameData.ClientInfo.Get(i).Id == id) {
                            gameData.Log.InsertAfter(-1, new LogDetail() {
                                Content = "客户 " + id.ToString() + "离开。备注：" + gameData.ClientInfo.Get(i).Remark,
                                Time = Environment.TickCount
                            });
                            gameData.ClientInfo.Del(i);
                        }
                    }
                    Debug.WriteLine("leave");
                });
            });
        }
        private void Button_Click(object sender, RoutedEventArgs e) {
            contextMenuStrip.Show();
        }
        private void ListViewItem_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            if (sender is ListViewItem item) {
                if (item.Content is UserInfo userInfo) {
                    Point point = e.GetPosition(this);
                    point = PointToScreen(point);
                    contextMenuStrip.Show((int)point.X, (int)point.Y);
                    menuUser = userInfo;
                    e.Handled = true;
                }
            }
        }
        private void TextBox_SourceUpdated(object sender, DataTransferEventArgs e) {
            System.Windows.Controls.TextBox sourceTextBox = (System.Windows.Controls.TextBox)e.Source;
            UserInfo userInfo = ((ContentPresenter)sourceTextBox.TemplatedParent).Content as UserInfo;
            iniFile.IniWriteValue(userInfo.MachineIdentity, "用户备注", sourceTextBox.Text);
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                server.SendPackage(gameData.ClientInfo.Get(i).Id, 13, null);
            }
        }
    }
}