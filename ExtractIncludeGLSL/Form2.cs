using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ExtractIncludeGLSL
{
    public partial class Form2 : Form
    {
        private Bitmap sourceBitmap;
        private List<Rectangle> markers = new List<Rectangle>();

        public Form2()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "검은색 배경에 흰색 사각형 마커 검출기";
            this.Width = 800;
            this.Height = 600;
            this.StartPosition = FormStartPosition.CenterScreen;

            Button btnLoadImage = new Button
            {
                Text = "이미지 불러오기",
                Location = new Point(10, 10),
                Width = 150,
                Height = 30
            };
            btnLoadImage.Click += new EventHandler(btnLoadImage_Click);

            Button btnDetectMarkers = new Button
            {
                Text = "마커 검출",
                Location = new Point(170, 10),
                Width = 150,
                Height = 30
            };
            btnDetectMarkers.Click += new EventHandler(btnDetectMarkers_Click);

            PictureBox pictureBox = new PictureBox
            {
                Location = new Point(10, 50),
                Width = 500,
                Height = 500,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.pictureBox = pictureBox;

            RichTextBox resultTextBox = new RichTextBox
            {
                Location = new Point(520, 50),
                Width = 260,
                Height = 500,
                ReadOnly = true
            };
            this.resultTextBox = resultTextBox;

            this.Controls.Add(btnLoadImage);
            this.Controls.Add(btnDetectMarkers);
            this.Controls.Add(pictureBox);
            this.Controls.Add(resultTextBox);
        }

        private PictureBox pictureBox;
        private RichTextBox resultTextBox;

        private void btnLoadImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "이미지 파일|*.jpg;*.jpeg;*.png;*.bmp|모든 파일|*.*";
                openFileDialog.Title = "마커 이미지 선택";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        sourceBitmap = new Bitmap(openFileDialog.FileName);
                        pictureBox.Image = sourceBitmap;
                        resultTextBox.Clear();
                        resultTextBox.AppendText("이미지가 로드되었습니다.\n");
                        resultTextBox.AppendText("'마커 검출' 버튼을 클릭하여 분석을 시작하세요.\n");

                        markers.Clear();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("이미지를 불러오는 도중 오류가 발생했습니다: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnDetectMarkers_Click(object sender, EventArgs e)
        {
            if (sourceBitmap == null)
            {
                MessageBox.Show("먼저 이미지를 불러와 주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                resultTextBox.Clear();
                resultTextBox.AppendText("마커 검출 중...\n");

                DetectSquareInSquareMarkers();
                DisplayResults();
            }
            catch (Exception ex)
            {
                MessageBox.Show("마커 검출 중 오류가 발생했습니다: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DetectSquareInSquareMarkers()
        {
            markers.Clear();

            using (Bitmap binaryImage = BinarizeImage(sourceBitmap))
            {
                resultTextBox.AppendText("흑백 변환 완료\n");

                List<Rectangle> whiteRegions = FindWhiteRegions(binaryImage);
                resultTextBox.AppendText($"흰색 영역 {whiteRegions.Count}개 발견\n");

                FindBlackBorders(binaryImage, whiteRegions);
            }
        }

        private Bitmap BinarizeImage(Bitmap original)
        {
            Bitmap result = new Bitmap(original.Width, original.Height);

            int threshold = 50;

            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    Color originalColor = original.GetPixel(x, y);
                    int grayValue = (int)(originalColor.R * 0.3 + originalColor.G * 0.59 + originalColor.B * 0.11);

                    byte pixelValue = (byte)(grayValue > threshold ? 255 : 0);
                    Color newColor = Color.FromArgb(pixelValue, pixelValue, pixelValue);
                    result.SetPixel(x, y, newColor);
                }
            }

            return result;
        }

        private List<Rectangle> FindWhiteRegions(Bitmap binaryImage)
        {
            List<Rectangle> regions = new List<Rectangle>();
            bool[,] visited = new bool[binaryImage.Width, binaryImage.Height];

            int step = 1;

            for (int y = 0; y < binaryImage.Height; y += step)
            {
                for (int x = 0; x < binaryImage.Width; x += step)
                {
                    if (visited[x, y] || binaryImage.GetPixel(x, y).R != 255)
                        continue;

                    List<Point> component = new List<Point>();
                    FloodFill(binaryImage, visited, x, y, component, true);

                    if (component.Count > 5)
                    {
                        int minX = int.MaxValue;
                        int minY = int.MaxValue;
                        int maxX = int.MinValue;
                        int maxY = int.MinValue;

                        foreach (Point p in component)
                        {
                            minX = Math.Min(minX, p.X);
                            minY = Math.Min(minY, p.Y);
                            maxX = Math.Max(maxX, p.X);
                            maxY = Math.Max(maxY, p.Y);
                        }

                        int width = maxX - minX + 1;
                        int height = maxY - minY + 1;

                        float ratio = (float)width / height;
                        if (ratio >= 0.5f && ratio <= 2.0f)
                        {
                            regions.Add(new Rectangle(minX, minY, width, height));
                        }
                    }
                }
            }

            return regions;
        }

        private void FindBlackBorders(Bitmap binaryImage, List<Rectangle> whiteRegions)
        {
            foreach (Rectangle whiteRect in whiteRegions)
            {
                int expansionSize = Math.Max(5, Math.Max(whiteRect.Width, whiteRect.Height) / 5);

                Rectangle expandedRect = new Rectangle(
                    Math.Max(0, whiteRect.X - expansionSize),
                    Math.Max(0, whiteRect.Y - expansionSize),
                    Math.Min(binaryImage.Width - whiteRect.X, whiteRect.Width + expansionSize * 2),
                    Math.Min(binaryImage.Height - whiteRect.Y, whiteRect.Height + expansionSize * 2)
                );

                int blackPixelCount = 0;
                int totalPixelCount = 0;

                for (int y = expandedRect.Y; y < expandedRect.Y + expandedRect.Height; y++)
                {
                    for (int x = expandedRect.X; x < expandedRect.X + expandedRect.Width; x++)
                    {
                        if (x >= whiteRect.X && x < whiteRect.X + whiteRect.Width &&
                            y >= whiteRect.Y && y < whiteRect.Y + whiteRect.Height)
                            continue;

                        if (x >= 0 && y >= 0 && x < binaryImage.Width && y < binaryImage.Height)
                        {
                            totalPixelCount++;
                            if (binaryImage.GetPixel(x, y).R < 128)
                                blackPixelCount++;
                        }
                    }
                }

                float blackRatio = (float)blackPixelCount / totalPixelCount;

                if (blackRatio > 0.3f)
                {
                    markers.Add(expandedRect);
                    resultTextBox.AppendText($"마커 발견: 흰색 영역 {whiteRect.Width}x{whiteRect.Height}, 검은색 비율: {blackRatio:P0}\n");
                }
            }
        }

        private void FloodFill(Bitmap image, bool[,] visited, int x, int y, List<Point> component, bool isWhite)
        {
            Stack<Point> stack = new Stack<Point>();
            stack.Push(new Point(x, y));

            while (stack.Count > 0)
            {
                Point p = stack.Pop();

                if (p.X < 0 || p.Y < 0 || p.X >= image.Width || p.Y >= image.Height)
                    continue;

                if (visited[p.X, p.Y])
                    continue;

                Color pixelColor = image.GetPixel(p.X, p.Y);
                bool isPixelWhite = pixelColor.R > 128;

                if (isWhite != isPixelWhite)
                    continue;

                visited[p.X, p.Y] = true;
                component.Add(p);

                if (component.Count > 10000)
                    return;

                stack.Push(new Point(p.X + 1, p.Y));
                stack.Push(new Point(p.X - 1, p.Y));
                stack.Push(new Point(p.X, p.Y + 1));
                stack.Push(new Point(p.X, p.Y - 1));
            }
        }

        private void DisplayResults()
        {
            resultTextBox.AppendText($"\n총 {markers.Count}개의 마커가 발견되었습니다.\n\n");

            for (int i = 0; i < markers.Count; i++)
            {
                Rectangle marker = markers[i];
                resultTextBox.AppendText($"마커 #{i + 1}:\n");
                resultTextBox.AppendText($"  위치: X={marker.X}, Y={marker.Y}\n");
                resultTextBox.AppendText($"  크기: {marker.Width}x{marker.Height}\n\n");
            }

            if (pictureBox.Image != null)
            {
                Bitmap markedImage = new Bitmap(sourceBitmap);
                using (Graphics g = Graphics.FromImage(markedImage))
                {
                    Color[] markerColors = { Color.Red, Color.Blue, Color.Green, Color.Magenta };

                    for (int i = 0; i < markers.Count; i++)
                    {
                        Color color = markerColors[i % markerColors.Length];
                        Pen pen = new Pen(color, 3);

                        // 마커 영역 테두리 그리기
                        g.DrawRectangle(pen, markers[i]);

                        // 마커 번호 표시
                        Font labelFont = new Font("Arial", 16, FontStyle.Bold);
                        g.DrawString((i + 1).ToString(), labelFont,
                            new SolidBrush(color),
                            markers[i].X, Math.Max(0, markers[i].Y - 25));
                    }

                    // 2번 마커(상단 왼쪽)와 4번 마커(하단 오른쪽)를 기준으로 사각형 그리기
                    if (markers.Count >= 4) // 최소 4개의 마커가 있어야 함
                    {
                        Rectangle marker2 = markers[1]; // 2번 마커 (인덱스 1)
                        Rectangle marker4 = markers[3]; // 4번 마커 (인덱스 3)

                        // 2번 마커의 좌상단과 4번 마커의 우하단을 기준으로 사각형 생성
                        int rectX = marker2.X; // 2번 마커의 X (좌상단)
                        int rectY = marker2.Y; // 2번 마커의 Y (좌상단)
                        int rectWidth = (marker4.X + marker4.Width) - marker2.X; // 4번 마커의 우측 끝 - 2번 마커의 좌측
                        int rectHeight = (marker4.Y + marker4.Height) - marker2.Y; // 4번 마커의 하단 끝 - 2번 마커의 상단

                        Rectangle newRect = new Rectangle(rectX, rectY, rectWidth, rectHeight);

                        // 사각형 그리기
                        Pen rectPen = new Pen(Color.Cyan, 3); // 사각형 테두리 색상 및 두께
                        g.DrawRectangle(rectPen, newRect);

                        // 사각형 내부에 12행 5열 격자선 그리기
                        Pen gridPen = new Pen(Color.Yellow, 2); // 격자선 색상 및 두께

                        // 5열로 나누기 (수직선)
                        float colWidth = (float)newRect.Width / 5;
                        for (int col = 1; col < 5; col++)
                        {
                            float x = newRect.X + col * colWidth;
                            g.DrawLine(gridPen, x, newRect.Y, x, newRect.Y + newRect.Height);
                        }

                        // 12행으로 나누기 (수평선)
                        float rowHeight = (float)newRect.Height / 12;
                        for (int row = 1; row < 12; row++)
                        {
                            float y = newRect.Y + row * rowHeight;
                            g.DrawLine(gridPen, newRect.X, y, newRect.X + newRect.Width, y);
                        }

                        // 1번부터 10번 문항의 선택지 읽기 (1행과 12행 제외, 2행~11행)
                        resultTextBox.AppendText("\n문항별 선택지 결과:\n");
                        for (int question = 1; question <= 10; question++)
                        {
                            float rowY = newRect.Y + (question + 1 - 1) * rowHeight; // 2행부터 시작 (question 1 = 2번째 행)
                            int selected = ReadChoice(newRect, rowY, colWidth);
                            resultTextBox.AppendText($"문항 {question}: {selected}\n");
                        }
                    }
                    else
                    {
                        resultTextBox.AppendText("마커가 4개 미만이어서 사각형을 생성할 수 없습니다.\n");
                    }
                }

                pictureBox.Image = markedImage;
            }

            resultTextBox.AppendText("처리가 완료되었습니다.\n");
        }

        private int ReadChoice(Rectangle rect, float rowY, float colWidth)
        {
            Bitmap bitmap = new Bitmap(sourceBitmap);
            int selected = 0;

            // 각 열(선택지 1, 2, 3, 4, 5)을 확인
            for (int col = 0; col < 5; col++)
            {
                float colX = rect.X + col * colWidth + (colWidth / 2); // 각 열의 중앙
                int x = (int)colX;
                int y = (int)(rowY + (rowHeight / 2)); // 행의 중앙

                if (x >= 0 && x < bitmap.Width && y >= 0 && y < bitmap.Height)
                {
                    Color pixelColor = bitmap.GetPixel(x, y);
                    int grayValue = (int)(pixelColor.R * 0.3 + pixelColor.G * 0.59 + pixelColor.B * 0.11);

                    // 흰색 점(255에 가까운 값)으로 간주하는 임계값
                    if (grayValue > 200) // 흰색에 가까운 픽셀로 판단
                    {
                        selected = col + 1; // 1, 2, 3, 4, 5 선택지
                        break;
                    }
                }
            }

            bitmap.Dispose();
            return selected == 0 ? 0 : selected; // 선택되지 않으면 0 반환
        }

        private float rowHeight; // 행 높이 변수 (DisplayResults에서 계산 후 사용)
    }
}