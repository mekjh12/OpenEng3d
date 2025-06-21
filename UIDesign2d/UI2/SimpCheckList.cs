using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using ZetaExt;
using static System.Net.Mime.MediaTypeNames;

namespace Ui2d
{
    public class SimpCheckList : Panel
    {
        protected List<SimpCheckbox> _items;
        protected event Action<SimpCheckList, SimpCheckbox, bool> _changeValue;
        private int _renderCount = 0;

        public string[] Items
        {
            set
            {
                if (_items == null) _items = new List<SimpCheckbox>();
                for (int i = 0; i < value.Length; i++)
                {
                    AddItem(value[i]);
                }
            }
        }

        public override float Alpha
        {
            get => base.Alpha;
            set 
            {
                _alpha = value;
            }
        }

        public int SelectedIndex
        {
            get
            {
                int i = 0;
                foreach (SimpCheckbox it in _items)
                {
                    if (it.Checked) return i;
                    i++;
                }
                return -1;
            }
        }

        public string Value
        {
            get
            {
                foreach (SimpCheckbox it in _items)
                {
                    if (it.Checked) return it.Name;
                }
                return "";
            }
        }

        public Action<SimpCheckList, SimpCheckbox, bool> ChangeValue
        {
            get => _changeValue;
            set => _changeValue = value;
        }

        public SimpCheckList(string name, FontFamily fontFamily): base(name)
        {
            ChangeValue = (o, s, b) =>
            {
                foreach (SimpCheckbox it in _items)
                {
                    it.Checked = false;
                }
                s.Checked = true;
            };
        }

        public void AddItem(string item)
        {
            if (_items == null)
            {
                _items = new List<SimpCheckbox>();
            }

            SimpCheckbox chkItem = new SimpCheckbox($"{_name}_{item}", FontFamilySet.연성체)
            {
                Name = item,
                Text = item,
                Align = CONTROL_ALIGN.NONE,
                FontSize = _fontSize,
                Margin = 0.0f,
                Alpha = 0.0f,
                ChangeValue = (o, b) =>
                {
                    if (_changeValue != null) _changeValue(this, o, b);
                }
            };

            _items.Add(chkItem);
            AddChild(chkItem);

            _width = 1.0f;
            _height = 1.0f;
            _alpha = 0.0f;
            _isSelectable = false;
            _backColor = new Vertex3f(0, 0, 0);

            
        }

        public void Init()
        {
            if (_items != null)
            {
                _items[0].Checked = true;
                Debug.PrintLine(Value.ToString());
            }
        }

        public override void Update(int deltaTime)
        {
            _renderCount++;
            base.Update(deltaTime);

            if (!_isInit)
            {
                _isInit = true;
                Init();
            }

            if (_renderCount == 2)
            {
                float px = 0.0f;
                foreach (SimpCheckbox it in _items)
                {
                    it.Left = px;
                    it.FontSize = _fontSize;
                    px += it.Width * 1.3f;
                }
            }

            if (_renderCount > 1000) _renderCount = 0;
            
        }
    }
}
