using AudioPlayer.ViewModel;
using System;
using System.Collections.Generic;
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
    }
}
