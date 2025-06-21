using System.Collections.Generic;

namespace UIDesign2d
{
    class EventControls
    {
        private static List<Control> _clickCtrls = new List<Control>();

        public static Control LastControl => _clickCtrls[Count - 1];

        public static Control SelectionControl { get; set; }
         
        public static Control FirstControl => _clickCtrls[0];

        public static int Count => _clickCtrls.Count;

        public static Control TopClickControl
        {
            get
            {
                Control prevControl = null;
                for (int i = Count - 1; i >= 0; i--)
                {
                    Control currentControl = _clickCtrls[i];
                    if (prevControl != currentControl.Parent)
                    {
                        return prevControl;
                    }
                    prevControl = (Control)_clickCtrls[i];
                }

                return prevControl;
            }
        }

        public static void Add(Control ctrl)
        {
            _clickCtrls.Add(ctrl);
        }

        public static void Clear()
        {
            _clickCtrls.Clear();
        }


    }
}
