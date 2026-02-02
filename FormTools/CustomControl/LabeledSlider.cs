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

            // 전체 컨트롤 높이를 트랙바에 맞춰 살짝 키웁니다.
            this.Size = new Size(350, 45);

            // 1. 이름 라벨 (왼쪽 끝)
            lblTitle = new Label();
            lblTitle.Text = "Parameter";
            lblTitle.Size = new Size(70, 20); // 너비를 적절히 고정
            lblTitle.Location = new Point(5, 12);
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;

            // 2. 값 표시 라벨 (이름 바로 오른쪽)
            lblValue = new Label();
            lblValue.Text = "5";
            lblValue.Size = new Size(35, 20); // 숫자가 들어갈 공간 확보
            lblValue.Location = new Point(lblTitle.Right + 5, 12);
            lblValue.TextAlign = ContentAlignment.MiddleCenter;
            lblValue.ForeColor = Color.Blue;
            lblValue.Font = new Font(this.Font, FontStyle.Bold);

            // 3. 트랙바 (값 라벨이 끝나는 지점부터 시작)
            trackBar = new TrackBar();
            trackBar.Minimum = 1;
            trackBar.Maximum = 10;
            trackBar.Value = 5;
            trackBar.TickStyle = TickStyle.None;
            trackBar.AutoSize = false; // 중요: 자동 크기 조절을 꺼야 높이를 제어할 수 있습니다.

            // 값 라벨의 오른쪽(lblValue.Right)에 여백(+5)을 더해 시작 위치 설정
            trackBar.Location = new Point(lblValue.Right + 5, 10);
            // 전체 너비에서 앞의 라벨들이 차지한 만큼을 뺀 나머지 너비 할당
            trackBar.Size = new Size(this.Width - (lblValue.Right + 15), 30);

            // 폼 크기 조절 시 트랙바만 늘어나도록 설정
            trackBar.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            trackBar.Scroll += (s, e) => {
                UpdateValueText();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            };

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblValue);
            this.Controls.Add(trackBar);

            this.ResumeLayout(false);
        }

        private void UpdateValueText()
        {
            lblValue.Text = trackBar.Value.ToString();
            // 값 라벨의 오른쪽(lblValue.Right)에 여백(+5)을 더해 시작 위치 설정
            trackBar.Location = new Point(lblValue.Right + 5, 10);
            // 전체 너비에서 앞의 라벨들이 차지한 만큼을 뺀 나머지 너비 할당
            trackBar.Size = new Size(this.Width - (lblValue.Right + 15), 30);

            // 값이 바뀌어도 위치는 고정이므로 Location 수정 코드는 삭제해도 됩니다.
        }
    }
}