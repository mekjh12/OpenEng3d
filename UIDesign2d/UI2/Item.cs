using System.Xml.Linq;

namespace Ui2d
{
    public class Item
    {
        protected string _name = "";
        protected string _itemImageName = "";
        protected float _power = 1.0f;
        protected bool _disposableProduct = true; // 일회성 아이템
        protected int _count;
        protected float _price = 1.0f;
        protected string _desc = "";

        public string Name => _name;

        public string Description
        {
            get => _desc;
            set => _desc = value;
        }
        
        public float Price
        {
            get => _price;
            set => _price = value;
        }

        public int Count
        {
            get => _count;
            set => _count = _disposableProduct ? value : 1;
        }

        public string ItemImageName => _itemImageName;

        public bool DisposableProduct
        {
            get => _disposableProduct;
            set => _disposableProduct = value;
        }

        public float Power
        {
            get => _power;
            set
            {
                _power = value;
                if (_power > 1) _power = 1;
                if (_power < 0) _power = 0;
            }
        }

        public float PowerUp
        {
            set => _power += value;
        }

        public Item(string name, string itemImageName, float price = 1.0f, bool disposableProduct = true)
        {
            _name = name;
            _price = price;
            _itemImageName = itemImageName;
            _disposableProduct = disposableProduct;
            _power = _disposableProduct ? 0.0f : 1.0f;
        }

        public void CountUp(int deltaCount = 1)
        {
            if (_disposableProduct)
                _count += deltaCount;
        }

        public void CountDown(int deltaCount = 1)
        {
            if (_disposableProduct)
            {
                _count -= deltaCount;
                if (_count < 0) _count = 0;
            }
        }
    }
}
