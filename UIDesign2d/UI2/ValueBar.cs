namespace Ui2d
{
    public abstract class ValueBar : Panel
    {
        protected int _round = 0;
        protected float _value = 50.0f;
        protected float _maxValue = 100.0f;
        protected float _minValue = 0.0f;
        protected float _stepValue = 1.0f;

        public float StepValue
        {
            get => _stepValue;
            set => _stepValue = value;
        }

        public float MaxValue
        {
            get => _maxValue;
            set => _maxValue = value;
        }

        public float MinValue
        {
            get => _minValue;
            set => _minValue = value;
        }

        /// <summary>
        /// 소숫점 아래 자리를 잘라서 표시해 준다.
        /// </summary>
        public int Round
        {
            get => _round;
            set => _round = value;
        }

        public virtual float Value
        {
            get => _value;
            set
            {
                _value = value.Clamp(_minValue, _maxValue);
            }
        }

        protected ValueBar(string name) : base(name)
        {
            _alpha = 0.0f;
        }
    }
}
