using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace QinDevilCommon.Data_structure {
    public class SafeLinkListEnumerator<T> : IEnumerator {
        public object Current => throw new NotImplementedException();
        private SafeLinkListNode<T> headNode;
        internal SafeLinkListEnumerator(SafeLinkListNode<T> head) {
            headNode = head;
        }
        public bool MoveNext() {
            throw new NotImplementedException();
        }
        public void Reset() {
            throw new NotImplementedException();
        }
    }
    public class SafeLinkListNode<T> {
        public T data;
        public SafeLinkListNode<T> next;
    }
    public class SafeLinkList<T> : IEnumerable {
        private SafeLinkListNode<T> headNode = null;
        private int count =0;
        /*public T this[int index] { 
            get { 
            } set { } }*/
        public void AddData(T data) {
            SafeLinkListNode<T> temp = new SafeLinkListNode<T>() {
                data = data, next = headNode
            };
            headNode = temp;
        }
        public void DeleteNode(SafeLinkListNode<T> node) {
            SafeLinkListNode<T> temp = headNode;
            if (temp != null) {
                if (temp.Equals(node)) {
                    headNode = headNode.next;
                } else {
                    SafeLinkListNode<T> beDelete = temp.next;
                    while (beDelete != null) {
                        if (beDelete.Equals(node)) {
                            temp.next = beDelete.next;
                            beDelete = null;
                        } else {
                            temp = temp.next;
                            beDelete = temp.next;
                        }
                    }
                }
            }
        }
        public int GetCount() {
            return count;
        }
        public SafeLinkListNode<T> GetHead() {
            return headNode;
        }
        public IEnumerator GetEnumerator() {
            return new SafeLinkListEnumerator<T>(headNode);
        }
    }
}
