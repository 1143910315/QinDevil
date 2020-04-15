using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace QinDevilClient {
    public class QinKeyConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values[0] is string str) {
                int index = (int)values[1];
                if (str.Length < index) {
                    return "";
                }
                switch (str[index - 1]) {
                    case '1':
                        return "宫";
                    case '2':
                        return "商";
                    case '3':
                        return "角";
                    case '4':
                        return "徵";
                    case '5':
                        return "羽";
                    default:
                        return "";
                }
            } else if (values[0] is List<int> list) {
                //return new SolidColorBrush(Colors.Silver);
                int index = (int)values[1];
                List<int> licence = values[2] as List<int>;
                if (index > 0 && list.Count >= index) {
                    if (licence.Contains(list[index - 1])) {
                        return new SolidColorBrush(Colors.Lime);
                    } else if (list[index - 1] != 0) {
                        return new SolidColorBrush(Colors.Red);
                    }
                }
                return new SolidColorBrush(Colors.Silver);
            } else {
                return null;
            }
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            return null;
        }
    }
}
