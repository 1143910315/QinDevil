using AudioPlayer.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QinDevilClient {
    public class GameData : ViewModelBase {
        private int _failTimes;
        public int FailTimes {
            get => _failTimes;
            set => Set(ref _failTimes, value);
        }
        private int _ping = 9999;
        public int Ping {
            get => _ping;
            set => Set(ref _ping, value);
        }
    }
}
