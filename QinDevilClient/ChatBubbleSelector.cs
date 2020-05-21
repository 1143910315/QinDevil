using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace QinDevilClient
{
    public class ChatBubbleSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var u = container as FrameworkElement;

            //MessageEntity message = item as MessageEntity;

            //if (message.IsSend)
            return u.FindResource("chatSend") as DataTemplate;
            //else
            //    return u.FindResource("chatRecv") as DataTemplate;
        }
    }
}
