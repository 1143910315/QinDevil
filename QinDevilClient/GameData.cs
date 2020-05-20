using QinDevilCommon.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QinDevilClient {
    public class GameData : ViewModelBase {
        private int _line;
        public int Line {
            get => _line;
            set => Set(ref _line, value);
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
        private readonly List<int> _qinKey = new List<int>(new int[12]);
        public List<int> QinKey {
            get => _qinKey;
            set => Update();
        }
        private readonly byte[] _hitQinKey = new byte[9];
        public byte[] HitQinKey {
            get => _hitQinKey;
            set => Update();
        }
        private string _matchColor = "未就绪";
        public string MatchColor {
            get => _matchColor;
            set => Set(ref _matchColor, value);
        }
        private int _hitKeyIndex = 0;
        public int HitKeyIndex {
            get => _hitKeyIndex;
            set => Set(ref _hitKeyIndex, value);
        }
        private int _killingIntentionStrip = 0;
        public int KillingIntentionStrip {
            get => _killingIntentionStrip;
            set => Set(ref _killingIntentionStrip, value);
        }
        private int _time = 0;
        public int Time {
            get => _time;
            set => Set(ref _time, value);
        }
        private int[] _fiveTone = new int[] { 0, 0, 0, 0, 0 };
        public int[] FiveTone {
            get => _fiveTone;
            set => Set(ref _fiveTone, value);
        }
        private bool _fiveToneReady = false;
        public bool FiveToneReady {
            get => _fiveToneReady;
            set => Set(ref _fiveToneReady, value);
        }
        private bool _autoLessKey = false;
        public bool AutoLessKey {
            get => _autoLessKey;
            set => Set(ref _autoLessKey, value);
        }
        private int _hitKeyCD = 0;
        public int HitKeyCD {
            get => _hitKeyCD;
            set => Set(ref _hitKeyCD, value);
        }
    }
}
