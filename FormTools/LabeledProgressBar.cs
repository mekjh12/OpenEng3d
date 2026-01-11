using System;
using System.Drawing;
using System.Windows.Forms;

namespace FormTools
{
    public class LabeledSlider : UserControl
    {
        private Label lblTitle;
        private TrackBar trackBar;
        private Label lblValue;
        public TrackBar TrackBar => trackBar;
        // 외부에서 접근할 속성들
        public string Title { get => lblTitle.Text; set => lblTitle.Text = value; }
        public int Value { get => trackBar.Value; set { trackBar.Value = value; UpdateValueText(); } }
        public int Minimum { get => trackBar.Minimum; set => trackBar.Minimum = value; }
        public int Maximum { get => trackBar.Maximum; set => trackBar.Maximum = value; }

        public event EventHandler ValueChanged; // 값이 바뀔 때 발생하는 이벤트
        public void SetText(string text)
        {
            lblValue.Text = text;
        }

        public LabeledSlider()
        {
            this.Size = new Size(400, 45); // 적절한 기본 크기

            // 1. 이름 라벨 (왼쪽 배치)
            lblTitle = new Label();
            lblTitle.Text = "Parameter";
            lblTitle.Size = new Size(120, 25);
            lblTitle.Location = new Point(0, 10);
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;

            // 2. 트랙바 (가운데 배치)
            trackBar = new TrackBar();
            trackBar.Size = new Size(200, 45);
            trackBar.Location = new Point(125, 5);
            trackBar.TickStyle = TickStyle.None;
            trackBar.Scroll += (s, e) => {
                UpdateValueText();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            };

            // 3. 값 표시 라벨 (오른쪽 배치)
            lblValue = new Label();
            lblValue.Text = "0";
            lblValue.Size = new Size(50, 25);
            lblValue.Location = new Point(330, 10);
            lblValue.TextAlign = ContentAlignment.MiddleLeft;
            lblValue.ForeColor = Color.Blue;
            lblValue.Font = new Font(lblValue.Font, FontStyle.Bold);

            // 컨트롤에 추가
            this.Controls.Add(lblTitle);
            this.Controls.Add(trackBar);
            this.Controls.Add(lblValue);

            UpdateValueText();
        }

        private void UpdateValueText()
        {
            lblValue.Text = trackBar.Value.ToString();
        }
    }
}