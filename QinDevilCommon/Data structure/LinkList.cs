using System;
using System.Collections.Generic;
using System.Text;

namespace QinDevilCommon.Data_structure {
    public class LinkList<T> {
        private class LinkListNode {
            public T Data {
                set; get;
            }          //数据域,当前结点数据
            public LinkListNode Next {
                set; get;
            }    //位置域,下一个结点地址
            public LinkListNode() {
                Data = default;
                Next = null;
            }
        }
        private LinkListNode Head {
            set; get;
        } //单链表头
        private int len = 0;
        private readonly object lockObject = new object();
        //构造
        public LinkList() {
            Clear();
        }

        /// <summary>
        /// 求单链表的长度
        /// </summary>
        /// <returns></returns>
        public int GetLength() {
            /*
            Node p = Head;
            int length = 0;
            while (p != null) {
                p = p.Next;
                length++;
            }
            return length;
            */
            return len;
        }

        /// <summary>
        /// 判断单键表是否为空
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty() {
            if (Head == null)
                return true;
            else
                return false;
        }

        /// <summary>
        /// 清空单链表
        /// </summary>
        public void Clear() {
            Head = null;
            len = 0;
        }

        /// <summary>
        /// 获得当前位置单链表中结点的值
        /// </summary>
        /// <param name="i">结点位置</param>
        /// <returns></returns>
        public T GetNodeValue(int i) {
            lock (lockObject) {
                if (IsEmpty() || i < 0 || i >= GetLength()) {
                    Console.WriteLine("单链表为空或结点位置有误！");
                    return default;
                }
                LinkListNode A = Head;
                int j = 0;
                while (A.Next != null && j < i) {
                    A = A.Next;
                    j++;
                }
                return A.Data;
            }
        }
        /// <summary>
        /// 增加新元素到单链表末尾
        /// </summary>
        public void Append(T item) {
            lock (lockObject) {
                LinkListNode foot = new LinkListNode() {
                    Data = item
                };
                if (Head == null) {
                    Head = foot;
                    len++;
                    return;
                }
                LinkListNode A = Head;
                while (A.Next != null) {
                    A = A.Next;
                }
                A.Next = foot;
                len++;
            }
        }
        /// <summary>
        /// 增加单链表插入的位置
        /// </summary>
        /// <param name="item">结点内容</param>
        /// <param name="n">结点插入的位置</param>
        public void Insert(T item, int n) {
            lock (lockObject) {
                if (IsEmpty() || n < 0 || n >= GetLength()) {
                    Console.WriteLine("单链表为空或结点位置有误！");
                    return;
                }
                if (n == 0) { //增加到头部
                    LinkListNode H = new LinkListNode() {
                        Data = item,
                        Next = Head
                    };
                    Head = H;
                    len++;
                    return;
                }
                LinkListNode A = new LinkListNode();
                LinkListNode B = Head;
                int j = 0;
                while (B.Next != null && j < n) {
                    A = B;
                    B = B.Next;
                    j++;
                }
                LinkListNode C = new LinkListNode() {
                    Data=item,
                    Next = B
                };
                A.Next = C;
                len++;
            }
        }
        /// <summary>
        /// 删除单链表结点
        /// </summary>
        /// <param name="i">删除结点位置</param>
        /// <returns></returns>
        public void Delete(int i) {
            lock (lockObject) {
                if (IsEmpty() || i < 1 || i > GetLength()) {
                    Console.WriteLine("单链表为空或结点位置有误！");
                    return;
                }
                if (i == 1) {//删除头
                    Head = Head.Next;
                    len--;
                    return;
                }
                LinkListNode A = null;
                LinkListNode B = Head;
                int j = 1;
                while (B.Next != null && j < i) {
                    A = B;
                    B = B.Next;
                    j++;
                }
                if (j == i) {
                    A.Next = B.Next;
                    len--;
                }
            }
        }
        /*
        /// <summary>
        /// 显示单链表
        /// </summary>
        public void Dispaly() {
            Node A = Head;
            while (A != null) {
                Console.WriteLine(A.Data);
                A = A.Next;
            }
        }
        */
        /// <summary>
        /// 单链表反转
        /// </summary>
        public void Reverse() {
            if (GetLength() == 1 || Head == null) {
                return;
            }
            LinkListNode NewNode = null;
            LinkListNode CurrentNode = Head;
            LinkListNode TempNode;
            while (CurrentNode != null) {
                TempNode = CurrentNode.Next;
                CurrentNode.Next = NewNode;
                NewNode = CurrentNode;
                CurrentNode = TempNode;
            }
            Head = NewNode;
            //Dispaly();
        }
        /// <summary>
        /// 获得单链表中间值
        /// 思路：使用两个指针，第一个每次走一步，第二个每次走两步：
        /// </summary>
        public T GetMiddleValue() {
            LinkListNode A = Head;
            LinkListNode B = Head;
            while (B != null && B.Next != null) {
                A = A.Next;
                B = B.Next.Next;
            }
            return A.Data;
            /*if (B != null) //奇数
            {
                Console.WriteLine("奇数:中间值为：{0}", A.Data);
            } else    //偶数
              {
                Console.WriteLine("偶数:中间值为：{0}和{1}", A.Data, A.Next.Data);
            }*/
        }
    }
}
