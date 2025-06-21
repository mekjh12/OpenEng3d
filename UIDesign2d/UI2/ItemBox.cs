using OpenGL;

namespace Ui2d
{
    public class ItemBox : PictureBox
    {
        public enum KindType { A, B, C, D, E, F, G, H, I };

        static Vertex2f _offsetClick;

        protected PictureBox _itemImage;
        protected Item _item;
        protected ImageLabel _countLabel;
        protected FontFamily _fontFamily;
        protected PictureBox _powerBar;

        protected int _selectedCount = 0;
        protected PictureBox _selectedImage;
        protected Label _selectedCountLabel;
        protected string _selectedText;

        public string SelectedText
        {
            get => _selectedText;
            set=>_selectedText = value;
        }

        protected KindType _kind = KindType.A;

        public KindType Kind
        {
            get => _kind;
            set => _kind = value;
        }

        public int SelectedCount
        {
            get => _selectedCount;
            set
            {
                _selectedCount = value;
                if (_selectedCount < 0)
                {
                    _selectedCount = 0;
                }
                if (_selectedCountLabel != null)
                {
                    _selectedCountLabel.Text = _selectedCount.ToString();
                }
            }
        }

        public bool IsEmpty => _item == null || _item.ItemImageName == "null";

        public ImageLabel CountLabel => _countLabel;

        public PictureBox ItemImage => _itemImage;

        public uint ItemImageID => _itemImage.BackgroundImage;

        public Item Item
        {
            get => _item;
            set
            {
                _item = value;
                if (_item != null)
                {
                    _itemImage.BackgroundImage = UITextureLoader.Texture(_item.ItemImageName);
                    if (_item.DisposableProduct)
                        _countLabel.Text = _item.Count.ToString();

                    if (!_item.DisposableProduct)
                    {
                        if (UIEngine.UI2d<PictureBox>(_name + "_powerbar") == null)
                        {
                            _powerBar = new PictureBox(_name + "_powerbar")
                            {
                                Bound = new Vertex4f(0, 0.9f, _item.Power, 0.1f),
                                BackgroundImage = UITextureLoader.Texture("ingame_arrow_text_select_bg")
                            };
                            _itemImage.AddChild(_powerBar);
                        }
                        else
                        {
                            _powerBar.Bound = new Vertex4f(0, 0.9f, _item.Power, 0.1f);
                        }
                        _powerBar.IsVisible = true;
                    }

                }
                else
                {
                    _itemImage.BackgroundImage = UITextureLoader.Transparent;
                    if (_powerBar != null) _powerBar.IsVisible = false;
                    _selectedCount = 0;
                    _selectedCountLabel.Text = "";
                }

            }
        }

        public ItemBox(string name, FontFamily fontFamily) : base(name)
        {
            _fontFamily = fontFamily;

            _itemImage = new PictureBox(name + "_itemImage")
            {
                Bound = new Vertex4f(0, 0, 1, 1),
                IsSelectable = false,
            };
            AddChild(_itemImage);

            _selectedImage = new PictureBox(name + "_selectedImage")
            {
                Bound = new Vertex4f(0, 0, 1, 1),
                IsSelectable = false,
                IsVisible = false,
                BackgroundImage = UITextureLoader.Texture("ingame_itembar_outline"),
            };
            AddChild(_selectedImage);
            
            _selectedCountLabel = new Label(name + "_selectedCountLabel")
            {
                Bound = new Vertex4f(0.1f, 0.1f, 1, 1),
                FontSize = 1.3f,
                ForeColor = new Vertex3f(0, 0.5f, 0),
                IsSelectable = false,
                Text = SelectedCount.ToString(),
                IsVisible = false,
            };
            AddChild(_selectedCountLabel);  

            _countLabel = new ImageLabel(name + "_countLabel", fontFamily)
            {
                Bound = new Vertex4f(0.28f, 0.65f, 0.7f, 0.32f),
                FontSize = 1.7f,
                BackgroundImage = UITextureLoader.Texture("Interaction_bg"),
                Text = "0",
                AutoSize = false,
                TextHorizonAlign = ImageLabel.ALIGN.RIGHT,
                TextVerticalAlign = ImageLabel.VALIGN.MIDDLE,
                Alpha = 0.5f,
                IsSelectable = true,
            };
            AddChild(_countLabel);

            _countLabel.MouseOut += (o, x, y) =>
            {
                UIEngine.MouseImageOut();
            };

            _countLabel.MouseIn += (o, x, y) =>
            {
                if (!IsEmpty)
                {
                    UIEngine.MouseImageOver();
                }
            };

            _mouseOut += (o, x, y) =>
            {
                UIEngine.MouseImageOut();
            };

            _mouseIn += (o, x, y) =>
            {
                if (!IsEmpty)
                {
                    UIEngine.MouseImageOver();
                }
            };

            _mouseDown += (o, x, y) =>
            {
                _itemImage.Location = new Vertex2f(0.1f, 0.1f);
                _itemImage.Size = new Vertex2f(0.8f, 0.8f);
                //Console.WriteLine($"{_name}_itemImage mouse down! default");
            };

            _mouseUp += (o, x, y) =>
            {
                _itemImage.Size = Vertex2f.One;
                _itemImage.Location = Vertex2f.Zero;
                //Console.WriteLine($"{_name}_itemImage mouse up! default");
            };

            _dragStart += (o, fx, fy, dx, dy) =>
            {
                Vertex2f pos = this.GetRelativeCoordinate2f(fx, fy);
                _offsetClick = pos;
                this.PushTopest();
            };

            _dragEnd += (o, fx, fy, dx, dy) =>
            {                
                _itemImage.Location = Vertex2f.Zero;
                _itemImage.Size = Vertex2f.One;
            };

            _dragdrop += (o, fx, fy, dx, dy) =>
            {
                Vertex2f pos = this.GetRelativeCoordinate2f(fx, fy);
                _itemImage.Location = pos - _offsetClick;
            };

        }

        public override void Update(int deltaTime)
        {
            if (!_isVisible) return;

            base.Update(deltaTime);

            _countLabel.IsVisible = _item == null ? false : _item.DisposableProduct;
            _selectedImage.IsVisible = _selectedCount > 0;
            _selectedCountLabel.IsVisible = _selectedCount > 0;
        }

        public void CountUp(int deltaCount = 1)
        {
            if (_item != null)
            {
                _item.CountUp(deltaCount);
                _countLabel.Text = _item.Count.ToString();
            }
        }

        public void CountDown(int deltaCount = 1)
        {
            if (_item != null)
            {
                _item.CountDown(deltaCount);
                _countLabel.Text = _item.Count.ToString();
            }
        }


    }
}
