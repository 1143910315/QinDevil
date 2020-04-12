using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;

namespace QinDevilCommon.Data_structure {

    public class DoubleLinkList<T> : IEnumerable, INotifyCollectionChanged {
        /// <summary>
        /// 双向链表节点
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class DoubleLinkListNode {
            public T Data {
                set; get;
            }
            public DoubleLinkListNode Next {
                set; get;
            }
            public DoubleLinkListNode Prev {
                set; get;
            }
        }
        public class DoubleLinkListEnumerator : IEnumerator {
            private int index = -1;
            private readonly DoubleLinkList<T> list;
            public DoubleLinkListEnumerator(DoubleLinkList<T> doubleLinkList) {
                list = doubleLinkList;
                list.CollectionChanged += OnCollectionChanged;
            }
            public object Current => list.Get(index);
            public bool MoveNext() {
                return ++index < list._size;
            }
            public void Reset() {
                index = -1;
            }
            public void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
                if (e.Action == NotifyCollectionChangedAction.Add) {
                    Debug.WriteLine(string.Format("-- DoubleLinkListEnumerator[{0}] -- 触发Add，{1}-{2}", index, e.OldStartingIndex, e.NewStartingIndex));
                    if (e.NewStartingIndex <= index) {
                        index++;
                    }
                }
                if (e.Action == NotifyCollectionChangedAction.Remove) {
                    Debug.WriteLine(string.Format("-- DoubleLinkListEnumerator[{0}] -- 触发Remove，{1}-{2}", index, e.OldStartingIndex, e.NewStartingIndex));
                    if (e.NewStartingIndex <= index) {
                        index--;
                    }
                }
                if (e.Action == NotifyCollectionChangedAction.Reset) {
                    index = -1;
                }
            }
        }
        //表头
        private readonly DoubleLinkListNode _linkHead;
        //节点个数
        private int _size;
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public DoubleLinkList() {
            _linkHead = new DoubleLinkListNode();//双向链表 表头为空
            _linkHead.Prev = _linkHead;
            _linkHead.Next = _linkHead;
            _size = 0;
        }
        //通过索引查找
        private DoubleLinkListNode GetNode(int index) {
            if (index < 0 || index >= _size) {
                throw new IndexOutOfRangeException("索引溢出或者链表为空");
            }
            if (index < _size / 2) { //正向查找
                DoubleLinkListNode node = _linkHead.Next;
                for (int i = 0; i < index; i++)
                    node = node.Next;
                return node;
            }
            //反向查找
            DoubleLinkListNode rnode = _linkHead.Prev;
            int rindex = _size - index - 1;
            for (int i = 0; i < rindex; i++) {
                rnode = rnode.Prev;
            }
            return rnode;
        }
        // 将节点插入到第index位置之前
        public void InsertBefore(int index, T t) {
            if (index >= _size) {
                InsertAfter(index, t);
            } else {
                while (index < 0) {
                    index = _size + index;
                }
                DoubleLinkListNode inode = GetNode(index);
                DoubleLinkListNode tnode = new DoubleLinkListNode() {
                    Data = t,
                    Prev = inode.Prev,
                    Next = inode
                };
                inode.Prev.Next = tnode;
                inode.Prev = tnode;
                _size++;
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, t, index));
            }
        }
        //追加到index位置之后
        public void InsertAfter(int index, T t) {
            DoubleLinkListNode inode;
            if (_size == 0) {
                inode = _linkHead;
            } else {
                if (index >= _size) {
                    index = _size - 1;
                }
                while (index < 0) {
                    index = _size + index;
                }
                inode = GetNode(index);
            }
            DoubleLinkListNode tnode = new DoubleLinkListNode() {
                Data = t,
                Prev = inode,
                Next = inode.Next
            };
            inode.Next.Prev = tnode;
            inode.Next = tnode;
            _size++;
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, t, index + 1));
        }
        public void Del(int index) {
            if (_size == 0) {
                throw new IndexOutOfRangeException("没有可以删除的元素！");
            }
            if (index >= _size) {
                index = _size - 1;
            }
            while (index < 0) {
                index = _size + index;
            }
            DoubleLinkListNode inode = GetNode(index);
            inode.Prev.Next = inode.Next;
            inode.Next.Prev = inode.Prev;
            _size--;
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, inode.Data, index));
        }
        public int GetSize() => _size;
        public bool IsEmpty() => (_size == 0);
        public T Get(int index) => GetNode(index).Data;
        public T GetFirst() => GetNode(0).Data;
        public T GetLast() => GetNode(_size - 1).Data;
        public void DelFirst() => Del(0);
        public void DelLast() => Del(-1);
        public void ShowAll() {
            Console.WriteLine("******************* 链表数据如下 *******************");
            for (int i = 0; i < _size; i++) {
                Console.WriteLine("(" + i + ")=" + Get(i));
            }
            Console.WriteLine("******************* 链表数据展示完毕 *******************\n");
        }

        public IEnumerator GetEnumerator() {
            throw new NotImplementedException();
        }
    }
}
