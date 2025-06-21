using OpenGL;
using System;

namespace Ui2d
{
    public class VScrollBar : Panel
    {
        int _index = 0;
        int _maxIdx = 0;
        int _minIdx = 0;
        Vertex3f _scrollColor = Vertex3f.Zero;
        Panel _focus;
        Panel _background;

        event Action<int> _scroll;
        event Action<int> _scrollChanged;

        public Action<int> Scroll
        {
            get => _scroll; 
            set => _scroll = value;
        }

        public Action<int> ScrollChanged
        {
            get => _scrollChanged;
            set => _scrollChanged = value;
        }

        public Vertex3f ScrollColor
        {
            set => _scrollColor = value;
        }

        public int Index
        {
            set => _index = value;// Math.Min(Math.Max(, _minIdx), _maxIdx);
            get => _index;
        }

        public int MaxLine
        {
            get => _maxIdx;
            set => _maxIdx = value;
        }

        public VScrollBar(string name, FontFamily fontFamily) : base(name)
        {
            _minIdx = 0;

            _background = new Panel(name + "_focusBackground")
            {
                Align = CONTROL_ALIGN.NONE,
                Width = 1.0f,
                Height = 1.0f,
                BackColor = _scrollColor,
                Alpha = 0.3f,
            };
            AddChild(_background);

            _focus = new Panel(name + "_focus")
            {
                Align = CONTROL_ALIGN.NONE,
                Width = 1.0f,
                Height = 0.1f,
                BackColor = _scrollColor * 0.3f,
                Alpha = 1.0f,
                DragDrop = (obj, x, y, dx, dy) =>
                {
                    
                },
            };
            AddChild(_focus);
        }

        public override void Update(int deltaTime)
        {
            float h = 1.0f / (_maxIdx - _minIdx);

            _focus.Top = _index * h;
            _focus.Height = Math.Max(h, 0.01f);
            
            base.Update(deltaTime);
        }
    }
}
