using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FormTools
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    public class LabeledPictureBox : PictureBox
    {
        private string labelText = "";
        private Font labelFont = new Font("맑은 고딕", 9F, FontStyle.Bold);
        private Color labelBackColor = Color.FromArgb(180, 0, 0, 0); // 반투명 검정
        private Color labelForeColor = Color.White;
        private Padding labelPadding = new Padding(5, 2, 5, 2);

        [Category("Label")]
        [Description("라벨에 표시할 텍스트")]
        public string LabelText
        {
            get => labelText;
            set
            {
                labelText = value;
                Invalidate();
            }
        }

        [Category("Label")]
        [Description("라벨의 폰트")]
        public Font LabelFont
        {
            get => labelFont;
            set
            {
                labelFont = value;
                Invalidate();
            }
        }

        [Category("Label")]
        [Description("라벨의 배경색")]
        public Color LabelBackColor
        {
            get => labelBackColor;
            set
            {
                labelBackColor = value;
                Invalidate();
            }
        }

        [Category("Label")]
        [Description("라벨의 글자색")]
        public Color LabelForeColor
        {
            get => labelForeColor;
            set
            {
                labelForeColor = value;
                Invalidate();
            }
        }

        [Category("Label")]
        [Description("라벨의 여백")]
        public Padding LabelPadding
        {
            get => labelPadding;
            set
            {
                labelPadding = value;
                Invalidate();
            }
        }

        public LabeledPictureBox()
        {
            this.DoubleBuffered = true;
            this.SizeMode = PictureBoxSizeMode.StretchImage;
            this.BorderStyle = BorderStyle.FixedSingle;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 이미지가 없으면 빨간 X 그리기
            if (Image == null)
            {
                using (Pen redPen = new Pen(Color.Red, 1))
                {
                    // 왼쪽 위에서 오른쪽 아래로
                    g.DrawLine(redPen, 0, 0, Width - 1, Height - 1);
                    // 오른쪽 위에서 왼쪽 아래로
                    g.DrawLine(redPen, Width - 1, 0, 0, Height - 1);
                }
            }

            // 라벨 텍스트 그리기
            if (!string.IsNullOrEmpty(labelText))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                // 텍스트 크기 측정
                SizeF textSize = g.MeasureString(labelText, labelFont);

                // 라벨 배경 영역
                RectangleF labelRect = new RectangleF(
                    0,
                    0,
                    textSize.Width + labelPadding.Left + labelPadding.Right,
                    textSize.Height + labelPadding.Top + labelPadding.Bottom
                );

                // 배경 그리기
                using (SolidBrush backBrush = new SolidBrush(labelBackColor))
                {
                    g.FillRectangle(backBrush, labelRect);
                }

                // 텍스트 그리기
                using (SolidBrush textBrush = new SolidBrush(labelForeColor))
                {
                    g.DrawString(
                        labelText,
                        labelFont,
                        textBrush,
                        labelPadding.Left,
                        labelPadding.Top
                    );
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                labelFont?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
