using AudioPlayer.ViewModel;
using QinDevilCommon.Data_structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QinDevilServer {
    public class GameData : ViewModelBase {
        public const int State_LeakHunting = 0;
        public const int State_HitKey = 1;
        private DoubleLinkList<UserInfo> _clientInfo = new DoubleLinkList<UserInfo>();
        public DoubleLinkList<UserInfo> ClientInfo {
            get => _clientInfo;
            set => Set(ref _clientInfo, value);
        }
        private int _state;
        public int State {
            get => _state;
            set => Set(ref _state, value);
        }
    }
}
