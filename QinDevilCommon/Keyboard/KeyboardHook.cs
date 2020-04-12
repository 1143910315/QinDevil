using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QinDevilCommon.Keyboard {
    public class KeyboardHook {
        [StructLayout(LayoutKind.Sequential)] //声明键盘钩子的封送结构类型 
        public class KeyboardHookStruct {
            public readonly int vkCode; //表示一个在1到254间的虚似键盘码 
            public readonly int scanCode; //表示硬件扫描码 
            public readonly int flags;
            public readonly int time;
            public readonly int dwExtraInfo;
        }
        [DllImport("user32.dll")]
        public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);
        [DllImport("user32.dll")]
        public static extern int CallNextHookEx(int hHook, int ncode, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern int UnhookWindowsHookEx(int hHook);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int HookProc(int idHook, int wParam, int lParam);
        public delegate void KeyDown(KeyCode keyCode);
        public delegate void KeyUp(KeyCode keyCode);
        public KeyDown KeyDownEvent;
        public KeyDown KeyUpEvent;
        private HookProc hookProc;
        private const int HC_ACTION = 0;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 256;
        private const int WM_KEYUP = 257;
        private readonly int hHook;
        public KeyboardHook() {
            hookProc = HookCallback;
            hHook = SetWindowsHookEx(WH_KEYBOARD_LL, hookProc, Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().ManifestModule), 0);
        }
        private int HookCallback(int idHook, int wParam, int lParam) {
            if (idHook == HC_ACTION) {
                KeyboardHookStruct keyboardHookStruct = Marshal.PtrToStructure<KeyboardHookStruct>(new IntPtr(lParam));
                switch (wParam) {
                    case WM_KEYDOWN:
                        new Task((keycode) => {
                            KeyDownEvent?.Invoke((KeyCode)keycode);
                        }, keyboardHookStruct.vkCode).Start();
                        break;
                    case WM_KEYUP:
                        new Task((keycode) => {
                            KeyUpEvent?.Invoke((KeyCode)keycode);
                        }, keyboardHookStruct.vkCode).Start();
                        break;
                    default:
                        break;
                }
            }
            return CallNextHookEx(hHook, idHook, wParam, lParam);
        }
        ~KeyboardHook() {
            UnhookWindowsHookEx(hHook);
        }
    }
}
