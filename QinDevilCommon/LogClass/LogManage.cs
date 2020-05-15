using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QinDevilCommon.LogClass {
    public class LogManage {
        private readonly StreamWriter sw;
        private readonly TextWriter tw;
        public LogManage(string Path) {
            sw = File.AppendText(Path);
            tw = TextWriter.Synchronized(sw);
        }
        public void Generate(string log) {
            tw.WriteLineAsync(log).Wait();
        }
        public void Flush() {
            tw.Flush();
        }
        ~LogManage() {
        }
    }
}
