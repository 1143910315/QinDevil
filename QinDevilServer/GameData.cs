using QinDevilCommon.ViewModel;
using QinDevilCommon.Data_structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QinDevilServer {
    public class GameData : ViewModelBase {
        private DoubleLinkList<UserInfo> _clientInfo = new DoubleLinkList<UserInfo>();
        public DoubleLinkList<UserInfo> ClientInfo {
            get => _clientInfo;
            set => Set(ref _clientInfo, value);
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
        private string _hitQinKey = "";
        public string HitQinKey {
            get => _hitQinKey;
            set => Set(ref _hitQinKey, value);
        }
        private readonly List<LogDetail> _log = new List<LogDetail>();
        public List<LogDetail> Log {
            get => _log;
            set => Update();
        }
    }
}
