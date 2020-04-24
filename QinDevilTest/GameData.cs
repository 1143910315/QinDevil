using QinDevilCommon.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace QinDevilTest {
    public class GameData :ViewModelBase {
        private string _mousePoint="";
        public string MousePoint {
            get => _mousePoint;
            set => Set(ref _mousePoint, value);
        }
        private string _mouseColor = "";
        public string MouseColor {
            get => _mouseColor;
            set => Set(ref _mouseColor, value);
        }
        private SolidColorBrush _color=new SolidColorBrush(System.Windows.Media.Color.FromRgb(0,0,0));
        public SolidColorBrush Color {
            get => _color;
            set => Set(ref _color, value);
        }
        private string _colorDifference = "";
        public string ColorDifference {
            get => _colorDifference;
            set => Set(ref _colorDifference, value);
        }
        private string _gamePath = "";
        public string GamePath {
            get => _gamePath;
            set => Set(ref _gamePath, value);
        }
        private string _key = "";
        public string Key {
            get => _key;
            set => Set(ref _key, value);
        }
    }
}
