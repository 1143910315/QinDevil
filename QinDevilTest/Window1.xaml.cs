using NAudio.CoreAudioApi;
using NAudio.Wave;
using SharpCapture;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace QinDevilTest {
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class Window1 : Window {
        public Window1() {
            InitializeComponent();
            MMDeviceEnumerator mMDeviceEnumerator = new MMDeviceEnumerator();
            MMDevice mMDevice = mMDeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            Debug.WriteLine(mMDevice.FriendlyName);
            AudioClient audioClient=mMDevice.AudioClient;
            audioClient.Initialize(AudioClientShareMode.Shared, AudioClientStreamFlags.Loopback, 1000, 100, null, Guid.NewGuid());
            AudioCaptureClient audioCaptureClient = audioClient.AudioCaptureClient;
            int len;
            AudioClientBufferFlags audioClientBufferFlags;
            IntPtr intPtr = audioCaptureClient.GetBuffer(out len, out audioClientBufferFlags);
            Debug.WriteLine(intPtr.ToInt32());
            Debug.WriteLine(len);
            Debug.WriteLine(audioClientBufferFlags);
        }
    }
}
