using NAudio.CoreAudioApi;
using NAudio.Wave;
using QinDevilCommon.AccurateTimer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QinDevilCommon.Sound {
    public class AudioCapture {
        public delegate void DataCallback(byte[] bs);
        public delegate void FormatCallback(WaveFormat waveFormat);
        private MMDeviceEnumerator mMDeviceEnumerator;
        private MMDevice mMDevice;
        private AudioClient audioClient;
        private WaveFormat mixFormat;
        private AudioCaptureClient audioCaptureClient;
        ///private Action action;
        private DataCallback cb;
        //private bool capture = true;
        private AccurateTimerClass accurateTimer;
        private AccurateSingleTimer accurateSingleTimer;
        private int success = 0;
        private int fail = 0;
        public AudioCapture() {
        }
        public void Capture(DataCallback callback, FormatCallback formatCallback) {
            cb = callback;
            mMDeviceEnumerator = new MMDeviceEnumerator();
            mMDevice = mMDeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            audioClient = mMDevice.AudioClient;
            mixFormat = audioClient.MixFormat;
            Debug.WriteLine(mixFormat);
            formatCallback?.Invoke(mixFormat);
            audioClient.Initialize(AudioClientShareMode.Shared, AudioClientStreamFlags.Loopback, 0, 0, mixFormat, Guid.Empty);
            audioCaptureClient = audioClient.AudioCaptureClient;
            audioClient.Start();
            //new Task(action).Start();
            accurateTimer = new AccurateTimerClass();
            accurateSingleTimer = accurateTimer.AddTimer(0, (uint)(audioClient.DefaultDevicePeriod / 1.3 / (10 * 1000)), WaitOrTimerCallbackFunc);

        }
        private void WaitOrTimerCallbackFunc(object state, bool timedOut) {
            lock (audioCaptureClient) {
                int nextPacketSize = audioCaptureClient.GetNextPacketSize();
                if (nextPacketSize > 0) {
                    success++;
                    IntPtr intPtr = audioCaptureClient.GetBuffer(out int readNum, out AudioClientBufferFlags audioClientBufferFlags);
                    byte[] ys = new byte[readNum * mixFormat.BlockAlign];
                    Marshal.Copy(intPtr, ys, 0, readNum * mixFormat.BlockAlign);
                    audioCaptureClient.ReleaseBuffer(readNum);
                    cb.Invoke(ys);
                } else {
                    fail++;
                }
            }
        }
        public void StopCapture() {
            audioClient.Stop();
            accurateSingleTimer.Close();
            audioClient.Reset();
            Debug.WriteLine(string.Format("{0}-{1}", success, fail));
        }
    }
}
