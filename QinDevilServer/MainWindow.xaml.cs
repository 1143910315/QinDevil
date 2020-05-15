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
using System.Linq;

namespace QinDevilServer {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        private readonly SocketServer server;
        private readonly List<GameData> gameData;
        private readonly List<byte> sendData = new List<byte>();
        private readonly KeyboardHook hook = new KeyboardHook();
        private readonly IniFile iniFile = new IniFile(AppDomain.CurrentDomain.BaseDirectory + "用户记录.ini");
        private bool ctrlState;
        private readonly ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
        private UserInfo menuUser;
        private int current = 0;
        public MainWindow() {
            gameData = new List<GameData> {
                new GameData()
            };
            InitializeComponent();
            contextMenuStrip.Items.Add("请求所有玩家屏幕截图（JPG）").Click += PrintScreenAll_Click;
            contextMenuStrip.Items.Add("请求当前玩家截图（JPG）").Click += PrintScreen_Click;
            contextMenuStrip.Items.Add("请求当前玩家截图（PNG）").Click += PrintScreenHighQuality_Click;
            contextMenuStrip.Items.Add("查看当前玩家截图").Click += PictureViewer_Click;
            contextMenuStrip.Items.Add("清除当前玩家游戏路径").Click += ClearGamePath_Click;
            contextMenuStrip.Items.Add("扫描当前玩家缺弦").Click += Scanning_Click;
            contextMenuStrip.Items.Add("判断当前玩家杀意条").Click += KillingIntentionStrip_Click;
            contextMenuStrip.Items.Add("断开当前玩家").Click += CloseClient_Click;
            server = new SocketServer();
            server.OnAcceptSuccessEvent += OnAcceptSuccess;
            server.OnReceivePackageEvent += OnReceivePackage;
            server.OnLeaveEvent += OnLeave;
            server.Start(12148);
            hook.KeyDownEvent += KeyDownCallbak;
            hook.KeyUpEvent += KeyUpCallbak;
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
            gameData[current].ClientInfoLock.EnterReadLock();
            try {
                foreach (UserInfo userInfo in gameData[current].ClientInfo) {
                    server.SendPackage(userInfo.Id, 9, null);
                }
            } finally {
                gameData[current].ClientInfoLock.ExitReadLock();
            }
        }
        private void AllowAutoPlay_Click(object sender, RoutedEventArgs e) {
            if (sender is MenuItem menuItem) {
                byte[] sendData = new byte[1];
                sendData[0] = menuItem.IsChecked ? (byte)1 : (byte)0;
                gameData[current].ClientInfoLock.EnterReadLock();
                try {
                    foreach (UserInfo userInfo in gameData[current].ClientInfo) {
                        server.SendPackage(userInfo.Id, 15, sendData);
                    }
                } finally {
                    gameData[current].ClientInfoLock.ExitReadLock();
                }
            }
        }
        private void AllowAutoLessKey_Click(object sender, RoutedEventArgs e) {
            if (sender is MenuItem menuItem) {
                byte[] sendData = new byte[1];
                sendData[0] = menuItem.IsChecked ? (byte)1 : (byte)0;
                gameData[current].ClientInfoLock.EnterReadLock();
                try {
                    foreach (UserInfo userInfo in gameData[current].ClientInfo) {
                        server.SendPackage(userInfo.Id, 14, sendData);
                    }
                } finally {
                    gameData[current].ClientInfoLock.ExitReadLock();
                }
            }
        }
        private void PrintScreenAll_Click(object sender, EventArgs e) {
            gameData[current].ClientInfoLock.EnterReadLock();
            try {
                foreach (UserInfo userInfo in gameData[current].ClientInfo) {
                    server.SendPackage(userInfo.Id, 9, null);
                }
            } finally {
                gameData[current].ClientInfoLock.ExitReadLock();
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
            gameData[current].LogLock.EnterReadLock();
            try {
                new LogViewerWindow(gameData[current].Log.Select(logDetail => logDetail).ToArray()).Show();
            } finally {
                gameData[current].LogLock.ExitReadLock();
            }
        }
        private void KeyDownCallbak(KeyCode keyCode) {
            switch (keyCode) {
                case KeyCode.VK_LCONTROL:
                    ctrlState = true;
                    break;
                case KeyCode.Numeric1:
                    PopLog(current, "数字键 1被按下，" + (ctrlState ? "按下了ctrl。" : "没按ctrl。"));
                    if (ctrlState) {
                        int i = 0;
                        for (; i < gameData[current].HitQinKey.Length; i++) {
                            if (gameData[current].HitQinKey[i] == 0) {
                                break;
                            }
                        }
                        if (i < gameData[current].HitQinKey.Length) {
                            gameData[current].HitQinKey[i++] = 1;
                            if (i < gameData[current].HitQinKey.Length) {
                                gameData[current].HitQinKey[i] = 0;
                            }
                        }
                        gameData[current].ClientInfoLock.EnterReadLock();
                        try {
                            foreach (UserInfo userInfo in gameData[current].ClientInfo) {
                                server.SendPackage(userInfo.Id, 8, gameData[current].HitQinKey);
                            }
                        } finally {
                            gameData[current].ClientInfoLock.ExitReadLock();
                        }
                    }
                    break;
                case KeyCode.Numeric2:
                    PopLog(current, "数字键 2被按下，" + (ctrlState ? "按下了ctrl。" : "没按ctrl。"));
                    if (ctrlState) {
                        int i = 0;
                        for (; i < gameData[current].HitQinKey.Length; i++) {
                            if (gameData[current].HitQinKey[i] == 0) {
                                break;
                            }
                        }
                        if (i < gameData[current].HitQinKey.Length) {
                            gameData[current].HitQinKey[i++] = 2;
                            if (i < gameData[current].HitQinKey.Length) {
                                gameData[current].HitQinKey[i] = 0;
                            }
                        }
                        gameData[current].ClientInfoLock.EnterReadLock();
                        try {
                            foreach (UserInfo userInfo in gameData[current].ClientInfo) {
                                server.SendPackage(userInfo.Id, 8, gameData[current].HitQinKey);
                            }
                        } finally {
                            gameData[current].ClientInfoLock.ExitReadLock();
                        }
                    }
                    break;
                case KeyCode.Numeric3:
                    PopLog(current, "数字键 3被按下，" + (ctrlState ? "按下了ctrl。" : "没按ctrl。"));
                    if (ctrlState) {
                        int i = 0;
                        for (; i < gameData[current].HitQinKey.Length; i++) {
                            if (gameData[current].HitQinKey[i] == 0) {
                                break;
                            }
                        }
                        if (i < gameData[current].HitQinKey.Length) {
                            gameData[current].HitQinKey[i++] = 3;
                            if (i < gameData[current].HitQinKey.Length) {
                                gameData[current].HitQinKey[i] = 0;
                            }
                        }
                        gameData[current].ClientInfoLock.EnterReadLock();
                        try {
                            foreach (UserInfo userInfo in gameData[current].ClientInfo) {
                                server.SendPackage(userInfo.Id, 8, gameData[current].HitQinKey);
                            }
                        } finally {
                            gameData[current].ClientInfoLock.ExitReadLock();
                        }
                    }
                    break;
                case KeyCode.Numeric4:
                    PopLog(current, "数字键 4被按下，" + (ctrlState ? "按下了ctrl。" : "没按ctrl。"));
                    if (ctrlState) {
                        int i = 0;
                        for (; i < gameData[current].HitQinKey.Length; i++) {
                            if (gameData[current].HitQinKey[i] == 0) {
                                break;
                            }
                        }
                        if (i < gameData[current].HitQinKey.Length) {
                            gameData[current].HitQinKey[i++] = 4;
                            if (i < gameData[current].HitQinKey.Length) {
                                gameData[current].HitQinKey[i] = 0;
                            }
                        }
                        gameData[current].ClientInfoLock.EnterReadLock();
                        try {
                            foreach (UserInfo userInfo in gameData[current].ClientInfo) {
                                server.SendPackage(userInfo.Id, 8, gameData[current].HitQinKey);
                            }
                        } finally {
                            gameData[current].ClientInfoLock.ExitReadLock();
                        }
                    }
                    break;
                case KeyCode.Numeric5:
                    PopLog(current, "数字键 5被按下，" + (ctrlState ? "按下了ctrl。" : "没按ctrl。"));
                    if (ctrlState) {
                        int i = 0;
                        for (; i < gameData[current].HitQinKey.Length; i++) {
                            if (gameData[current].HitQinKey[i] == 0) {
                                break;
                            }
                        }
                        if (i < gameData[current].HitQinKey.Length) {
                            gameData[current].HitQinKey[i++] = 5;
                            if (i < gameData[current].HitQinKey.Length) {
                                gameData[current].HitQinKey[i] = 0;
                            }
                        }
                        gameData[current].ClientInfoLock.EnterReadLock();
                        try {
                            foreach (UserInfo userInfo in gameData[current].ClientInfo) {
                                server.SendPackage(userInfo.Id, 8, gameData[current].HitQinKey);
                            }
                        } finally {
                            gameData[current].ClientInfoLock.ExitReadLock();
                        }
                    }
                    break;
                case KeyCode.Numeric7:
                    if (ctrlState) {
                        PopLog(current, "补弦清屏---------------------");
                        gameData[current].No1Qin = gameData[current].No2Qin = gameData[current].No3Qin = gameData[current].No4Qin = "";
                        gameData[current].HitQinKey[0] = 0;
                        for (int i = 0; i < 12; i++) {
                            gameData[current].QinKey[i] = 0;
                        }
                        gameData[current].ClientInfoLock.EnterReadLock();
                        try {
                            byte[] intByte = new byte[4];
                            foreach (UserInfo userInfo in gameData[current].ClientInfo) {
                                SerializeTool.IntToByte(userInfo.Id, intByte, 0);
                                server.SendPackage(userInfo.Id, 5, intByte);
                            }
                        } finally {
                            gameData[current].ClientInfoLock.ExitReadLock();
                        }
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
            return Dispatcher.Invoke(() => {
                PopLog(0, "客户 " + id.ToString() + "进入。");
                gameData[0].ClientInfoLock.EnterWriteLock();
                try {
                    return gameData[0].ClientInfo.AddLast(new UserInfo() {
                        Id = id,
                        LastReceiveTime = DateTime.Now
                    });
                } finally {
                    gameData[0].ClientInfo.ChangeComplete();
                    gameData[0].ClientInfoLock.ExitWriteLock();
                }
            });
        }
        private void OnReceivePackage(int id, int signal, byte[] buffer, object userToken) {
            if (userToken is LinkedListNode<UserInfo> userInfoNode) {
                UserInfo userInfo = userInfoNode.Value;
                userInfo.LastReceiveTime = DateTime.Now;
                int startIndex = 0;
                switch (signal) {
                    case 0:
                        int line = SerializeTool.ByteToInt(buffer, ref startIndex);
                        Dispatcher.Invoke(() => {
                            lock (gameData) {
                                int i = 0;
                                for (; i < gameData.Count; i++) {
                                    if (line == gameData[i].Line) {
                                        if (userInfo.Line != i) {
                                            gameData[userInfo.Line].ClientInfoLock.EnterWriteLock();
                                            try {
                                                gameData[userInfo.Line].ClientInfo.Remove(userInfoNode);
                                            } finally {
                                                gameData[userInfo.Line].ClientInfo.ChangeComplete();
                                                gameData[userInfo.Line].ClientInfoLock.ExitWriteLock();
                                            }
                                            userInfo.Line = i;
                                            gameData[userInfo.Line].ClientInfoLock.EnterWriteLock();
                                            try {
                                                gameData[userInfo.Line].ClientInfo.AddLast(userInfoNode);
                                            } finally {
                                                gameData[userInfo.Line].ClientInfo.ChangeComplete();
                                                gameData[userInfo.Line].ClientInfoLock.ExitWriteLock();
                                            }
                                        }
                                        break;
                                    }
                                }
                                if (i == gameData.Count) {
                                    gameData.Add(new GameData() {
                                        Line = line
                                    });
                                    gameData[userInfo.Line].ClientInfoLock.EnterWriteLock();
                                    try {
                                        gameData[userInfo.Line].ClientInfo.Remove(userInfoNode);
                                    } finally {
                                        gameData[userInfo.Line].ClientInfo.ChangeComplete();
                                        gameData[userInfo.Line].ClientInfoLock.ExitWriteLock();
                                    }
                                    userInfo.Line = i;
                                    gameData[userInfo.Line].ClientInfoLock.EnterWriteLock();
                                    try {
                                        gameData[userInfo.Line].ClientInfo.AddLast(userInfoNode);
                                    } finally {
                                        gameData[userInfo.Line].ClientInfo.ChangeComplete();
                                        gameData[userInfo.Line].ClientInfoLock.ExitWriteLock();
                                    }
                                    _ = chooseLine.Items.Add(new ComboBoxItem() { Content = "线路" + i.ToString(), Tag = i.ToString() });
                                }
                            }
                        });
                        string machineIdentity = SerializeTool.ByteToString(buffer, ref startIndex);
                        if (machineIdentity.Length > 0 && !userInfo.MachineIdentity.Equals(machineIdentity)) {
                            userInfo.MachineIdentity = machineIdentity;
                            userInfo.Remark = iniFile.IniReadValue(userInfo.MachineIdentity, "用户备注");
                            iniFile.IniWriteValue(userInfo.MachineIdentity, "用户备注", userInfo.Remark);
                        }
                        userInfo.GamePath = SerializeTool.ByteToString(buffer, ref startIndex);
                        iniFile.IniWriteValue(userInfo.MachineIdentity, "游戏路径", userInfo.GamePath);
                        int ping = SerializeTool.ByteToInt(buffer, ref startIndex);
                        lock (sendData) {
                            sendData.Clear();
                            SerializeTool.IntToByteList(userInfo.Id, sendData);
                            SerializeTool.StringToByteList(gameData[userInfo.Line].No1Qin, sendData);
                            SerializeTool.StringToByteList(gameData[userInfo.Line].No2Qin, sendData);
                            SerializeTool.StringToByteList(gameData[userInfo.Line].No3Qin, sendData);
                            SerializeTool.StringToByteList(gameData[userInfo.Line].No4Qin, sendData);
                            for (int i = 0; i < gameData[userInfo.Line].QinKey.Count; i++) {
                                SerializeTool.IntToByteList(gameData[userInfo.Line].QinKey[i], sendData);
                            }
                            sendData.AddRange(gameData[userInfo.Line].HitQinKey);
                            SerializeTool.IntToByteList(ping, sendData);
                            server.SendPackage(id, 0, sendData.ToArray());
                        }
                        break;
                    case 1:
                        gameData[userInfo.Line].No1Qin = SerializeTool.ByteToString(buffer, ref startIndex);
                        gameData[userInfo.Line].ClientInfoLock.EnterReadLock();
                        try {
                            foreach (UserInfo tempUserInfo in gameData[userInfo.Line].ClientInfo) {
                                if (tempUserInfo.Id != id) {
                                    server.SendPackage(tempUserInfo.Id, 1, buffer);
                                }
                            }
                        } finally {
                            gameData[userInfo.Line].ClientInfoLock.ExitReadLock();
                        }
                        ExpandLog(userInfo.Line, userInfo.Remark + " 修改一号琴缺弦为：" + gameData[userInfo.Line].No1Qin);
                        break;
                    case 2:
                        gameData[userInfo.Line].No2Qin = SerializeTool.ByteToString(buffer, ref startIndex);
                        gameData[userInfo.Line].ClientInfoLock.EnterReadLock();
                        try {
                            foreach (UserInfo tempUserInfo in gameData[userInfo.Line].ClientInfo) {
                                if (tempUserInfo.Id != id) {
                                    server.SendPackage(tempUserInfo.Id, 2, buffer);
                                }
                            }
                        } finally {
                            gameData[userInfo.Line].ClientInfoLock.ExitReadLock();
                        }
                        ExpandLog(userInfo.Line, userInfo.Remark + " 修改二号琴缺弦为：" + gameData[userInfo.Line].No2Qin);
                        break;
                    case 3:
                        gameData[userInfo.Line].No3Qin = SerializeTool.ByteToString(buffer, ref startIndex);
                        gameData[userInfo.Line].ClientInfoLock.EnterReadLock();
                        try {
                            foreach (UserInfo tempUserInfo in gameData[userInfo.Line].ClientInfo) {
                                if (tempUserInfo.Id != id) {
                                    server.SendPackage(tempUserInfo.Id, 3, buffer);
                                }
                            }
                        } finally {
                            gameData[userInfo.Line].ClientInfoLock.ExitReadLock();
                        }
                        ExpandLog(userInfo.Line, userInfo.Remark + " 修改三号琴缺弦为：" + gameData[userInfo.Line].No3Qin);
                        break;
                    case 4:
                        gameData[userInfo.Line].No4Qin = SerializeTool.ByteToString(buffer, ref startIndex);
                        gameData[userInfo.Line].ClientInfoLock.EnterReadLock();
                        try {
                            foreach (UserInfo tempUserInfo in gameData[userInfo.Line].ClientInfo) {
                                if (tempUserInfo.Id != id) {
                                    server.SendPackage(tempUserInfo.Id, 4, buffer);
                                }
                            }
                        } finally {
                            gameData[userInfo.Line].ClientInfoLock.ExitReadLock();
                        }
                        ExpandLog(userInfo.Line, userInfo.Remark + " 修改四号琴缺弦为：" + gameData[userInfo.Line].No4Qin);
                        break;
                    case 5:
                        byte[] data;
                        lock (sendData) {
                            sendData.Clear();
                            int keyIndex = SerializeTool.ByteToInt(buffer, ref startIndex);
                            for (int i = 0; i < gameData[userInfo.Line].QinKey.Count; i++) {
                                if (i != keyIndex) {
                                    if (gameData[userInfo.Line].QinKey[i] == userInfo.Id) {
                                        gameData[userInfo.Line].QinKey[i] = 0;
                                    }
                                } else {
                                    if (gameData[userInfo.Line].QinKey[i] == 0) {
                                        gameData[userInfo.Line].QinKey[i] = userInfo.Id;
                                        ExpandLog(userInfo.Line, userInfo.Remark + " 补 " + keyIndex.ToString() + " 琴弦。");
                                    } else if (gameData[userInfo.Line].QinKey[i] == userInfo.Id) {
                                        gameData[userInfo.Line].QinKey[i] = 0;
                                        ExpandLog(userInfo.Line, userInfo.Remark + " 放弃补 " + keyIndex.ToString() + " 琴弦。");
                                    } else {
                                        ExpandLog(userInfo.Line, userInfo.Remark + " 尝试补 " + keyIndex.ToString() + " 琴弦但冲突。");
                                    }
                                }
                                SerializeTool.IntToByteList(gameData[userInfo.Line].QinKey[i], sendData);
                            }
                            SerializeTool.IntToByteList(keyIndex, sendData);
                            data = sendData.ToArray();
                        }
                        gameData[userInfo.Line].ClientInfoLock.EnterReadLock();
                        try {
                            foreach (UserInfo tempUserInfo in gameData[userInfo.Line].ClientInfo) {
                                if (tempUserInfo.Id != id) {
                                    server.SendPackage(tempUserInfo.Id, 6, data, 0, 48);
                                } else {
                                    server.SendPackage(tempUserInfo.Id, 7, data);
                                }
                            }
                        } finally {
                            gameData[userInfo.Line].ClientInfoLock.ExitReadLock();
                        }
                        break;
                    case 6:
                        if (!Directory.Exists(@".\Pic")) {
                            Directory.CreateDirectory(@".\Pic");
                        }
                        userInfo.PicPath = string.Format(".\\Pic\\{0}-{1}-{2}-{3}.jpg", DateTime.Now.ToString("yyyy年MM月dd日HH时mm分ss秒"), userInfo.MachineIdentity, Environment.TickCount, new Random().Next());
                        userInfo.PicPathStream = File.Create(userInfo.PicPath);
                        userInfo.PicPathStream.SetLength(SerializeTool.ByteToLong(buffer, ref startIndex));
                        lock (sendData) {
                            sendData.Clear();
                            SerializeTool.LongToByteList(userInfo.PicPathStream.Position, sendData);
                            server.SendPackage(userInfo.Id, 10, sendData.ToArray());
                        }
                        break;
                    case 7:
                        lock (userInfo.PicPathStream) {
                            userInfo.PicPathStream.Position = SerializeTool.ByteToLong(buffer, ref startIndex);
                            lock (sendData) {
                                sendData.Clear();
                                SerializeTool.LongToByteList(userInfo.PicPathStream.Position + buffer.Length - startIndex, sendData);
                                server.SendPackage(userInfo.Id, 10, sendData.ToArray());
                            }
                            userInfo.PicPathStream.Write(buffer, startIndex, buffer.Length - startIndex);
                            if (userInfo.PicPathStream.Position == userInfo.PicPathStream.Length) {
                                userInfo.PicPathStream.Close();
                                userInfo.PicPathStream = null;
                            }
                        }
                        break;
                    case 8:
                        if (!Directory.Exists(@".\Png")) {
                            Directory.CreateDirectory(@".\Png");
                        }
                        userInfo.PngPath = string.Format(".\\Png\\{0}-{1}-{2}-{3}.png", DateTime.Now.ToString("yyyy年MM月dd日HH时mm分ss秒"), userInfo.MachineIdentity, Environment.TickCount, new Random().Next());
                        userInfo.PngPathStream = File.Create(userInfo.PngPath);
                        userInfo.PngPathStream.SetLength(SerializeTool.ByteToLong(buffer, ref startIndex));
                        lock (sendData) {
                            sendData.Clear();
                            SerializeTool.LongToByteList(userInfo.PngPathStream.Position, sendData);
                            server.SendPackage(userInfo.Id, 12, sendData.ToArray());
                        }
                        break;
                    case 9:
                        lock (userInfo.PngPathStream) {
                            userInfo.PngPathStream.Position = SerializeTool.ByteToLong(buffer, ref startIndex);
                            lock (sendData) {
                                sendData.Clear();
                                SerializeTool.LongToByteList(userInfo.PngPathStream.Position + buffer.Length - startIndex, sendData);
                                server.SendPackage(userInfo.Id, 12, sendData.ToArray());
                            }
                            userInfo.PngPathStream.Write(buffer, startIndex, buffer.Length - startIndex);
                            if (userInfo.PngPathStream.Position == userInfo.PngPathStream.Length) {
                                userInfo.PngPathStream.Close();
                                userInfo.PngPathStream = null;
                            }
                        }
                        break;
                    case 10:
                        if (userInfo.GamePath.Length == 0) {
                            server.SendPackage(id, 18, buffer);
                        } else {
                            server.SendPackage(id, 17, buffer);
                        }
                        break;
                    case 11:
                        userInfo.KillingIntentionStrip = SerializeTool.ByteToInt(buffer, ref startIndex);
                        iniFile.IniWriteValue(userInfo.MachineIdentity, "杀意高度", userInfo.KillingIntentionStrip.ToString());
                        break;
                    case 12:
                        userInfo.FiveTone[0] = SerializeTool.ByteToInt(buffer, ref startIndex);
                        userInfo.FiveTone[1] = SerializeTool.ByteToInt(buffer, ref startIndex);
                        userInfo.FiveTone[2] = SerializeTool.ByteToInt(buffer, ref startIndex);
                        userInfo.FiveTone[3] = SerializeTool.ByteToInt(buffer, ref startIndex);
                        userInfo.FiveTone[4] = SerializeTool.ByteToInt(buffer, ref startIndex);
                        iniFile.IniWriteValue(userInfo.MachineIdentity, "宫", userInfo.FiveTone[0].ToString());
                        iniFile.IniWriteValue(userInfo.MachineIdentity, "商", userInfo.FiveTone[1].ToString());
                        iniFile.IniWriteValue(userInfo.MachineIdentity, "角", userInfo.FiveTone[2].ToString());
                        iniFile.IniWriteValue(userInfo.MachineIdentity, "徵", userInfo.FiveTone[3].ToString());
                        iniFile.IniWriteValue(userInfo.MachineIdentity, "羽", userInfo.FiveTone[4].ToString());
                        userInfo.FiveTone = userInfo.FiveTone;
                        break;
                    case 13:
                        ExpandLog(userInfo.Line, userInfo.Remark + " 推测缺 " + SerializeTool.ByteToString(buffer, ref startIndex));
                        break;
                    case 14:
                        if (userInfo.Manager) {
                            for (int i = 0; i < gameData[userInfo.Line].HitQinKey.Length; i++) {
                                gameData[userInfo.Line].HitQinKey[i] = buffer[startIndex++];
                            }
                            gameData[userInfo.Line].ClientInfoLock.EnterReadLock();
                            try {
                                foreach (UserInfo tempUserInfo in gameData[userInfo.Line].ClientInfo) {
                                    if (tempUserInfo.Id != id) {
                                        server.SendPackage(tempUserInfo.Id, 8, gameData[userInfo.Line].HitQinKey);
                                    }
                                }
                            } finally {
                                gameData[userInfo.Line].ClientInfoLock.ExitReadLock();
                            }
                        }
                        break;
                    case 15:
                        if (userInfo.Manager) {
                            ExpandLog(userInfo.Line, userInfo.Remark + " 补弦清屏---------------------");
                            gameData[userInfo.Line].No1Qin = gameData[userInfo.Line].No2Qin = gameData[userInfo.Line].No3Qin = gameData[userInfo.Line].No4Qin = "";
                            gameData[userInfo.Line].HitQinKey[0] = 0;
                            for (int i = 0; i < 12; i++) {
                                gameData[userInfo.Line].QinKey[i] = 0;
                            }
                            gameData[userInfo.Line].ClientInfoLock.EnterReadLock();
                            try {
                                byte[] intByte = new byte[4];
                                foreach (UserInfo tempUserInfo in gameData[userInfo.Line].ClientInfo) {
                                    SerializeTool.IntToByte(tempUserInfo.Id, intByte, 0);
                                    server.SendPackage(tempUserInfo.Id, 5, intByte);
                                }
                            } finally {
                                gameData[userInfo.Line].ClientInfoLock.ExitReadLock();
                            }
                        }
                        break;
                    case 16:
                        int success = SerializeTool.ByteToInt(buffer, ref startIndex);
                        int fail = SerializeTool.ByteToInt(buffer, ref startIndex);
                        int[] color = new int[15];
                        for (int i = 0; i < 15; i++) {
                            color[i] = SerializeTool.ByteToInt(buffer, ref startIndex);
                        }
                        ExpandLog(userInfo.Line, string.Format("{0} 颜色识别成功{1}次，失败{2}，颜色({3},{4},{5})({6},{7},{8})({9},{10},{11})({12},{13},{14})({15},{16},{17})",
                            userInfo.Remark, success, fail, color[0], color[1], color[2], color[3], color[4], color[5], color[6], color[7], color[8], color[9], color[10], color[11], color[12], color[13], color[14]));
                        break;
                    case 17:
                        lock (sendData) {
                            sendData.Clear();
                            int keyIndex = SerializeTool.ByteToInt(buffer, ref startIndex);
                            for (int i = 0; i < gameData[userInfo.Line].QinKey.Count; i++) {
                                if (i == keyIndex) {
                                    if (gameData[userInfo.Line].QinKey[i] == 0) {
                                        gameData[userInfo.Line].QinKey[i] = userInfo.Id;
                                        ExpandLog(userInfo.Line, userInfo.Remark + " 强制补 " + keyIndex.ToString() + " 琴弦。");
                                    } else {
                                        gameData[userInfo.Line].QinKey[i] = 0;
                                        ExpandLog(userInfo.Line, userInfo.Remark + " 强制取消补 " + keyIndex.ToString() + " 琴弦。");
                                    }
                                }
                                SerializeTool.IntToByteList(gameData[userInfo.Line].QinKey[i], sendData);
                            }
                            SerializeTool.IntToByteList(keyIndex, sendData);
                            data = sendData.ToArray();
                        }
                        gameData[userInfo.Line].ClientInfoLock.EnterReadLock();
                        try {
                            foreach (UserInfo tempUserInfo in gameData[userInfo.Line].ClientInfo) {
                                if (tempUserInfo.Id != id) {
                                    server.SendPackage(tempUserInfo.Id, 6, data, 0, 48);
                                } else {
                                    server.SendPackage(tempUserInfo.Id, 7, data);
                                }
                            }
                        } finally {
                            gameData[userInfo.Line].ClientInfoLock.ExitReadLock();
                        }
                        break;
                    case 18:
                        userInfo.GamePath = SerializeTool.ByteToString(buffer, ref startIndex);
                        break;
                    default:
                        break;

                }
            }
        }
        private void OnLeave(int id, object userToken) {
            if (userToken is LinkedListNode<UserInfo> user) {
                Dispatcher.Invoke(() => {
                    PopLog(0, "客户 " + id.ToString() + "离开。备注：" + user.Value.Remark);
                    gameData[user.Value.Line].ClientInfoLock.EnterWriteLock();
                    try {
                        gameData[user.Value.Line].ClientInfo.Remove(user);
                    } finally {
                        gameData[user.Value.Line].ClientInfo.ChangeComplete();
                        gameData[user.Value.Line].ClientInfoLock.ExitWriteLock();
                    }
                });
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e) {
            contextMenuStrip.Show();
        }
        private void ListViewItem_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            if (sender is ListViewItem item) {
                if (item.Content is UserInfo userInfo) {
                    Point point = e.GetPosition(this);
                    point = PointToScreen(point);
                    menuUser = userInfo;
                    contextMenuStrip.Show((int)point.X, (int)point.Y);
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
            for (int i = 0; i < gameData.Count; i++) {
                gameData[i].ClientInfoLock.EnterReadLock();
                try {
                    foreach (UserInfo tempUserInfo in gameData[i].ClientInfo) {
                        server.SendPackage(tempUserInfo.Id, 13, null);
                    }
                } finally {
                    gameData[i].ClientInfoLock.ExitReadLock();
                }
            }
        }
        private void ChooseLine_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            current = int.Parse((chooseLine.SelectedItem as ComboBoxItem).Tag.ToString());
            GamePanel.DataContext = gameData[current];
        }
        private void ExpandLog(int i, string content) {
            gameData[i].LogLock.EnterWriteLock();
            try {
                if (gameData[i].LogBack.Count < 15) {
                    for (int j = 0; j < 15; j++) {
                        gameData[i].LogBack.Push(new LinkedListNode<LogDetail>(new LogDetail()));
                    }
                }
                LinkedListNode<LogDetail> node = gameData[i].LogBack.Pop();
                node.Value.Content = content;
                node.Value.Time = Environment.TickCount;
                gameData[i].Log.AddFirst(node);
            } finally {
                gameData[i].LogLock.ExitWriteLock();
            }
        }
        private void PopLog(int i, string content) {
            gameData[i].LogLock.EnterWriteLock();
            try {
                LinkedListNode<LogDetail> node = gameData[i].LogBack.Count > 0 ? gameData[i].LogBack.Pop() : new LinkedListNode<LogDetail>(new LogDetail());
                node.Value.Content = content;
                node.Value.Time = Environment.TickCount;
                gameData[i].Log.AddFirst(node);
            } finally {
                gameData[i].LogLock.ExitWriteLock();
            }
        }
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) {
            System.Windows.Controls.TextBox sourceTextBox = (System.Windows.Controls.TextBox)e.Source;
            UserInfo userInfo = ((ContentPresenter)sourceTextBox.TemplatedParent).Content as UserInfo;
            int line = gameData[userInfo.Line].Line;
            try {
                line = int.Parse(sourceTextBox.Text);
            } catch (Exception) {
            }
            server.SendPackage(userInfo.Id, 19, SerializeTool.IntToByte(line));
        }
        private void CheckBox_SourceUpdated(object sender, DataTransferEventArgs e) {
            System.Windows.Controls.TextBox sourceTextBox = (System.Windows.Controls.TextBox)e.Source;
            UserInfo userInfo = ((ContentPresenter)sourceTextBox.TemplatedParent).Content as UserInfo;
            byte[] b = new byte[1];
            b[0] = userInfo.Manager ? (byte)1 : (byte)0;
            server.SendPackage(userInfo.Id, 20, b);
        }
    }
}