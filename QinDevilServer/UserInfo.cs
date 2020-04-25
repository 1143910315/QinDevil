using QinDevilCommon.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QinDevilServer {
    public class UserInfo : ViewModelBase {
        private int _id;
        public int Id {
            get => _id;
            set => Set(ref _id, value);
        }
        private DateTime _lastReceiveTime;
        public DateTime LastReceiveTime {
            get => _lastReceiveTime;
            set => Set(ref _lastReceiveTime, value);
        }
        private string _ipAndPort;
        public string IpAndPort {
            get => _ipAndPort;
            set => Set(ref _ipAndPort, value);
        }
        private string _machineIdentity = "";
        public string MachineIdentity {
            get => _machineIdentity;
            set => Set(ref _machineIdentity, value);
        }
        private string _gamePath = "";
        public string GamePath {
            get => _gamePath;
            set => Set(ref _gamePath, value);
        }
        private FileStream _picPathStream;
        public FileStream PicPathStream {
            get => _picPathStream;
            set => Set(ref _picPathStream, value);
        }
        private string _picPath = "";
        public string PicPath {
            get => _picPath;
            set => Set(ref _picPath, value);
        }
        private FileStream _bmpPathStream;
        public FileStream PngPathStream {
            get => _bmpPathStream;
            set => Set(ref _bmpPathStream, value);
        }
        private string _bmpPath = "";
        public string PngPath {
            get => _bmpPath;
            set => Set(ref _bmpPath, value);
        }
        private string _remark = "";
        public string Remark {
            get => _remark;
            set => Set(ref _remark, value);
        }
        private int _killingIntentionStrip = 0;
        public int KillingIntentionStrip {
            get => _killingIntentionStrip;
            set => Set(ref _killingIntentionStrip, value);
        }
    }
}
