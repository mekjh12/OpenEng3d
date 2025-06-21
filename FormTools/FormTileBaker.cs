using Shader;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Terrain;
using ZetaExt;

namespace FormTools
{
    public partial class FormTileBaker : Form
    {
        public const int DEFAULT_TILE_SIZE = 250;

        Bitmap _bitmap;

        public FormTileBaker()
        {
            InitializeComponent();
        }

        private void FormTileBaker_Load(object sender, EventArgs e)
        {
            
        }

        public void SplitHeightmapAsync(string sourceImagePath, string outputDirectory, int tileSize, float a, float b)
        {
            // 출력 디렉토리 확인 및 생성
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
            // 원본 이미지 로드
            using (Bitmap sourceImage = new Bitmap(sourceImagePath))
            {
                // 이미지 크기 검증
                if (sourceImage.Width != 2000 || sourceImage.Height != 2000)
                {
                    throw new ArgumentException($"원본 이미지는 2000x2000 크기여야 합니다. 현재 크기: {sourceImage.Width}x{sourceImage.Height}");
                }
                // 타일 개수 계산
                int tilesPerRow = sourceImage.Width / tileSize;
                int tilesPerColumn = sourceImage.Height / tileSize;
                // 각 타일 처리
                for (int y = 0; y < tilesPerColumn; y++)
                {
                    for (int x = 0; x < tilesPerRow; x++)
                    {
                        ProcessTile(sourceImage, outputDirectory, x, y, tileSize, a, b);
                    }
                }
                Print($"높이맵 분할 완료. {tilesPerRow * tilesPerColumn}개 타일 생성됨.");
            }
        }
        private void ProcessTile(Bitmap sourceImage, string outputDirectory, int tileX, int tileY, int tileSize, float a, float b)
        {
            string tileFileName = $"tile_{tileX}_{tileY}.png";
            string tileFilePath = Path.Combine(outputDirectory, tileFileName);

            // UI 스레드에서 UI 업데이트
            this.Invoke((MethodInvoker)delegate {
                Print($"({tileX},{tileY}) {tileFileName} 타일을 로드함.");
                this.textBox2.Refresh();
                Application.DoEvents();
            });

            // 타일 영역 계산
            Rectangle tileRect = new Rectangle(tileX * tileSize, tileY * tileSize, tileSize, tileSize);

            // 타일 비트맵 추출
            using (Bitmap tileBitmap = sourceImage.Clone(tileRect, sourceImage.PixelFormat))
            {
                // 선형 변환 적용 (y = ax + b) - LockBits 사용
                if (a != 1.0f || b != 0.0f)
                {
                    // 픽셀 형식이 32bppArgb인지 확인
                    if (tileBitmap.PixelFormat != PixelFormat.Format32bppArgb)
                    {
                        // 32bppArgb 형식으로 변환
                        using (Bitmap convertedBitmap = new Bitmap(tileBitmap.Width, tileBitmap.Height, PixelFormat.Format32bppArgb))
                        {
                            using (Graphics g = Graphics.FromImage(convertedBitmap))
                            {
                                g.DrawImage(tileBitmap, 0, 0);
                            }

                            ApplyLinearTransformWithLockBits(convertedBitmap, a, b);
                            convertedBitmap.Save(tileFilePath, ImageFormat.Png);
                        }
                    }
                    else
                    {
                        // 이미 32bppArgb 형식이면 직접 처리
                        ApplyLinearTransformWithLockBits(tileBitmap, a, b);
                        tileBitmap.Save(tileFilePath, ImageFormat.Png);
                    }
                }
                else
                {
                    // 변환이 필요 없으면 그냥 저장
                    tileBitmap.Save(tileFilePath, ImageFormat.Png);
                }
            }
        }

        private void ApplyLinearTransformWithLockBits(Bitmap bitmap, float a, float b)
        {
            // 이미지 데이터를 잠금
            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite,
                bitmap.PixelFormat);

            try
            {
                // 이미지 데이터의 포인터 가져오기
                IntPtr ptr = bitmapData.Scan0;

                // 이미지 데이터의 크기 계산
                int bytes = bitmapData.Stride * bitmap.Height;
                byte[] rgbValues = new byte[bytes];

                // 이미지 데이터를 배열로 복사
                System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

                // Format32bppArgb 형식에서는 픽셀당 4바이트(BGRA 순서)
                int bytesPerPixel = 4;

                // 모든 픽셀에 대해 선형 변환 적용
                for (int i = 0; i < rgbValues.Length; i += bytesPerPixel)
                {
                    // BGR 순서로 저장됨 (인덱스 0: B, 1: G, 2: R, 3: A)
                    byte blue = rgbValues[i];
                    byte green = rgbValues[i + 1];
                    byte red = rgbValues[i + 2];
                    // alpha는 변경하지 않음

                    // 선형 변환 적용
                    rgbValues[i] = (byte)Clamp((int)(a * blue + b), 0, 255);       // B
                    rgbValues[i + 1] = (byte)Clamp((int)(a * green + b), 0, 255);  // G
                    rgbValues[i + 2] = (byte)Clamp((int)(a * red + b), 0, 255);    // R
                }

                // 변환된 데이터를 다시 이미지로 복사
                System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);
            }
            finally
            {
                // 이미지 잠금 해제
                bitmap.UnlockBits(bitmapData);
            }
        }

        // 값의 범위를 제한하는 헬퍼 함수
        private int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// 원본 이미지를 저해상도(125x125)로 변환하여 /low 폴더에 저장합니다.
        /// </summary>
        /// <param name="sourceImagePath">원본 이미지 경로</param>
        /// <param name="baseOutputDirectory">기본 출력 디렉토리</param>
        /// <param name="size">저장할 이미지의 크기 (기본 125)</param>
        private void SaveLowResolutionImage(string sourceImagePath, string baseOutputDirectory, int size = 125, float a = 1.0f, float b = 0.0f)
        {
            try
            {
                // low 폴더 경로 설정
                string lowResDirectory = Path.Combine(baseOutputDirectory, "");
                // 출력 디렉토리 확인 및 생성
                if (!Directory.Exists(lowResDirectory))
                {
                    Directory.CreateDirectory(lowResDirectory);
                }
                string fileName = Path.GetFileName(sourceImagePath);
                string outputPath = Path.Combine(lowResDirectory, "" + fileName);
                // UI 스레드에서 UI 업데이트
                this.Invoke((MethodInvoker)delegate {
                    Print($"저해상도 이미지 생성 중: {fileName} (크기: {size}x{size}, 변환: y = {a}x + {b})");
                    this.textBox2.Refresh();
                    Application.DoEvents();
                });

                // 원본 이미지 로드
                using (Bitmap sourceImage = new Bitmap(sourceImagePath))
                {
                    // 저해상도 이미지 생성
                    using (Bitmap lowResImage = new Bitmap(size, size))
                    {
                        // 먼저 이미지 크기 조정
                        using (Graphics g = Graphics.FromImage(lowResImage))
                        {
                            // 높은 품질의 보간 방식 설정
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                            // 배경 초기화
                            g.Clear(Color.Black);
                            // 이미지 크기 조정하여 그리기
                            g.DrawImage(sourceImage, new Rectangle(0, 0, size, size),
                                new Rectangle(0, 0, sourceImage.Width, sourceImage.Height),
                                GraphicsUnit.Pixel);
                        }

                        // 선형 변환 적용 (y = ax + b)
                        if (a != 1.0f || b != 0.0f)  // 기본값과 다를 경우에만 적용
                        {
                            // 각 픽셀에 대한 선형 변환 적용
                            for (int y = 0; y < lowResImage.Height; y++)
                            {
                                for (int x = 0; x < lowResImage.Width; x++)
                                {
                                    Color originalColor = lowResImage.GetPixel(x, y);

                                    // 각 색상 채널에 선형 변환 적용
                                    int newR = Clamp((int)(a * originalColor.R + b), 0, 255);
                                    int newG = Clamp((int)(a * originalColor.G + b), 0, 255);
                                    int newB = Clamp((int)(a * originalColor.B + b), 0, 255);

                                    // 변환된 색상 적용
                                    lowResImage.SetPixel(x, y, Color.FromArgb(originalColor.A, newR, newG, newB));
                                }
                            }
                        }

                        // PNG 형식으로 저장
                        lowResImage.Save(outputPath, ImageFormat.Png);
                    }
                }

                // UI 스레드에서 UI 업데이트
                this.Invoke((MethodInvoker)delegate {
                    Print($"저해상도 이미지 저장 완료: {outputPath}");
                    this.textBox2.Refresh();
                    Application.DoEvents();
                });
            }
            catch (Exception ex)
            {
                // 오류 처리
                this.Invoke((MethodInvoker)delegate {
                    Print($"저해상도 이미지 생성 중 오류 발생: {ex.Message}");
                    this.textBox2.Refresh();
                    Application.DoEvents();
                });
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

        private void Print(string txt)
        {
            this.textBox2.Text = txt + "\r\n" + this.textBox2.Text;
        }

        /// <summary>
        /// 높이맵 파일명에서 위치 정보를 분석하고 각 이미지의 위치를 계산합니다.
        /// </summary>
        private Dictionary<string, Rectangle> AnalyzeHeightmapPositions(string[] filePaths)
        {
            Dictionary<string, Rectangle> positions = new Dictionary<string, Rectangle>();

            foreach (string filePath in filePaths)
            {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);

                    // 파일명에서 region{X}x{Y} 형식 찾기
                    int regionIndex = fileName.IndexOf("region");
                    if (regionIndex < 0) continue;

                    int xIndex = fileName.IndexOf('x', regionIndex);
                    if (xIndex < 0) continue;

                    // X, Y 값 추출
                    if (!int.TryParse(fileName.Substring(regionIndex + 6, xIndex - (regionIndex + 6)), out int x))
                        continue;

                    if (!int.TryParse(fileName.Substring(xIndex + 1), out int y))
                        continue;

                    // 타일 크기를 125x125로 고정
                    int tileWidth = 125;
                    int tileHeight = 125;

                    // 위치 계산 (각 타일이 125x125 크기로 고정)
                    // X 좌표는 그대로, Y 좌표는 반전 (음수 Y값은 상단에 위치)
                    int posX = x * tileWidth;
                    int posY = y * tileHeight; // Y 좌표 반전

                    positions[filePath] = new Rectangle(posX, posY, tileWidth, tileHeight);
                    Print($"위치 분석: {Path.GetFileName(filePath)} -> 위치 ({x}, {y}), 계산된 위치 ({posX}, {posY}), 크기 {tileWidth}x{tileHeight}");
                }
                catch (Exception ex)
                {
                    Print($"파일 분석 중 오류: {Path.GetFileName(filePath)} - {ex.Message}");
                }
            }

            return positions;
        }

        private void lowFileConvertToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Multiselect = true;
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string[] filenames = this.openFileDialog1.FileNames;

                if (filenames.Length == 0)
                {
                    Print("선택된 파일이 없습니다.");
                    return;
                }

                try
                {
                    // 처리할 이미지 파일명 출력
                    foreach (string filename in filenames)
                    {
                        Print($"처리할 파일: {Path.GetFileName(filename)}");
                    }

                    // 이미지 영역 계산을 위해 모든 이미지 로드 및 분석
                    Dictionary<string, Rectangle> imagePositions = AnalyzeHeightmapPositions(filenames);

                    if (imagePositions.Count == 0)
                    {
                        Print("병합할 수 있는 높이맵이 없습니다.");
                        return;
                    }

                    // 전체 이미지 크기 계산
                    int minX = int.MaxValue, minY = int.MaxValue;
                    int maxX = int.MinValue, maxY = int.MinValue;

                    foreach (var pos in imagePositions.Values)
                    {
                        minX = Math.Min(minX, pos.X);
                        minY = Math.Min(minY, pos.Y);
                        maxX = Math.Max(maxX, pos.X + pos.Width);
                        maxY = Math.Max(maxY, pos.Y + pos.Height);
                    }

                    int totalWidth = maxX - minX;
                    int totalHeight = maxY - minY;

                    Print($"병합된 이미지 크기: {totalWidth}x{totalHeight} (125x125 크기 타일 기준)");

                    // 최종 이미지 생성
                    using (Bitmap mergedImage = new Bitmap(totalWidth, totalHeight))
                    {
                        using (Graphics g = Graphics.FromImage(mergedImage))
                        {
                            g.Clear(Color.Black); // 배경색 설정

                            // 각 이미지를 올바른 위치에 그리기
                            foreach (string filename in filenames)
                            {
                                if (!imagePositions.ContainsKey(filename))
                                    continue;

                                Rectangle pos = imagePositions[filename];
                                // 원점(minX, minY)를 (0,0)으로 변환
                                Rectangle drawRect = new Rectangle(
                                    pos.X - minX, pos.Y - minY, pos.Width, pos.Height);

                                Print($"이미지 배치 중: {Path.GetFileName(filename)} -> ({drawRect.X},{drawRect.Y})");

                                using (Bitmap img = new Bitmap(filename))
                                {
                                    // 이미지를 125x125 크기에 맞게 그리기
                                    g.DrawImage(img, drawRect, 0, 0, img.Width, img.Height, GraphicsUnit.Pixel);
                                }
                            }
                        }

                        // 저장 경로 설정
                        string directory = Path.GetDirectoryName(filenames[0]);
                        string saveFilePath = Path.Combine(directory, "merged_heightmap.png");

                        // 저장 대화상자 표시
                        using (SaveFileDialog saveDialog = new SaveFileDialog())
                        {
                            saveDialog.Filter = "PNG 파일 (*.png)|*.png";
                            saveDialog.FileName = "merged_heightmap.png";
                            saveDialog.InitialDirectory = directory;

                            if (saveDialog.ShowDialog() == DialogResult.OK)
                            {
                                saveFilePath = saveDialog.FileName;

                                // 이미지 저장
                                mergedImage.Save(saveFilePath, ImageFormat.Png);
                                Print($"병합된 높이맵이 저장됨: {Path.GetFileName(saveFilePath)}");

                                // 결과 이미지 표시 여부 묻기
                                DialogResult viewResult = MessageBox.Show(
                                    "병합된 이미지를 확인하시겠습니까?",
                                    "병합 완료",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question);

                                if (viewResult == DialogResult.Yes)
                                {
                                    System.Diagnostics.Process.Start("explorer.exe", saveFilePath);
                                }
                            }
                            else
                            {
                                Print("저장이 취소되었습니다.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Print($"이미지 병합 중 오류 발생: {ex.Message}");
                    MessageBox.Show($"이미지 병합 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void terrainRegionBakeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // a, b 값을 입력받는 간단한 입력 폼 표시
            float a = 1.0f;
            float b = 0.0f;

            // 입력 다이얼로그 생성
            Form inputForm = new Form();
            inputForm.Text = "선형 변환 계수 입력";
            inputForm.Width = 300;
            inputForm.Height = 200;
            inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            inputForm.StartPosition = FormStartPosition.CenterParent;
            inputForm.MaximizeBox = false;
            inputForm.MinimizeBox = false;

            // a 값 입력용 컨트롤
            Label labelA = new Label();
            labelA.Text = "a 값 (기울기):";
            labelA.SetBounds(10, 20, 100, 20);

            TextBox textBoxA = new TextBox();
            textBoxA.Text = "1.0";
            textBoxA.SetBounds(110, 20, 160, 20);

            // b 값 입력용 컨트롤
            Label labelB = new Label();
            labelB.Text = "b 값 (오프셋):";
            labelB.SetBounds(10, 50, 100, 20);

            TextBox textBoxB = new TextBox();
            textBoxB.Text = "0.0";
            textBoxB.SetBounds(110, 50, 160, 20);

            // 설명 라벨
            Label labelInfo = new Label();
            labelInfo.Text = "각 픽셀 값에 y = ax + b 변환을 적용합니다.";
            labelInfo.SetBounds(10, 80, 270, 40);

            // 버튼
            Button buttonOK = new Button();
            buttonOK.Text = "확인";
            buttonOK.DialogResult = DialogResult.OK;
            buttonOK.SetBounds(110, 120, 75, 30);

            Button buttonCancel = new Button();
            buttonCancel.Text = "취소";
            buttonCancel.DialogResult = DialogResult.Cancel;
            buttonCancel.SetBounds(195, 120, 75, 30);

            // 폼에 컨트롤 추가
            inputForm.Controls.Add(labelA);
            inputForm.Controls.Add(textBoxA);
            inputForm.Controls.Add(labelB);
            inputForm.Controls.Add(textBoxB);
            inputForm.Controls.Add(labelInfo);
            inputForm.Controls.Add(buttonOK);
            inputForm.Controls.Add(buttonCancel);

            inputForm.AcceptButton = buttonOK;
            inputForm.CancelButton = buttonCancel;

            // 폼 표시 및 결과 확인
            if (inputForm.ShowDialog() == DialogResult.OK)
            {
                // 입력된 값 파싱
                if (!float.TryParse(textBoxA.Text, out a))
                {
                    MessageBox.Show("a 값이 올바르지 않습니다. 기본값 1.0을 사용합니다.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    a = 1.0f;
                }

                if (!float.TryParse(textBoxB.Text, out b))
                {
                    MessageBox.Show("b 값이 올바르지 않습니다. 기본값 0.0을 사용합니다.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    b = 0.0f;
                }

                string[] filenames = null;
                this.openFileDialog1.Multiselect = true;
                if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    filenames = this.openFileDialog1.FileNames;
                    foreach (string filename in filenames)
                    {
                        Print(Path.GetFileName(filename) + "작업목록에 추가함.");
                        this.textBox1.Text += Path.GetFileName(filename) + "작업목록에 추가함.\r\n";
                    }
                    foreach (string filename in filenames)
                    {
                        Print(Path.GetFileName(filename) + "작업을 시작합니다.");
                        string folder = Path.GetDirectoryName(filename);
                        string fn = Path.GetFileNameWithoutExtension(filename);
                        int aa = fn.IndexOf("region");
                        int bb = fn.IndexOf("x");
                        int m = int.Parse(fn.Substring(aa + 6, bb - aa - 6));
                        int n = int.Parse(fn.Substring(bb + 1));

                        // 원본 타일 분할 작업
                        SplitHeightmapAsync(filename, folder + $"/region_{m}x{n}_tiles/", 250, a, b);
                        // 추가: 125x125 크기의 저해상도 이미지 저장
                        SaveLowResolutionImage(filename, folder + $"/low/", 125, a, b);
                        // 추가: 32x32 크기의 저해상도 이미지 저장
                        SaveLowResolutionImage(filename, folder + $"/simple/", 32, a, b);
                    }
                    this.textBox2.Select(this.textBox2.Text.Length, 0);
                }
            }
        }

        private void 종료ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void 리전타일부드럽게하기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Multiselect = true;
            openFileDialog1.Filter = "PNG 파일|*.png|모든 파일|*.*";

            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                // 처리 진행 상황을 보여줄 ProgressBar가 있다고 가정
                // progressBar1.Maximum = this.openFileDialog1.FileNames.Length;
                // progressBar1.Value = 0;
                // progressBar1.Visible = true;

                int blurRadius = 3; // 기본값 설정 또는 사용자 입력 받기
                int noiseAmount = 10; // 기본값 설정 또는 사용자 입력 받기

                // 선택적으로 블러 세기와 노이즈 세기를 입력받는 코드
                using (var inputDialog = new Form())
                {
                    inputDialog.Width = 300;
                    inputDialog.Height = 200;
                    inputDialog.Text = "이미지 처리 설정";
                    inputDialog.StartPosition = FormStartPosition.CenterParent;
                    inputDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                    inputDialog.MaximizeBox = false;
                    inputDialog.MinimizeBox = false;

                    var radiusLabel = new Label() { Left = 20, Top = 20, Text = "블러 강도 (1-10):" };
                    var radiusInput = new NumericUpDown() { Left = 150, Top = 20, Width = 100, Minimum = 1, Maximum = 10, Value = blurRadius };

                    var noiseLabel = new Label() { Left = 20, Top = 50, Text = "노이즈 세기 (1-50):" };
                    var noiseInput = new NumericUpDown() { Left = 150, Top = 50, Width = 100, Minimum = 1, Maximum = 50, Value = noiseAmount };

                    var okButton = new Button() { Text = "확인", Left = 110, Width = 80, Top = 100, DialogResult = DialogResult.OK };
                    okButton.Click += (s, args) => { inputDialog.Close(); };

                    inputDialog.Controls.Add(radiusLabel);
                    inputDialog.Controls.Add(radiusInput);
                    inputDialog.Controls.Add(noiseLabel);
                    inputDialog.Controls.Add(noiseInput);
                    inputDialog.Controls.Add(okButton);
                    inputDialog.AcceptButton = okButton;

                    if (inputDialog.ShowDialog() == DialogResult.OK)
                    {
                        blurRadius = (int)radiusInput.Value;
                        noiseAmount = (int)noiseInput.Value;
                    }
                    else
                    {
                        // progressBar1.Visible = false;
                        return; // 사용자가 취소한 경우
                    }
                }

                // 백그라운드 작업으로 이미지 처리
                Task.Run(() =>
                {
                    int fileCount = 0;
                    foreach (string inputFilePath in this.openFileDialog1.FileNames)
                    {
                        try
                        {
                            string fnWithoutExtension = Path.GetFileNameWithoutExtension(inputFilePath);
                            string path = Path.GetDirectoryName(inputFilePath);
                            string outputFilePath = $"{path}\\{fnWithoutExtension}_smooth.png";
                            string comparisonFilePath = $"{path}\\{fnWithoutExtension}_comparison.png";

                            // 메인 UI 스레드에 진행 상황 업데이트
                            this.Invoke(new Action(() =>
                            {
                                // UI 업데이트 코드
                                // progressBar1.Value = fileCount + 1;
                                this.Text = $"처리 중... ({fileCount + 1}/{this.openFileDialog1.FileNames.Length}) - {fnWithoutExtension}";
                            }));

                            // 이미지 처리
                            //HeightmapProcessor.ProcessHeightmap(inputFilePath, outputFilePath, blurRadius, noiseAmount);

                            // 비교 이미지 생성 (선택적)
                            //HeightmapProcessor.CreateComparisonImage(inputFilePath, outputFilePath, comparisonFilePath);

                            fileCount++;
                        }
                        catch (Exception ex)
                        {
                            // 오류 발생 시 메시지 표시
                            this.Invoke(new Action(() =>
                            {
                                MessageBox.Show($"파일 처리 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }));
                        }
                    }

                    // 작업 완료 시 UI 업데이트
                    this.Invoke(new Action(() =>
                    {
                        // progressBar1.Visible = false;
                        this.Text = "이미지 처리 완료";
                        MessageBox.Show($"{fileCount}개 파일 처리 완료!", "처리 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }));
                });
            }
        }

        private static Random random = new Random();

        // 약간의 노이즈를 추가하여 등고선 문제 해결
        public static void SmoothContourWithNoise(string inputFilePath, string outputFilePath, float noiseAmount = 0.02f)
        {
            try
            {
                Console.WriteLine($"'{inputFilePath}' 파일을 읽는 중...");

                // 이미지 파일 로드
                using (Bitmap inputBitmap = new Bitmap(inputFilePath))
                {
                    int width = inputBitmap.Width;
                    int height = inputBitmap.Height;

                    Console.WriteLine($"이미지 크기: {width}x{height}");

                    // 가우시안 필터 적용 후 미세한 노이즈 추가
                    using (Bitmap outputBitmap = new Bitmap(width, height))
                    {
                        // 입력 비트맵 데이터를 메모리에 잠금
                        BitmapData inputData = inputBitmap.LockBits(
                            new Rectangle(0, 0, width, height),
                            ImageLockMode.ReadOnly,
                            PixelFormat.Format32bppArgb);

                        // 출력 비트맵 데이터를 메모리에 잠금
                        BitmapData outputData = outputBitmap.LockBits(
                            new Rectangle(0, 0, width, height),
                            ImageLockMode.WriteOnly,
                            PixelFormat.Format32bppArgb);

                        try
                        {
                            // 픽셀 데이터 추출
                            int bytesPerPixel = 4; // ARGB 형식은 픽셀당 4바이트
                            int stride = inputData.Stride;
                            byte[] inputPixels = new byte[stride * height];
                            byte[] outputPixels = new byte[stride * height];

                            Marshal.Copy(inputData.Scan0, inputPixels, 0, inputPixels.Length);

                            // 가우시안 블러 커널 생성 (7x7)
                            int filterSize = 7;
                            float sigma = 2.0f; // 시그마값 증가
                            float[,] kernel = CreateGaussianKernel(filterSize, sigma);

                            // 가우시안 필터 적용 + 노이즈 추가
                            ApplyGaussianAndNoise(inputPixels, outputPixels, width, height, stride, bytesPerPixel, kernel, filterSize, noiseAmount);

                            // 출력 비트맵에 픽셀 데이터 설정
                            Marshal.Copy(outputPixels, 0, outputData.Scan0, outputPixels.Length);
                        }
                        finally
                        {
                            // 비트맵 잠금 해제
                            inputBitmap.UnlockBits(inputData);
                            outputBitmap.UnlockBits(outputData);
                        }

                        // 결과 이미지 저장
                        outputBitmap.Save(outputFilePath, ImageFormat.Png);
                        Console.WriteLine($"등고선 문제가 해결된 하이트맵을 '{outputFilePath}'에 저장 완료");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류 발생: {ex.Message}");
            }
        }

        // 가우시안 커널 생성
        private static float[,] CreateGaussianKernel(int size, float sigma)
        {
            float[,] kernel = new float[size, size];
            float sum = 0.0f;

            int radius = size / 2;

            // 가우시안 함수 적용
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    float exponent = -(x * x + y * y) / (2.0f * sigma * sigma);
                    kernel[x + radius, y + radius] = (float)Math.Exp(exponent);
                    sum += kernel[x + radius, y + radius];
                }
            }

            // 정규화
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    kernel[x, y] /= sum;
                }
            }

            return kernel;
        }

        // 가우시안 필터와 노이즈 적용
        private static void ApplyGaussianAndNoise(byte[] inputPixels, byte[] outputPixels, int width, int height,
                                                  int stride, int bytesPerPixel, float[,] kernel, int filterSize, float noiseAmount)
        {
            int radius = filterSize / 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sumR = 0, sumG = 0, sumB = 0;

                    // 가우시안 필터 적용
                    for (int fy = -radius; fy <= radius; fy++)
                    {
                        for (int fx = -radius; fx <= radius; fx++)
                        {
                            int nx = x + fx;
                            int ny = y + fy;

                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                int offset = ny * stride + nx * bytesPerPixel;

                                float kernelValue = kernel[fx + radius, fy + radius];
                                sumB += inputPixels[offset] * kernelValue;
                                sumG += inputPixels[offset + 1] * kernelValue;
                                sumR += inputPixels[offset + 2] * kernelValue;
                            }
                        }
                    }

                    // 노이즈 추가
                    float noise = ((float)random.NextDouble() * 2 - 1) * noiseAmount * 255.0f;

                    // 최종 값 계산 및 범위 제한
                    int finalB = Math.Max(0, Math.Min(255, (int)(sumB + noise)));
                    int finalG = Math.Max(0, Math.Min(255, (int)(sumG + noise)));
                    int finalR = Math.Max(0, Math.Min(255, (int)(sumR + noise)));

                    // 출력 픽셀에 설정
                    int outOffset = y * stride + x * bytesPerPixel;
                    outputPixels[outOffset] = (byte)finalB;
                    outputPixels[outOffset + 1] = (byte)finalG;
                    outputPixels[outOffset + 2] = (byte)finalR;
                    outputPixels[outOffset + 3] = 255; // 알파 채널
                }
            }
        }

        // 여러 번 필터링 적용 (반복적 스무딩)
        public static void MultiPassSmoothing(string inputFilePath, string outputFilePath, int passes = 3)
        {
            try
            {
                string tempFile = "temp_heightmap.png";
                string currentInput = inputFilePath;

                Console.WriteLine($"다중 패스 스무딩 시작 ({passes}회)...");

                // 여러 번 필터링 적용
                for (int i = 0; i < passes; i++)
                {
                    string currentOutput = (i == passes - 1) ? outputFilePath : tempFile;
                    Console.WriteLine($"패스 {i + 1}/{passes} 처리 중...");

                    // 작은 가우시안 필터 적용 (더 미세한 제어를 위해)
                    using (Bitmap inputBitmap = new Bitmap(currentInput))
                    using (Bitmap outputBitmap = new Bitmap(inputBitmap.Width, inputBitmap.Height))
                    {
                        // 가우시안 필터 적용 (각 패스마다 작은 필터 사용)
                        ApplyGaussianFilter(inputBitmap, outputBitmap, 5, 1.5f);

                        // 결과 저장
                        outputBitmap.Save(currentOutput, ImageFormat.Png);
                    }

                    // 다음 패스를 위해 임시 파일을 입력으로 설정
                    if (i < passes - 1)
                    {
                        currentInput = tempFile;
                    }
                }

                // 임시 파일 삭제 시도
                try
                {
                    if (System.IO.File.Exists(tempFile))
                    {
                        System.IO.File.Delete(tempFile);
                    }
                }
                catch
                {
                    Console.WriteLine("임시 파일 삭제 실패 (파일이 사용 중일 수 있음)");
                }

                Console.WriteLine($"다중 패스 스무딩 완료. 결과 파일: '{outputFilePath}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"다중 패스 스무딩 오류: {ex.Message}");
            }
        }

        // 가우시안 필터 직접 적용
        private static void ApplyGaussianFilter(Bitmap input, Bitmap output, int filterSize, float sigma)
        {
            int width = input.Width;
            int height = input.Height;

            // 입출력 비트맵 잠금
            BitmapData inputData = input.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData outputData = output.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            try
            {
                int bytesPerPixel = 4;
                int stride = inputData.Stride;
                byte[] inputPixels = new byte[stride * height];
                byte[] outputPixels = new byte[stride * height];

                Marshal.Copy(inputData.Scan0, inputPixels, 0, inputPixels.Length);

                // 가우시안 커널 생성
                float[,] kernel = CreateGaussianKernel(filterSize, sigma);
                int radius = filterSize / 2;

                // 필터 적용
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float sumR = 0, sumG = 0, sumB = 0;

                        for (int fy = -radius; fy <= radius; fy++)
                        {
                            for (int fx = -radius; fx <= radius; fx++)
                            {
                                int nx = x + fx;
                                int ny = y + fy;

                                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                {
                                    int offset = ny * stride + nx * bytesPerPixel;

                                    float kernelValue = kernel[fx + radius, fy + radius];
                                    sumB += inputPixels[offset] * kernelValue;
                                    sumG += inputPixels[offset + 1] * kernelValue;
                                    sumR += inputPixels[offset + 2] * kernelValue;
                                }
                            }
                        }

                        // 출력 픽셀에 설정
                        int outOffset = y * stride + x * bytesPerPixel;
                        outputPixels[outOffset] = (byte)Math.Max(0, Math.Min(255, (int)sumB));
                        outputPixels[outOffset + 1] = (byte)Math.Max(0, Math.Min(255, (int)sumG));
                        outputPixels[outOffset + 2] = (byte)Math.Max(0, Math.Min(255, (int)sumR));
                        outputPixels[outOffset + 3] = 255; // 알파 채널
                    }
                }

                Marshal.Copy(outputPixels, 0, outputData.Scan0, outputPixels.Length);
            }
            finally
            {
                // 비트맵 잠금 해제
                input.UnlockBits(inputData);
                output.UnlockBits(outputData);
            }
        }
    
        private void 증폭하기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Multiselect = true;
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (string inputFilePath in this.openFileDialog1.FileNames)
                {
                    string fnWithoutExtension = Path.GetFileNameWithoutExtension(inputFilePath);
                    string path = Path.GetDirectoryName(inputFilePath);
                    string outputFilePath = $"{path}\\{fnWithoutExtension}_amplify.png";
                    AmplifyAndSaveHeightmap(inputFilePath, outputFilePath, 10);
                }
            }
        }

        public static void AmplifyAndSaveHeightmap(string inputFilePath, string outputFilePath, float amplificationFactor = 10.0f)
        {
            try
            {
                Console.WriteLine($"'{inputFilePath}' 파일을 읽는 중...");

                // 이미지 파일 로드
                using (Bitmap inputBitmap = new Bitmap(inputFilePath))
                {
                    int width = inputBitmap.Width;
                    int height = inputBitmap.Height;

                    Console.WriteLine($"이미지 크기: {width}x{height}");
                    Console.WriteLine($"높이 값을 {amplificationFactor}배로 증폭 중...");

                    // 출력 비트맵 생성
                    using (Bitmap outputBitmap = new Bitmap(width, height))
                    {
                        // 입력 비트맵 데이터를 메모리에 잠금
                        BitmapData inputData = inputBitmap.LockBits(
                            new Rectangle(0, 0, width, height),
                            ImageLockMode.ReadOnly,
                            PixelFormat.Format32bppArgb);

                        // 출력 비트맵 데이터를 메모리에 잠금
                        BitmapData outputData = outputBitmap.LockBits(
                            new Rectangle(0, 0, width, height),
                            ImageLockMode.WriteOnly,
                            PixelFormat.Format32bppArgb);

                        try
                        {
                            // 픽셀 데이터 추출
                            int bytesPerPixel = 4; // ARGB 형식은 픽셀당 4바이트
                            int stride = inputData.Stride;
                            byte[] inputPixels = new byte[stride * height];
                            byte[] outputPixels = new byte[stride * height];

                            Marshal.Copy(inputData.Scan0, inputPixels, 0, inputPixels.Length);

                            // 픽셀 값 증폭 및 제한
                            for (int y = 0; y < height; y++)
                            {
                                for (int x = 0; x < width; x++)
                                {
                                    int offset = y * stride + x * bytesPerPixel;

                                    // 픽셀의 RGB 채널 읽기 (그레이스케일이라면 모든 값이 동일)
                                    byte b = inputPixels[offset];
                                    byte g = inputPixels[offset + 1];
                                    byte r = inputPixels[offset + 2];
                                    byte a = inputPixels[offset + 3]; // 알파 채널

                                    // 그레이스케일 값 계산 (RGB 평균)
                                    byte grayValue = (byte)((r + g + b) / 3);

                                    // 값 증폭 및 255로 제한
                                    int amplifiedValue = (int)(grayValue * amplificationFactor);
                                    amplifiedValue = Math.Min(255, amplifiedValue); // 255를 초과하면 255로 제한

                                    // 증폭된 값을 모든 채널에 설정 (그레이스케일 유지)
                                    outputPixels[offset] = (byte)amplifiedValue;     // B
                                    outputPixels[offset + 1] = (byte)amplifiedValue; // G
                                    outputPixels[offset + 2] = (byte)amplifiedValue; // R
                                    outputPixels[offset + 3] = a;                   // A (알파 채널 유지)
                                }
                            }

                            // 출력 비트맵에 픽셀 데이터 설정
                            Marshal.Copy(outputPixels, 0, outputData.Scan0, outputPixels.Length);
                        }
                        finally
                        {
                            // 비트맵 잠금 해제
                            inputBitmap.UnlockBits(inputData);
                            outputBitmap.UnlockBits(outputData);
                        }

                        // 결과 이미지 저장
                        outputBitmap.Save(outputFilePath, ImageFormat.Png);
                        Console.WriteLine($"증폭된 하이트맵을 '{outputFilePath}'에 저장 완료");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류 발생: {ex.Message}");
            }
        }

        private void 리전타일침식적용ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Multiselect = true;
            openFileDialog1.Filter = "PNG 파일|*.png|모든 파일|*.*";

            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                // 침식 매개변수 설정을 위한 입력 대화상자
                float erosionStrength = 0.1f;
                float depositionStrength = 0.05f;
                int iterations = 10;

                using (var inputDialog = new Form())
                {
                    inputDialog.Text = "침식 매개변수 설정";
                    inputDialog.Size = new Size(350, 200);
                    inputDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                    inputDialog.StartPosition = FormStartPosition.CenterParent;
                    inputDialog.MaximizeBox = false;
                    inputDialog.MinimizeBox = false;

                    var lblIterations = new Label() { Left = 20, Top = 20, Text = "침식 반복 횟수 (1-50):" };
                    var nudIterations = new NumericUpDown() { Left = 200, Top = 18, Width = 100, Minimum = 1, Maximum = 50, Value = iterations };

                    var lblErosion = new Label() { Left = 20, Top = 50, Text = "침식 강도 (0.01-0.5):" };
                    var nudErosion = new NumericUpDown() { Left = 200, Top = 48, Width = 100, Minimum = 1, Maximum = 50, Value = (decimal)(erosionStrength * 100), DecimalPlaces = 0 };

                    var lblDeposition = new Label() { Left = 20, Top = 80, Text = "퇴적 강도 (0.01-0.5):" };
                    var nudDeposition = new NumericUpDown() { Left = 200, Top = 78, Width = 100, Minimum = 1, Maximum = 50, Value = (decimal)(depositionStrength * 100), DecimalPlaces = 0 };

                    var btnOk = new Button() { Text = "확인", Left = 130, Width = 80, Top = 120, DialogResult = DialogResult.OK };
                    btnOk.Click += (s, args) => { inputDialog.Close(); };

                    inputDialog.Controls.Add(lblIterations);
                    inputDialog.Controls.Add(nudIterations);
                    inputDialog.Controls.Add(lblErosion);
                    inputDialog.Controls.Add(nudErosion);
                    inputDialog.Controls.Add(lblDeposition);
                    inputDialog.Controls.Add(nudDeposition);
                    inputDialog.Controls.Add(btnOk);
                    inputDialog.AcceptButton = btnOk;

                    if (inputDialog.ShowDialog() == DialogResult.OK)
                    {
                        iterations = (int)nudIterations.Value;
                        erosionStrength = (float)nudErosion.Value / 100.0f;
                        depositionStrength = (float)nudDeposition.Value / 100.0f;
                    }
                    else
                    {
                        return; // 사용자가 취소함
                    }
                }

                // 백그라운드 작업으로 처리
                Task.Run(() =>
                {
                    int fileCount = 0;
                    int totalFiles = this.openFileDialog1.FileNames.Length;

                    foreach (string inputFilePath in this.openFileDialog1.FileNames)
                    {
                        try
                        {
                            string fnWithoutExtension = Path.GetFileNameWithoutExtension(inputFilePath);
                            string path = Path.GetDirectoryName(inputFilePath);
                            string outputFilePath = $"{path}\\{fnWithoutExtension}_eroded.png";
                            string comparisonFilePath = $"{path}\\{fnWithoutExtension}_erosion_comparison.png";

                            // UI 업데이트
                            this.Invoke(new Action(() =>
                            {
                                this.Text = $"파일 처리 중... ({fileCount + 1}/{totalFiles}) - {fnWithoutExtension}";
                            }));

                            // 침식 알고리즘 적용
                            //HeightmapErosion.ApplyThermalAndHydraulicErosion(inputFilePath, outputFilePath, iterations, erosionStrength, depositionStrength);

                            // 비교 이미지 생성
                            //HeightmapErosion.CreateComparisonImage(inputFilePath, outputFilePath, comparisonFilePath);

                            fileCount++;
                        }
                        catch (Exception ex)
                        {
                            // 오류 발생 시 메시지 표시
                            this.Invoke(new Action(() =>
                            {
                                MessageBox.Show($"파일 처리 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }));
                        }
                    }

                    // 작업 완료 시 UI 업데이트
                    this.Invoke(new Action(() =>
                    {
                        this.Text = "처리 완료";
                        MessageBox.Show($"{fileCount}개 파일 처리 완료!", "처리 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }));
                });
            }
        }
    }
}
