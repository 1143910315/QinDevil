using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace QinDevilClient {
    public class HitQinKeyContentConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            string str = "";
            if (value is byte[] bs) {
                for (int i = 0; i < bs.Length; i++) {
                    switch (bs[i]) {
                        case 1:
                            str += "1 ";
                            break;
                        case 2:
                            str += "2 ";
                            break;
                        case 3:
                            str += "3 ";
                            break;
                        case 4:
                            str += "4 ";
                            break;
                        case 5:
                            str += "5 ";
                            break;
                        default:
                            i = bs.Length;
                            break;
                    }
                }
            }
            return str;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
