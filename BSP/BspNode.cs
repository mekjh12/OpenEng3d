using System;

namespace BSP
{
    public class BspNode
    {
        static int guid = 0;

        int _id;
        BspNode _front;
        BspNode _back;
        BspNode _parent;
        Segment3 _segment;
        string _txt;

        public int ID => _id;

        public string EquationToString => _segment.EquationToString;

        public string Text
        {
            get => _txt;
            set => _txt = value;
        }

        public Segment3 Segment
        {
            get => _segment;
            set => _segment = value;
        }

        public BspNode Front
        {
            get => _front;
            set => _front = value;
        }

        public BspNode Back
        {
            get => _back;
            set => _back = value;
        }

        public BspNode Parent
        {
            get => _parent;
            set => _parent = value;
        }

        public BspNode(Segment3 segment)
        {
            BspNode.guid++;
            _id = BspNode.guid;
            Segment = segment;
        }

    }
}
