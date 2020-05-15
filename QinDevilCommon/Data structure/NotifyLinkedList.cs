using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace QinDevilCommon.Data_structure {
    public class NotifyLinkedList<T> : LinkedList<T>, INotifyCollectionChanged {
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public NotifyLinkedList() : base() {
        }
        public NotifyLinkedList(IEnumerable<T> collection) : base(collection) {
        }
        public void ChangeComplete() {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
