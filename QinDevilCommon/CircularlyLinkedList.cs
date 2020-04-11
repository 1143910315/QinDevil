using System;
using System.Collections.Generic;
using System.Text;

namespace QinDevilCommon {
    public class CircularlyLinkedList<T> {
        private class NodeList {
            T node;
            NodeList next;
        }
        private NodeList head = new NodeList();
    }
}
