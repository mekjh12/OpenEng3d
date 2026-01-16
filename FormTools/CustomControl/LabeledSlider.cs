using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FormTools
{
    [ToolboxItem(true)]  // 툴박스에 표시되도록 설정
    [Description("제목, 트랙바, 값을 표시하는 라벨이 결합된 컨트롤")]
    public class LabeledSlider : UserControl
    {
        private Label lblTitle;
        private TrackBar trackBar;
        private Label lblValue;

        [Browsable(false)]  // 프로퍼티 창에서 숨김
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public TrackBar TrackBar => trackBar;

        // 외부에서 접근할 속성들
        [Category("Appearance")]
        [Description("라벨에 표시될 제목")]
        [DefaultValue("Parameter")]
        public string Title
        {
            get => lblTitle.Text;
            set => lblTitle.Text = value;
        }

        [Category("Behavior")]
        [Description("트랙바의 현재 값")]
        [DefaultValue(0)]
        public int Value
        {
            get => trackBar.Value;
            set
            {
                trackBar.Value = value;
                UpdateValueText();
            }
        }

        [Category("Behavior")]
        [Description("트랙바의 최소값")]
        [DefaultValue(0)]
        public int Minimum
        {
            get => trackBar.Minimum;
            set => trackBar.Minimum = value;
        }

        [Category("Behavior")]
        [Description("트랙바의 최대값")]
        [DefaultValue(10)]
        public int Maximum
        {
            get => trackBar.Maximum;
            set => trackBar.Maximum = value;
        }

        [Category("Behavior")]
        [Description("작은 증감 단위")]
        [DefaultValue(1)]
        public int SmallChange
        {
            get => trackBar.SmallChange;
            set => trackBar.SmallChange = value;
        }

        [Category("Behavior")]
        [Description("큰 증감 단위")]
        [DefaultValue(5)]
        public int LargeChange
        {
            get => trackBar.LargeChange;
            set => trackBar.LargeChange = value;
        }

        [Category("Action")]
        [Description("값이 변경될 때 발생하는 이벤트")]
        public event EventHandler ValueChanged;

        public void SetText(string text)
        {
            lblValue.Text = text;
        }

        public LabeledSlider()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Size = new Size(400, 35);
            this.BackColor = SystemColors.Control;

            // 1. 이름 라벨 (왼쪽 배치)
            lblTitle = new Label();
            lblTitle.Text = "Parameter";
            lblTitle.Size = new Size(80, 15);
            lblTitle.Location = new Point(0, 10);
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;

            // 2. 트랙바 (가운데 배치)
            trackBar = new TrackBar();
            trackBar.Size = new Size(200, 45);
            trackBar.Location = new Point(85, 5);
            trackBar.Minimum = 0;
            trackBar.Maximum = 10;
            trackBar.TickStyle = TickStyle.None;
            trackBar.Scroll += (s, e) => {
                UpdateValueText();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            };

            // 3. 값 표시 라벨 (오른쪽 배치)
            lblValue = new Label();
            lblValue.Text = "0";
            lblValue.Size = new Size(50, 25);
            lblValue.Location = new Point(290, 10);
            lblValue.TextAlign = ContentAlignment.MiddleLeft;
            lblValue.ForeColor = Color.Blue;
            lblValue.Font = new Font(lblValue.Font, FontStyle.Bold);

            // 컨트롤에 추가
            this.Controls.Add(lblTitle);
            this.Controls.Add(trackBar);
            this.Controls.Add(lblValue);

            UpdateValueText();

            this.ResumeLayout(false);
        }

        private void UpdateValueText()
        {
            lblValue.Text = trackBar.Value.ToString();
        }
    }
}