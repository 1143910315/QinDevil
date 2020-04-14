using QinDevilCommon.Data;
using QinDevilCommon;
using QinDevilCommon.Keyboard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QinDevilServer {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        private SocketServer server;
        private readonly GameData gameData = new GameData();
        private KeyboardHook hook = new KeyboardHook();
        private bool ctrlState;
        public MainWindow() {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            /*RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            Debug.WriteLine(rsa.ToXmlString(true));
            Debug.WriteLine(rsa.ToXmlString(false));*/
            //rsa.FromXmlString("");
            server = new SocketServer(13748) {
                onAcceptSuccessEvent = OnAcceptSuccess,
                onReceiveOriginalDataEvent = OnReceiveOriginalData,
                onReceivePackageEvent = OnReceivePackage,
                onLeaveEvent = OnLeave
            };
            GamePanel.DataContext = gameData;
            hook.KeyDownEvent += KeyDownCallbak;
            hook.KeyUpEvent += KeyUpCallbak;
            /*Regex regex = new Regex("^\\d{1,3}.\\d{1,3}.\\d{1,3}.\\d{1,3}:\\d+$");
            bool t = regex.IsMatch("192.168.1.1:12345");*/
        }
        private void KeyDownCallbak(KeyCode keyCode) {
            switch (keyCode) {
                case KeyCode.VK_LCONTROL:
                    ctrlState = true;
                    break;
                case KeyCode.Numeric7:
                    if (ctrlState) {
                        gameData.State = GameData.State_LeakHunting;
                    }
                    break;
                case KeyCode.Numeric8:
                    if (ctrlState) {
                        gameData.State = GameData.State_HitKey;
                    }
                    break;
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
            /*connectNum++;
            shouConnectNumber.Dispatcher.Invoke(() => {
                shouConnectNumber.Content = connectNum.ToString();
            });*/
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
            /*
            if (userToken is UserInfo userInfo) {
                if (userInfo.IpAndPort == null) {
                    try {
                        UTF8Encoding utf8 = new UTF8Encoding(false, true);
                        string s = utf8.GetString(buffer, offest, count);
                        Regex regex = new Regex("^\\d{1,3}.\\d{1,3}.\\d{1,3}.\\d{1,3}:\\d+$");
                        if (regex.IsMatch(s)) {
                            userInfo.IpAndPort = s;
                            return true;
                        }
                    } catch (Exception) {
                    }
                }
            }
            */
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
            switch (signal) {
                case 0: {
                        int length = BitConverter.ToInt32(buffer, 0);
                        if (length > 0) {
                            userInfo.MachineIdentity = Encoding.UTF8.GetString(buffer, 4, length);
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
                        }
                        List<byte> sendData = new List<byte>();
                        sendData.AddRange(SerializeTool.RawSerialize(userInfo.Id));
                        sendData.AddRange(SerializeTool.RawSerializeForUTF8String(gameData.No1Qin));
                        sendData.AddRange(SerializeTool.RawSerializeForUTF8String(gameData.No2Qin));
                        sendData.AddRange(SerializeTool.RawSerializeForUTF8String(gameData.No3Qin));
                        sendData.AddRange(SerializeTool.RawSerializeForUTF8String(gameData.No4Qin));
                        server.SendPackage(id, 0, sendData.ToArray(), 0, sendData.Count);
                        break;
                    }
                case 1: {
                        int startIndex = 0;
                        gameData.No1Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                        for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                            UserInfo tempUserInfo = gameData.ClientInfo.Get(i);
                            if (tempUserInfo.Id != userInfo.Id) {
                                server.SendPackage(tempUserInfo.Id, 1, SerializeTool.RawSerializeForUTF8String(gameData.No1Qin));
                            }
                        }
                        break;
                    }
                case 2: {
                        int startIndex = 0;
                        gameData.No2Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                        for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                            UserInfo tempUserInfo = gameData.ClientInfo.Get(i);
                            if (tempUserInfo.Id != userInfo.Id) {
                                server.SendPackage(tempUserInfo.Id, 2, SerializeTool.RawSerializeForUTF8String(gameData.No2Qin));
                            }
                        }
                        break;
                    }
                case 3: {
                        int startIndex = 0;
                        gameData.No3Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                        for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                            UserInfo tempUserInfo = gameData.ClientInfo.Get(i);
                            if (tempUserInfo.Id != userInfo.Id) {
                                server.SendPackage(tempUserInfo.Id, 3, SerializeTool.RawSerializeForUTF8String(gameData.No3Qin));
                            }
                        }
                        break;
                    }
                case 4: {
                        int startIndex = 0;
                        gameData.No4Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                        for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                            UserInfo tempUserInfo = gameData.ClientInfo.Get(i);
                            if (tempUserInfo.Id != userInfo.Id) {
                                server.SendPackage(tempUserInfo.Id, 4, SerializeTool.RawSerializeForUTF8String(gameData.No4Qin));
                            }
                        }
                        break;
                    }
                default:
                    break;
            }
            /*shouConnectNumber.Dispatcher.Invoke(() => {
                shouConnectNumber.Content = string.Format("信号值：{0}\n消息内容：{1}", signal, Encoding.ASCII.GetString(buffer));
            });*/
        }
        private void OnLeave(int id, object userToken) {
            /*connectNum--;
            shouConnectNumber.Dispatcher.Invoke(() => {
                shouConnectNumber.Content = connectNum.ToString();
            });*/
            _ = ThreadPool.QueueUserWorkItem(delegate {
                _ = Dispatcher.Invoke(new Action(() => {
                    for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                        if (gameData.ClientInfo.Get(i).Id == id) {
                            gameData.ClientInfo.Del(i);
                        }
                    }
                }), null);
            });
        }
        private void Button_Click(object sender, RoutedEventArgs e) {
            /*
            byte[] v = Encoding.ASCII.GetBytes(magEdit.Text);
            server.SendPackage(0, int.Parse(signalEdit.Text), v, 0, v.Length);*/
            gameData.ClientInfo.InsertAfter(-1, new UserInfo() {
                Id = int.Parse(signalEdit.Text),
                LastReceiveTime = DateTime.Now
            });
        }
    }
}
