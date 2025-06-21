using OpenGL;

namespace Ui2d
{
    public class HValueBar : ValueBar
    {
        public enum ProcessAlign { Left, Right }

        PictureBox _valuePicture;
        PictureBox _backPicture;

        ProcessAlign _processAlign = ProcessAlign.Left;

        public PictureBox PictureValueImage => _valuePicture;

        public ProcessAlign ProcessBarAlign
        {
            get => _processAlign;
            set
            {
                _processAlign = value;
            }
        }

        public new float Padding
        {
            get => _padding;
            set=> _padding = value;
        }

        /// <summary>
        /// 값의 방향을 수평으로 뒤집는다.
        /// </summary>
        public bool HorzFlip
        {
            set
            {
                if (_valuePicture != null)
                {
                    _valuePicture.HorzFlip = value;
                }
            }
        }
        
        public uint ValuePicture
        {
            get => _valuePicture.BackgroundImage;
            set => _valuePicture.BackgroundImage = value;
        }

        public uint BackgroundPicture
        {
            get => _backPicture.BackgroundImage;
            set
            {
                if (value > 1)
                {
                    _alpha = 1.0f;
                    _backPicture.Alpha = 1.0f;
                    _backPicture.BackgroundImage = value;
                }
                else
                {
                    _alpha = 0.0f;
                    _backPicture.Alpha = 0.0f;
                    _backPicture.BackgroundImage = value;
                }
            }
        }

        public new float Value
        {
            get => _value;

            set
            {
                _value = (value < _minValue) ? _minValue : value;
                _value = (value > _maxValue) ? _maxValue : value;
                float r = _value / (_maxValue - _minValue);
                _valuePicture.Width = r - 2 * _padding;

                if (_processAlign == ProcessAlign.Left)
                {
                    _valuePicture.Left = 1.0f - _padding - _valuePicture.Width;
                }

                if (_processAlign == ProcessAlign.Right)
                {
                    _valuePicture.Left = _padding;
                }
            }
        }

        public HValueBar(string name): base(name)
        {
            Bound = new Vertex4f(0, 0, 1, 1);
            _alpha = 0.0f;            

            _backPicture = new PictureBox($"{_name}_backPicture")
            {
                Alpha = 1.0f,
                IsSelectable = false,
            };
            AddChild(_backPicture);

            _valuePicture = new PictureBox($"{_name}_valuePicture")
            {
                BackgroundImage = UITextureLoader.Texture(name),
                Padding = _padding,
                HorzFlip = true,
                IsSelectable = false,
            };

            _backPicture.AddChild(_valuePicture);
        }


    }
}
