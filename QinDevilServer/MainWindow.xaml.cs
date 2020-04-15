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
                case KeyCode.VK_LCONTROL: {
                        ctrlState = true;
                        break;
                    }
                case KeyCode.Numeric1: {
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
            try {
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
                            int ping = BitConverter.ToInt32(buffer, 8 + length + ciphertextLength);
                            List<byte> sendData = new List<byte>();
                            sendData.AddRange(SerializeTool.RawSerialize(userInfo.Id));
                            sendData.AddRange(SerializeTool.RawSerializeForUTF8String(gameData.No1Qin));
                            sendData.AddRange(SerializeTool.RawSerializeForUTF8String(gameData.No2Qin));
                            sendData.AddRange(SerializeTool.RawSerializeForUTF8String(gameData.No3Qin));
                            sendData.AddRange(SerializeTool.RawSerializeForUTF8String(gameData.No4Qin));
                            for (int i = 0; i < gameData.QinKey.Count; i++) {
                                sendData.AddRange(SerializeTool.RawSerialize(gameData.QinKey[i]));
                            }
                            sendData.AddRange(SerializeTool.RawSerialize(ping));
                            server.SendPackage(id, 0, sendData.ToArray(), 0, sendData.Count);
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
                                    } else if (gameData.QinKey[i] == userInfo.Id) {
                                        gameData.QinKey[i] = 0;
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
                    default:
                        break;
                }
            } catch (Exception) {
            }
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
