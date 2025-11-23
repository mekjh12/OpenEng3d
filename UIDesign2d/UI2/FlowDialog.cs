using OpenGL;
using System;
using System.Collections.Generic;

namespace Ui2d
{
    public class FlowDialog : PictureBox
    {
        public static uint TExT_GUID = 0;

        float TExT_INTIME_FLOW_VELOCITY = 0.1f;
        float TExT_ONTIME_FLOW_VELOCITY = 0.008f;
        float TExT_OUTTIME_FLOW_VELOCITY = 0.25f;
        public float LINE_SPACE = 1.01f;

        protected event Action<TimeControl, float> _inTime;
        protected event Action<TimeControl, float> _onTime;
        protected event Action<TimeControl, float> _outTime;

        public float INTIME_FLOW_VELOCITY
        {
            get => TExT_INTIME_FLOW_VELOCITY;
            set => TExT_INTIME_FLOW_VELOCITY = value;
        }

        public float ONTIME_FLOW_VELOCITY
        {
            get => TExT_ONTIME_FLOW_VELOCITY;
            set => TExT_ONTIME_FLOW_VELOCITY = value;
        }

        public float OUTTIME_FLOW_VELOCITY
        {
            get => TExT_OUTTIME_FLOW_VELOCITY;
            set => TExT_OUTTIME_FLOW_VELOCITY = value;
        }

        public Action<TimeControl, float> InTime
        {
            get => _inTime; 
            set => _inTime = value;
        }

        public Action<TimeControl, float> OnTime
        {
            get => _onTime;
            set => _onTime = value;
        }

        public Action<TimeControl, float> OutTime
        {
            get => _outTime;
            set => _outTime = value;
        }

        public class TimeControl
        {
            public Control Control;
            public float OnTime = 1.0f;
            public float InTime = 1.0f;
            public float OutTime = 1.0f;
        }

        List<TimeControl> _labels;

        public TimeControl LastControl
        {
            get
            {
                if (_labels.Count < 1)
                {
                    return null;
                }
                else
                {
                    return _labels[_labels.Count - 1];
                }
            }
        }

        public FlowDialog(string name) : base(name)
        {
            _labels = new List<TimeControl>();
        }

        public void Insert(TimeControl timeControl)
        {
            _labels.Add(timeControl);
            AddChild(timeControl.Control);
            TExT_GUID++;
        }

        public void Clear()
        {
            _labels.Clear();
        }

        public override void Update(int deltaTime)
        {
            for (int i = 0; i < _labels.Count; i++)
            {
                if (_labels[i].InTime > 0.0f)
                {
                    if (InTime != null)
                    {
                        InTime(_labels[i], _labels[i].InTime);
                        _labels[i].InTime -= TExT_INTIME_FLOW_VELOCITY;
                    }                    
                }

                if (_labels[i].InTime <= 0.0f && _labels[i].OnTime > 0.0f)
                {
                    if (OnTime != null)
                    {
                        OnTime(_labels[i], _labels[i].OnTime);
                        _labels[i].OnTime -= TExT_ONTIME_FLOW_VELOCITY;
                    }
                }

                if (_labels[i].InTime <= 0.0f && _labels[i].OnTime <= 0.0f && _labels[i].OutTime > 0.0f)
                {
                    if (OutTime != null)
                    {
                        OutTime(_labels[i], _labels[i].OutTime);
                        _labels[i].OutTime -= TExT_OUTTIME_FLOW_VELOCITY;
                    }
                }

                // remove
                if (_labels[i].InTime <= 0.0f && _labels[i].OnTime <= 0.0f && _labels[i].OutTime <= 0.0f)
                {
                    if (_labels[i].OnTime < _labels[i].Control.Height)
                    {
                        Control lbl = _labels[i].Control;
                        _labels.Remove(_labels[i]);
                        Remove(lbl);
                        lbl = null;
                    }
                }

            }

            base.Update(deltaTime);

        }
    }
}
