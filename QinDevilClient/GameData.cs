﻿using AudioPlayer.ViewModel;
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
        private List<int> _licence = new List<int>();
        public List<int> Licence {
            get => _licence;
            set => Set(ref _licence, value);
        }
        private string _no1Qin = "";
        public string No1Qin {
            get => _no1Qin;
            set => Set(ref _no1Qin, value);
        }
        private string _no2Qin = "";
        public string No2Qin {
            get => _no2Qin;
            set => Set(ref _no2Qin, value);
        }
        private string _no3Qin = "";
        public string No3Qin {
            get => _no3Qin;
            set => Set(ref _no3Qin, value);
        }
        private string _no4Qin = "";
        public string No4Qin {
            get => _no4Qin;
            set => Set(ref _no4Qin, value);
        }
    }
}
