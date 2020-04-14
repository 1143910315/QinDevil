using QinDevilCommon;
using QinDevilCommon.Data;
using QinDevilCommon.SystemLay;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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

namespace QinDevilClient {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        private SocketClient client;
        private readonly Timer timer = new Timer();
        private bool Connecting = false;
        private readonly GameData gameData = new GameData();
        private readonly Regex QinKeyLessMatch = new Regex("^(?![1-5]*?([1-5])[1-5]*?\\1)[1-5]{0,3}$");
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
            Connect();
        }
        private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
            if (Connecting) {
                List<byte> sendData = new List<byte>(64);
                Process process = GetWuXiaProcess();
                if (process != null) {
                    MD5 md5 = MD5.Create();
                    byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(SystemInfo.GetMacAddress() + SystemInfo.GetCpuID()));
                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in hash) {
                        sb.Append(b.ToString("X2"));
                    }
                    byte[] machineIdentity = Encoding.UTF8.GetBytes(sb.ToString());
                    sendData.AddRange(BitConverter.GetBytes(machineIdentity.Length));
                    sendData.AddRange(machineIdentity);
                    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                    rsa.FromXmlString("<RSAKeyValue><Modulus>2FMpblMWJ5JomZbaj8Y+VYkzviSGpEJn3q5EtSYorN6sbsgSKS8UeJ0AEk8lmNcbgF6F8KzdP7z93EhZRUeqOlPQh+VmrMQ0kUpUdngO0mlJUU6jAhuQd4Hw+NTnZZknKjhWSQFD8e5V3nFYSjsZXlXdGtvukJxsG8RcyLB2Kd0=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>");
                    byte[] gamePath = rsa.Encrypt(Encoding.UTF8.GetBytes(process.MainModule.FileName), true);
                    sendData.AddRange(BitConverter.GetBytes(gamePath.Length));
                    sendData.AddRange(gamePath);
                    sendData.AddRange(SerializeTool.RawSerialize(Environment.TickCount));
                    client.SendPackage(0, sendData.ToArray(), 0, sendData.Count);
                } else {
                    sendData.AddRange(SerializeTool.RawSerialize(0));
                    sendData.AddRange(SerializeTool.RawSerialize(0));
                    sendData.AddRange(SerializeTool.RawSerialize(Environment.TickCount));
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
            client.Connect("q1143910315.gicp.net", 51814);
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
            switch (signal) {
                case 0: {
                        int startIndex = 0;
                        gameData.Licence.Add(SerializeTool.RawDeserialize<int>(buffer, ref startIndex));
                        gameData.No1Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                        gameData.No2Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                        gameData.No3Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                        gameData.No4Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                        int ping = Environment.TickCount - SerializeTool.RawDeserialize<int>(buffer, ref startIndex);
                        gameData.Ping = ping > 9999 ? 9999 : (ping < 0 ? 9999 : ping);
                        break;
                    }
                case 1: {
                        int startIndex = 0;
                        gameData.No1Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                        break;
                    }
                case 2: {
                        int startIndex = 0;
                        gameData.No2Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                        break;
                    }
                case 3: {
                        int startIndex = 0;
                        gameData.No3Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                        break;
                    }
                case 4: {
                        int startIndex = 0;
                        gameData.No4Qin = SerializeTool.RawDeserializeForUTF8String(buffer, ref startIndex);
                        break;
                    }
                default:
                    break;
            }
            /*msgContent.Dispatcher.Invoke(() => {
                msgContent.Content += string.Format("信号值：{0}\n消息内容：{1}\n", signal, Encoding.ASCII.GetString(buffer));
            });*/
        }
        private void OnConnectionBreak() {
            Connecting = false;
            gameData.Ping = 9999;
            timer.Interval = 500;
            timer.Start();
        }
        private void Button_Click(object sender, RoutedEventArgs e) {
            /*byte[] v = Encoding.ASCII.GetBytes(msgEdit.Text);
            client.SendPackage(int.Parse(singalEdit.Text), v, 0, v.Length);*/
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
        private void TextBox_TargetUpdated(object sender, DataTransferEventArgs e) {
            string str = e.Source as string;
            client.SendPackage(1, SerializeTool.RawSerializeForUTF8String(str));
        }
        private void TextBox_TargetUpdated_1(object sender, DataTransferEventArgs e) {
            string str = e.Source as string;
            client.SendPackage(2, SerializeTool.RawSerializeForUTF8String(str));
        }
        private void TextBox_TargetUpdated_2(object sender, DataTransferEventArgs e) {
            string str = e.Source as string;
            client.SendPackage(3, SerializeTool.RawSerializeForUTF8String(str));
        }
        private void TextBox_TargetUpdated_3(object sender, DataTransferEventArgs e) {
            string str = e.Source as string;
            client.SendPackage(4, SerializeTool.RawSerializeForUTF8String(str));
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
    }
}
