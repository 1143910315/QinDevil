using QinDevilCommon.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QinDevilServer {
    public class LogDetail : ViewModelBase {
        private string _content;
        public string Content {
            get => _content;
            set => Set(ref _content, value);
        }
        private int _time;
        public int Time {
            get => _time;
            set => Set(ref _time, value);
        }
    }
}
