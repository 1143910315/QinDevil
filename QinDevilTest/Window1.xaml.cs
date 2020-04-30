using NAudio.Wave;
using System;
using System.Collections.Generic;
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
        }
        public WaveIn mWavIn;
        public WaveFileWriter mWavWriter;

        /// <summary>
        /// 开始录音
        /// </summary>
        /// <param name="filePath"></param>
        public void StartRecord(string filePath) {
            mWavIn = new WaveIn();
            mWavIn.DataAvailable += MWavIn_DataAvailable;
            // mWavIn.RecordingStopped += MWavIn_RecordingStopped; 有冲突
            mWavWriter = new WaveFileWriter(filePath, mWavIn.WaveFormat);
            mWavIn.StartRecording();
        }

        /// <summary>
        /// 停止录音
        /// </summary>
        public void StopRecord() {
            mWavIn?.StopRecording();
            mWavIn?.Dispose();
            mWavIn = null;
            mWavWriter?.Close();
            mWavWriter = null;
        }

        //这个方法在调用关闭时会有冲突

        private void MWavIn_RecordingStopped(object sender, StoppedEventArgs e) {
            //mWavIn?.Dispose();
            //mWavIn = null;
            //mWavWriter?.Close();
            //mWavWriter = null;
        }

        private void MWavIn_DataAvailable(object sender, WaveInEventArgs e) {
            mWavWriter.Write(e.Buffer, 0, e.BytesRecorded);
            int secondsRecorded = (int)mWavWriter.Length / mWavWriter.WaveFormat.AverageBytesPerSecond;
        }
        /// <summary>
        /// 耳机sidetone测试
        /// </summary>
        /// <param name="i"></param>
        public void Sidetone() {
            StartRecord("e:\\rec.wav");
            Thread.Sleep(2000);
            StopRecord();
        }
    }
}
