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
        //专为字符串序列化
        public static byte[] RawSerializeForUTF8String(string str) {
            byte[] stringBytes = Encoding.UTF8.GetBytes(str);
            byte[] rawdatas = new byte[4 + stringBytes.Length];
            rawdatas[0] = (byte)(stringBytes.Length & 0xFF);
            rawdatas[1] = (byte)((stringBytes.Length >> 8) & 0xFF);
            rawdatas[2] = (byte)((stringBytes.Length >> 16) & 0xFF);
            rawdatas[3] = (byte)((stringBytes.Length >> 24) & 0xFF);
            stringBytes.CopyTo(rawdatas, 4);
            return rawdatas;
        }
        //专为字符串反序列化
        public static string RawDeserializeForUTF8String(byte[] rawdatas, ref int startIndex) {
            int length = rawdatas[startIndex++]
                | (rawdatas[startIndex++] >> 8)
                | (rawdatas[startIndex++] >> 16)
                | (rawdatas[startIndex++] >> 24);
            string str = Encoding.UTF8.GetString(rawdatas, startIndex, length);
            startIndex += length;
            return str;
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
