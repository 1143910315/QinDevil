using QinDevilCommon.ViewModel;
using QinDevilCommon.Data_structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace QinDevilServer {
    public class GameData : ViewModelBase {
        public readonly ReaderWriterLockSlim ClientInfoLock = new ReaderWriterLockSlim();
        private readonly NotifyLinkedList<UserInfo> _clientInfo = new NotifyLinkedList<UserInfo>();
        public NotifyLinkedList<UserInfo> ClientInfo {
            get => _clientInfo;
            set => Update();
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
        private byte[] _hitQinKey = new byte[9];
        public byte[] HitQinKey {
            get => _hitQinKey;
            set => Set(ref _hitQinKey, value);
        }
        public readonly ReaderWriterLockSlim LogLock = new ReaderWriterLockSlim();
        private readonly NotifyLinkedList<LogDetail> _log = new NotifyLinkedList<LogDetail>();
        public NotifyLinkedList<LogDetail> Log {
            get => _log;
            set => Update();
        }
        private Stack<LinkedListNode<LogDetail>> _logBack = new Stack<LinkedListNode<LogDetail>>();
        public Stack<LinkedListNode<LogDetail>> LogBack {
            get => _logBack;
            set => Set(ref _logBack, value);
        }
        private int _line;
        public int Line {
            get => _line;
            set => Set(ref _line, value);
        }
        private bool _allowAutoPlay = false;
        public bool AllowAutoPlay {
            get => _allowAutoPlay;
            set => Set(ref _allowAutoPlay, value);
        }
        private bool _allowAutoLessKey = false;
        public bool AllowAutoLessKey {
            get => _allowAutoLessKey;
            set => Set(ref _allowAutoLessKey, value);
        }
    }
}
