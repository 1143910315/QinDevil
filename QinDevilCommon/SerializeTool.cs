using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MusicPlayer3.Serialize {
    public class SerializeTool {
        private SerializeTool() {
        }
        //序列化
        public static byte[] RawSerialize(object obj) {
            int rawsize = Marshal.SizeOf(obj);
            IntPtr buffer = Marshal.AllocHGlobal(rawsize);
            Marshal.StructureToPtr(obj, buffer, false);
            byte[] rawdatas = new byte[rawsize];
            Marshal.Copy(buffer, rawdatas, 0, rawsize);
            Marshal.FreeHGlobal(buffer);
            return rawdatas;
        }
        //反序列化
        public static T RawDeserialize<T>(byte[] rawdatas, ref int startIndex) {
            int rawsize = Marshal.SizeOf<T>();
            if (startIndex + rawsize > rawdatas.Length) {
                throw new Exception("byte数组长度不足读取该类型！");
            }
            IntPtr buffer = Marshal.AllocHGlobal(rawsize);
            Marshal.Copy(rawdatas, startIndex, buffer, rawsize);
            T retobj = Marshal.PtrToStructure<T>(buffer);
            Marshal.FreeHGlobal(buffer);
            startIndex += rawsize;
            return retobj;
        }
        //从文件流当前位置反序列化一个对象
        public static T RawDeserializeFromFileStream<T>(FileStream fileStream) {
            int rawsize = Marshal.SizeOf<T>();
            byte[] rawdatas = new byte[rawsize];
            if (fileStream.Read(rawdatas, 0, rawdatas.Length) < rawsize) {
                throw new Exception("意外到达文件尾！");
            }
            IntPtr buffer = Marshal.AllocHGlobal(rawsize);
            Marshal.Copy(rawdatas, 0, buffer, rawsize);
            T retobj = Marshal.PtrToStructure<T>(buffer);
            Marshal.FreeHGlobal(buffer);
            return retobj;
        }
    }
}
