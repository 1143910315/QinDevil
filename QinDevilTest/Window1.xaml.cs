using NAudio.CoreAudioApi;
using NAudio.Wave;
using QinDevilCommon.AccurateTimer;
using QinDevilCommon.Sound;
using SharpCapture;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        //private AudioCapture.DataCallback cb;
        //private WaveFileWriter waveFileWriter;
        //private int len = 0;
        public Window1() {
            InitializeComponent();
            Task task = new Task(() => {
                MMDeviceEnumerator mMDeviceEnumerator = new MMDeviceEnumerator();
                MMDevice mMDevice = mMDeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                AudioClient audioClient = mMDevice.AudioClient;
                WaveFormat mixFormat = audioClient.MixFormat;
                WaveFormat format = new WaveFormat(mixFormat.SampleRate, 16, 2);
                audioClient.Initialize(AudioClientShareMode.Shared, AudioClientStreamFlags.Loopback, 0, 0, format, Guid.Empty);
                AudioCaptureClient audioCaptureClient = audioClient.AudioCaptureClient;
                //WaveFormat waveFormat1 = new WaveFormat(format.SampleRate, format.BitsPerSample, format.Channels);
                WaveFileWriter waveFileWriter = new WaveFileWriter("e:\\testtest4.wav", format);
                audioClient.Start();
                int sleepTime = (int)(audioClient.DefaultDevicePeriod / 2 / (10 * 1000));
                int success = 0;
                while (success++ < 3000) {
                    Thread.Sleep(sleepTime);
                    int nextPacketSize = audioCaptureClient.GetNextPacketSize();
                    while (nextPacketSize > 0) {
                        IntPtr intPtr = audioCaptureClient.GetBuffer(out int readNum, out AudioClientBufferFlags audioClientBufferFlags);
                        byte[] ys = new byte[readNum * format.BlockAlign];
                        Marshal.Copy(intPtr, ys, 0, readNum * format.BlockAlign);
                        audioCaptureClient.ReleaseBuffer(readNum);
                        waveFileWriter.Write(ys, 0, ys.Length);
                        nextPacketSize = audioCaptureClient.GetNextPacketSize();
                    }
                }
                audioClient.Stop();
                audioCaptureClient.Dispose();
                waveFileWriter.Close();
            });
            task.Start();
            Thread.Sleep(10000);
            task.Wait();
            /*AudioCapture audioCapture = new AudioCapture();
            cb = DataCallbackFunction;
            audioCapture.Capture(cb, FormatCallbackFunction);
            Thread.Sleep(10000);
            audioCapture.StopCapture();
            waveFileWriter.Close();
            Debug.WriteLine(len);*/
        }
        /*private void DataCallbackFunction(byte[] bs) {
            waveFileWriter.Write(bs, 0, bs.Length);
            //len += bs.Length;
        }
        private void FormatCallbackFunction(WaveFormat waveFormat) {
            WaveFormat waveFormat1 = new WaveFormat(waveFormat.SampleRate, waveFormat.BitsPerSample, waveFormat.Channels);
            Debug.WriteLine(waveFormat1.Encoding.ToString() + "-----" + waveFormat.Encoding.ToString());
            waveFileWriter = new WaveFileWriter("e:\\testtest4.wav", waveFormat);
        }*/
    }
}
