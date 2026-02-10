using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using Common;
using Occlusion;

namespace FormTools
{
    public partial class FormRaw16Viewer : Form
    {
        private Raw16HeightmapViewer _viewer;
        private PictureBox _pictureBox;

        private Label _infoLabel;
        private Label _heightLabel;
        private Label _statsLabel;
        private ComboBox _colorModeCombo;
        private CheckBox _autoContrastCheck;

        private Point _lastMousePos;

        public FormRaw16Viewer(string filename)
        {
            InitializeWindow();
            _viewer = new Raw16HeightmapViewer();
            if (!string.IsNullOrEmpty(filename) && File.Exists(filename))
            {
                LoadFile(filename);
            }
        }

        private void InitializeWindow()
        {
            // 윈도우 설정
            Text = "RAW 16bit Heightmap Viewer (1025x1025)";
            Width = 900;
            Height = 900;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(30, 30, 30);

            // 메인 레이아웃
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120)); // 컨트롤 영역
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // 이미지 영역

            // ──────────────────────────────────
            // 컨트롤 패널
            // ──────────────────────────────────
            Panel controlPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40)
            };

            // 로드 버튼
            Button loadButton = new Button
            {
                Text = "Load RAW File",
                Location = new Point(10, 10),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            loadButton.Click += OnLoadClick;

            // 컬러 모드 콤보박스
            Label colorLabel = new Label
            {
                Text = "Color Mode:",
                Location = new Point(140, 15),
                Size = new Size(80, 25),
                ForeColor = Color.White
            };

            _colorModeCombo = new ComboBox
            {
                Location = new Point(220, 12),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _colorModeCombo.Items.AddRange(new object[] { "Grayscale", "Terrain", "Rainbow", "Heatmap" });
            _colorModeCombo.SelectedIndex = 2;
            _colorModeCombo.SelectedIndexChanged += OnColorModeChanged;

            // Auto Contrast 체크박스
            _autoContrastCheck = new CheckBox
            {
                Text = "Auto Contrast",
                Location = new Point(350, 12),
                Size = new Size(120, 25),
                ForeColor = Color.White,
            };
            _autoContrastCheck.CheckedChanged += OnAutoContrastChanged;

            // 새로고침 버튼
            Button refreshButton = new Button
            {
                Text = "Refresh",
                Location = new Point(480, 10),
                Size = new Size(80, 35),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            refreshButton.Click += (s, e) => RefreshImage();

            // 정보 버튼
            Button infoButton = new Button
            {
                Text = "Info",
                Location = new Point(570, 10),
                Size = new Size(80, 35),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            infoButton.Click += (s, e) => _viewer?.PrintInfo();

            // 정보 라벨들
            _infoLabel = new Label
            {
                Location = new Point(10, 55),
                Size = new Size(800, 20),
                ForeColor = Color.LightGray,
                Text = "파일을 로드하세요 (1025x1025 RAW 16bit)"
            };

            _heightLabel = new Label
            {
                Location = new Point(10, 80),
                Size = new Size(600, 20),
                ForeColor = Color.Lime,
                Text = ""
            };

            _statsLabel = new Label
            {
                Location = new Point(620, 55),
                Size = new Size(400, 40),
                ForeColor = Color.Cyan,
                Text = ""
            };

            controlPanel.Controls.AddRange(new Control[] {
                loadButton, colorLabel, _colorModeCombo, _autoContrastCheck,
                refreshButton, infoButton, _infoLabel, _heightLabel, _statsLabel
            });

            // ──────────────────────────────────
            // PictureBox
            // ──────────────────────────────────
            _pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };
            _pictureBox.MouseMove += OnMouseMove;
            _pictureBox.Paint += OnPictureBoxPaint;

            // 레이아웃 조립
            mainLayout.Controls.Add(controlPanel, 0, 0);
            mainLayout.Controls.Add(_pictureBox, 0, 1);
            Controls.Add(mainLayout);

            // 키 이벤트
            KeyPreview = true;
            KeyDown += OnKeyDown;

            // 드래그 앤 드롭
            AllowDrop = true;
            DragEnter += OnDragEnter;
            DragDrop += OnDragDrop;
        }

        private void OnLoadClick(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "RAW 16bit Heightmap 열기 (1025x1025)";
                dialog.Filter = "RAW Files (*.raw)|*.raw|All Files (*.*)|*.*";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    LoadFile(dialog.FileName);
                }
            }
        }

        private void LoadFile(string filePath)
        {
            if (_viewer.LoadRaw16File(filePath))
            {
                _infoLabel.Text = $"로드됨: {Path.GetFileName(filePath)}";
                _statsLabel.Text = $"Range: {_viewer.MinHeight:F4} ~ {_viewer.MaxHeight:F4}";
                RefreshImage();

                Timer timer = new Timer()
                {
                    Interval = 500,
                    Enabled = true
                };
                timer.Tick += (o, ee) =>
                {
                    _viewer.AutoContrast = true;
                    _viewer.SetColorMode(Raw16HeightmapViewer.ColorMode.Rainbow);
                    RefreshImage();
                    timer.Enabled = false;
                };
            }
            else
            {
                _infoLabel.Text = "로드 실패! (파일 크기: 1025x1025x2바이트 필요)";
            }
        }

        private void RefreshImage()
        {
            if (!_viewer.IsLoaded) return;

            Cursor = Cursors.WaitCursor;

            // 기존 이미지 해제
            if (_pictureBox.Image != null)
            {
                _pictureBox.Image.Dispose();
            }

            // 새 이미지 생성 (PictureBox 크기에 맞게)
            _pictureBox.Image = _viewer.CreateResizedBitmap(_pictureBox.Width, _pictureBox.Height);

            Cursor = Cursors.Default;
        }

        private void OnColorModeChanged(object sender, EventArgs e)
        {
            Raw16HeightmapViewer.ColorMode mode = (Raw16HeightmapViewer.ColorMode)_colorModeCombo.SelectedIndex;
            _viewer.SetColorMode(mode);
            RefreshImage();
        }

        private void OnAutoContrastChanged(object sender, EventArgs e)
        {
            _viewer.ToggleAutoContrast();
            RefreshImage();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_viewer.IsLoaded) return;

            _lastMousePos = e.Location;

            // PictureBox 좌표를 1025x1025 좌표로 변환
            float scaleX = 1025f / _pictureBox.Width;
            float scaleY = 1025f / _pictureBox.Height;

            int x = (int)(e.X * scaleX);
            int y = (int)(e.Y * scaleY);

            float? height = _viewer.GetHeightAt(x, y);
            ushort? rawHeight = _viewer.GetRawHeightAt(x, y);

            if (height.HasValue && rawHeight.HasValue)
            {
                _heightLabel.Text = $"Pos: ({x}, {y}) | Height: {height.Value:F6} | Raw: {rawHeight.Value} (0x{rawHeight.Value:X4})";
            }
        }

        private void OnPictureBoxPaint(object sender, PaintEventArgs e)
        {
            // 크로스헤어 그리기
            if (_viewer.IsLoaded && _pictureBox.ClientRectangle.Contains(_lastMousePos))
            {
                using (Pen pen = new Pen(Color.Red, 1))
                {
                    e.Graphics.DrawLine(pen, _lastMousePos.X, 0, _lastMousePos.X, _pictureBox.Height);
                    e.Graphics.DrawLine(pen, 0, _lastMousePos.Y, _pictureBox.Width, _lastMousePos.Y);
                }
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.R:
                    RefreshImage();
                    break;
                case Keys.I:
                    _viewer?.PrintInfo();
                    break;
                case Keys.C:
                    _autoContrastCheck.Checked = !_autoContrastCheck.Checked;
                    break;
                case Keys.D1:
                    _colorModeCombo.SelectedIndex = 0;
                    break;
                case Keys.D2:
                    _colorModeCombo.SelectedIndex = 1;
                    break;
                case Keys.D3:
                    _colorModeCombo.SelectedIndex = 2;
                    break;
                case Keys.D4:
                    _colorModeCombo.SelectedIndex = 3;
                    break;
                case Keys.Escape:
                    Close();
                    break;
            }
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                LoadFile(files[0]);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _pictureBox.Image?.Dispose();
        }

        private void FormRaw16Viewer_Load(object sender, EventArgs e)
        {
            Timer timer = new Timer()
            {
                Interval = 500,
                Enabled = true
            };
            timer.Tick += (o, ee) =>
            {
                _viewer.AutoContrast = true;
                _viewer.SetColorMode(Raw16HeightmapViewer.ColorMode.Rainbow);
                RefreshImage();
                timer.Enabled = false;
            };
        }
    }
}