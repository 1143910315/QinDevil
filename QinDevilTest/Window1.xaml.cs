using NAudio.CoreAudioApi;
using NAudio.Wave;
using QinDevilCommon.AccurateTimer;
using QinDevilCommon.Sound;
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
        private AccurateTimerClass accurateTimer = new AccurateTimerClass();
        private AudioCapture.DataCallback cb;
        private WaveFileWriter waveFileWriter;
        private int len = 0;
        public Window1() {
            InitializeComponent();
            AudioCapture audioCapture = new AudioCapture();
            cb = DataCallbackFunction;
            audioCapture.Capture(cb, FormatCallbackFunction);
            Thread.Sleep(10000);
            audioCapture.StopCapture();
            waveFileWriter.Close();
            Debug.WriteLine(len);
        }
        private void DataCallbackFunction(byte[] bs) {
            waveFileWriter.Write(bs, 0, bs.Length);
            len += bs.Length;
        }
        private void FormatCallbackFunction(WaveFormat waveFormat) {
            waveFileWriter = new WaveFileWriter("e:\\testtest4.wav", waveFormat);
        }
    }
}
