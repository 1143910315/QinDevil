using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace QinDevilCommon.AccurateTimer {
    public class AccurateSingleTimer {
        [DllImport("Kernel32.dll", SetLastError = true, EntryPoint = "DeleteTimerQueueTimer")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteTimerQueueTimer(IntPtr TimerQueue, IntPtr Timer, IntPtr CompletionEvent);
        private IntPtr newTimer;
#pragma warning disable IDE0052 // 删除未读的私有成员
        private readonly WaitOrTimerCallback waitOrTimerCallback;
#pragma warning restore IDE0052 // 删除未读的私有成员
        private readonly IntPtr timerQueue;
        internal AccurateSingleTimer(IntPtr newTimer, WaitOrTimerCallback waitOrTimerCallback, IntPtr timerQueue) {
            this.newTimer = newTimer;
            this.waitOrTimerCallback = waitOrTimerCallback;
            this.timerQueue = timerQueue;
        }
        public void Close() {
            if (!newTimer.Equals(IntPtr.Zero)) {
                _ = DeleteTimerQueueTimer(timerQueue, newTimer, new IntPtr(-1));
                newTimer = IntPtr.Zero;
            }
        }
        ~AccurateSingleTimer() {
            if (!newTimer.Equals(IntPtr.Zero)) {
                _ = DeleteTimerQueueTimer(timerQueue, newTimer, new IntPtr(-1));
            }
        }
    }
    public class AccurateTimerClass {
        [StructLayout(LayoutKind.Sequential)] //声明键盘钩子的封送结构类型 
        public class ParameterStruct {
            public readonly int vkCode; //表示一个在1到254间的虚似键盘码 
            public readonly int scanCode; //表示硬件扫描码 
            public readonly int flags;
            public readonly int time;
            public readonly int dwExtraInfo;
        }
        [DllImport("Kernel32.dll", SetLastError = true, EntryPoint = "CreateTimerQueue")]
        public static extern IntPtr CreateTimerQueue();
        [DllImport("Kernel32.dll", SetLastError = true, EntryPoint = "CreateTimerQueueTimer")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateTimerQueueTimer(ref IntPtr phNewTimer, IntPtr TimerQueue, WaitOrTimerCallback Callback, IntPtr Parameter, uint DueTime, uint Period, uint Flags);
        [DllImport("Kernel32.dll", SetLastError = true, EntryPoint = "DeleteTimerQueueEx")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteTimerQueueEx(IntPtr TimerQueue, IntPtr CompletionEvent);
        private readonly IntPtr timerQueue;
        public AccurateTimerClass() {
            timerQueue = CreateTimerQueue();
        }
        public AccurateSingleTimer AddTimer(uint startTime, uint ElapsedTime, WaitOrTimerCallback waitOrTimerCallback) {
            IntPtr newTimer = new IntPtr(0);
            //CreateTimerQueueTimer_1(ref newTimer, timerQueue, waitOrTimerCallback, 0, (int)startTime, (int)ElapsedTime, 0);
            //return null;
            return CreateTimerQueueTimer(ref newTimer, timerQueue, waitOrTimerCallback, IntPtr.Zero, startTime, ElapsedTime, 0)
                ? new AccurateSingleTimer(newTimer, waitOrTimerCallback, timerQueue)
                : null;
        }
        ~AccurateTimerClass() {
            if (!timerQueue.Equals(IntPtr.Zero)) {
                _ = DeleteTimerQueueEx(timerQueue, new IntPtr(-1));
            }
        }
    }
}
