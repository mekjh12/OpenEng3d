using Common.Abstractions;
using OpenGL;
using Renderer;
using Shader;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using ZetaExt;

namespace FormTools
{
    public partial class FormWaterFlow : Form
    {
        readonly string PROJECT_PATH = @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\";

        private Texture _heightMapTexture;
        private int _width;
        private int _height;

        private IntPtr _glContext = IntPtr.Zero;
        private bool _glInitialized = false;

        private LabeledSlider _minStartHeight;
        private LabeledSlider _tracesPerPoint;

        private FlowAccumulationComputeShader _flowAccumulation;

        public FormWaterFlow()
        {
            InitializeComponent();
        }

        private void FormWaterFlow_Load(object sender, EventArgs e)
        {
            Image image = Image.FromFile(
                @"C:\Users\mekjh\OneDrive\바탕 화면\OpenEng3d\FormTools\bin\Debug\Res\Terrain\region0x0.png"
            );
            _width = image.Width;
            _height = image.Height;

            this.picOriginal.Image = image;

            FileHashManager.ROOT_FILE_PATH = PROJECT_PATH;
            InitializeOpenGL();

            AddControl(ref _minStartHeight, "시작점 최소높이(%)", 60, 0, 255, this.picOriginal);
            AddControl(ref _tracesPerPoint, "점당 추적횟수", 5, 1, 100, _minStartHeight);
        }

        private void AddControl(ref LabeledSlider control, string title, int value, int min, int max, Control adjControl)
        {
            control = new LabeledSlider();
            control.Title = title;
            control.Maximum = max;
            control.Minimum = min;
            control.Value = value;
            control.Location = new Point(adjControl.Left, adjControl.Bottom + 10);
            control.Size = new Size(adjControl.Width, 30);
            control.TrackBar.MouseUp += (o, e) =>
            {
                this.txtPrint.Clear();
                Process();
            };
            this.Controls.Add(control);
        }

        private void InitializeOpenGL()
        {
            try
            {
                _glContext = Wgl.GetCurrentContext();

                if (_glContext == IntPtr.Zero)
                {
                    ConsoleWrite("⚠️ OpenGL 컨텍스트가 없습니다. 메인 앱에서 초기화가 필요합니다.");
                    _glInitialized = false;
                }
                else
                {
                    ConsoleWrite("✅ OpenGL 컨텍스트 확인됨");
                    _glInitialized = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"OpenGL 초기화 실패: {ex.Message}", "오류");
                _glInitialized = false;
            }
        }

        private void 읽어오기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (this.picOriginal.Image != null)
                {
                    this.picOriginal.Image.Dispose();
                }

                Image bitmap = Image.FromFile(this.openFileDialog1.FileName);
                this.picOriginal.Image = bitmap;
                _width = bitmap.Width;
                _height = bitmap.Height;
                ConsoleWrite($"✅ 이미지 로딩 완료: {_width}x{_height}");
            }
        }

        public void ConsoleWrite(string text)
        {
            this.txtPrint.Text += text + "\r\n";
        }

        private void 결과저장하기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                SaveAsRgb24Png(this.picResult.Image, this.saveFileDialog1.FileName);
            }
        }

        private void SaveAsRgb24Png(Image sourceImage, string filePath)
        {
            using (Bitmap sourceBitmap = new Bitmap(sourceImage))
            {
                using (Bitmap rgb24Bitmap = new Bitmap(
                    sourceBitmap.Width,
                    sourceBitmap.Height,
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                {
                    using (Graphics g = Graphics.FromImage(rgb24Bitmap))
                    {
                        g.DrawImage(sourceBitmap, 0, 0, sourceBitmap.Width, sourceBitmap.Height);
                    }

                    rgb24Bitmap.Save(filePath, ImageFormat.Png);
                }
            }

            ConsoleWrite($"  포맷: RGB24 (24bpp)");
            ConsoleWrite($"  크기: {new Bitmap(filePath).Width} x {new Bitmap(filePath).Height}");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CleanupResources();
            base.OnFormClosing(e);
        }

        private void 물흐름처리하기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process();
        }

        private void Process()
        {
            this.txtPrint.Clear();
            CleanupResources();

            Bitmap bitmap = (Bitmap)this.picOriginal.Image;
            _heightMapTexture = new Texture(bitmap);

            ConsoleWrite("[FlowAccumulation] 계곡 경로 추적 시작...");

            uint accumulationMap = CreateAccumulationMap();

            if (_flowAccumulation == null)
            {
                _flowAccumulation = new FlowAccumulationComputeShader(PROJECT_PATH);
            }

            float minStartHeight = _minStartHeight.Value / 255.0f;
            _minStartHeight.SetText(minStartHeight.ToString("#.###"));
            int tracesPerPoint = _tracesPerPoint.Value;

            ConsoleWrite($"  시작점 최소 높이: {minStartHeight:F2}");
            ConsoleWrite($"  점당 추적 횟수: {tracesPerPoint}");

            _flowAccumulation.Bind();
            _flowAccumulation.LoadParams(
                width: _width,
                height: _height,
                maxIterations: 1000,
                minSlopeThreshold: 0.0001f,
                searchRadius: 5,
                minStartHeight: minStartHeight,
                tracesPerPoint: tracesPerPoint
            );

            Gl.BindImageTexture(0, _heightMapTexture.TextureID, 0, false, 0,
                BufferAccess.ReadOnly, InternalFormat.R32f);
            Gl.BindImageTexture(1, accumulationMap, 0, false, 0,
                BufferAccess.ReadWrite, InternalFormat.R32ui);

            _flowAccumulation.Dispatch(_width, _height);
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
            _flowAccumulation.Unbind();

            ConsoleWrite("[FlowAccumulation] 완료!");

            Texture accumulationTexture = new Texture(accumulationMap, _width, _height);
            Bitmap rawValleyMap = VisualizeFlowAccumulationHybrid(
                accumulationTexture,
                heightWeight: 2.0f,
                baseThreshold: 5.0f
            );

            Bitmap filteredMap = FilterBitmapByPercentile(rawValleyMap, topPercentile: 0.05f);
            Bitmap blend = BlendBitmaps((Bitmap)this.picOriginal.Image, filteredMap, BlendMode.Additive);

            if (this.picResult.Image != null)
                this.picResult.Image.Dispose();
            if (this.picCompose.Image != null)
                this.picCompose.Image.Dispose();

            this.picResult.Image = filteredMap;
            this.picCompose.Image = blend;

            rawValleyMap.Dispose();
            Gl.DeleteTextures(accumulationMap);

            ConsoleWrite("✅ 처리 완료!");
        }

        private uint CreateAccumulationMap()
        {
            uint buffer = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, buffer);

            Gl.TexImage2D(
                TextureTarget.Texture2d,
                0,
                InternalFormat.R32ui,
                _width, _height,
                0,
                OpenGL.PixelFormat.RedInteger,
                PixelType.UnsignedInt,
                IntPtr.Zero
            );

            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureMinFilter, Gl.NEAREST);
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureMagFilter, Gl.NEAREST);

            Gl.BindTexture(TextureTarget.Texture2d, 0);

            uint[] zeros = new uint[_width * _height];
            Gl.BindTexture(TextureTarget.Texture2d, buffer);
            Gl.TexSubImage2D(
                TextureTarget.Texture2d,
                0, 0, 0,
                _width, _height,
                OpenGL.PixelFormat.RedInteger,
                PixelType.UnsignedInt,
                zeros
            );
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            return buffer;
        }

        private Bitmap VisualizeFlowAccumulationHybrid(
            Texture accumulationTexture,
            float heightWeight = 2.0f,
            float baseThreshold = 5.0f)
        {
            ConsoleWrite("[Visualization] Flow Accumulation 시각화 중 (혼합 방법)...");

            uint[] data = new uint[_width * _height];
            Gl.BindTexture(TextureTarget.Texture2d, accumulationTexture.TextureID);
            Gl.GetTexImage(TextureTarget.Texture2d, 0, OpenGL.PixelFormat.RedInteger, PixelType.UnsignedInt, data);
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            float[] heightData = new float[_width * _height];
            Gl.BindTexture(TextureTarget.Texture2d, _heightMapTexture.TextureID);
            Gl.GetTexImage(TextureTarget.Texture2d, 0, OpenGL.PixelFormat.Red, PixelType.Float, heightData);
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            float[] weightedValues = new float[_width * _height];
            float maxWeighted = 0.0001f;

            for (int i = 0; i < data.Length; i++)
            {
                float height = heightData[i];
                uint count = data[i];

                if (float.IsNaN(height) || float.IsInfinity(height))
                    height = 0.5f;
                height = Math.Max(0.0f, Math.Min(1.0f, height));

                float threshold = baseThreshold * (1.0f - height * 0.8f);

                if (count < threshold)
                {
                    weightedValues[i] = 0.0f;
                    continue;
                }

                float heightFactor = 1.0f + (height * heightWeight);
                float weighted = (count - threshold) * heightFactor;

                weightedValues[i] = weighted;
                if (weighted > maxWeighted)
                    maxWeighted = weighted;
            }

            ConsoleWrite($"  최대 누적 카운트: {data.Max()}");
            ConsoleWrite($"  최대 가중치 값: {maxWeighted:F2}");

            Bitmap bitmap = new Bitmap(_width, _height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, _width, _height),
                ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;

                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        int dataIdx = y * _width + x;
                        int bmpIdx = ((_height - 1 - y) * _width + x) * 4;

                        float weighted = weightedValues[dataIdx];

                        float normalized = (float)Math.Log(weighted + 1.0) / (float)Math.Log(maxWeighted + 1.0);
                        normalized = Math.Max(0.0f, Math.Min(1.0f, normalized));

                        byte intensity = (byte)(normalized * 255);

                        ptr[bmpIdx + 0] = 0;
                        ptr[bmpIdx + 1] = 0;
                        ptr[bmpIdx + 2] = intensity;
                        ptr[bmpIdx + 3] = 255;
                    }
                }
            }

            bitmap.UnlockBits(bmpData);
            ConsoleWrite("[Visualization] 완료!");
            return bitmap;
        }

        public Bitmap FilterBitmapByPercentile(Bitmap sourceBitmap, float topPercentile = 0.05f)
        {
            ConsoleWrite($"[Filter] 백분위수 필터링 (상위 {topPercentile * 100:F1}%)...");

            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;

            float[] intensities = new float[width * height];

            BitmapData srcData = sourceBitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            unsafe
            {
                byte* srcPtr = (byte*)srcData.Scan0;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int idx = y * width + x;
                        int bmpIdx = (y * width + x) * 4;

                        byte r = srcPtr[bmpIdx + 2];
                        intensities[idx] = r / 255.0f;
                    }
                }
            }

            sourceBitmap.UnlockBits(srcData);

            float[] sortedIntensities = (float[])intensities.Clone();
            Array.Sort(sortedIntensities);

            int thresholdIndex = (int)(sortedIntensities.Length * (1.0f - topPercentile));
            float threshold = sortedIntensities[thresholdIndex];

            ConsoleWrite($"  임계값: {threshold:F4} (강도 0~1 범위)");

            Bitmap result = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            BitmapData dstData = result.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            int visiblePixels = 0;

            unsafe
            {
                byte* dstPtr = (byte*)dstData.Scan0;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int idx = y * width + x;
                        int bmpIdx = (y * width + x) * 4;

                        if (intensities[idx] < threshold)
                        {
                            dstPtr[bmpIdx + 0] = 0;
                            dstPtr[bmpIdx + 1] = 0;
                            dstPtr[bmpIdx + 2] = 0;
                            dstPtr[bmpIdx + 3] = 255;
                        }
                        else
                        {
                            visiblePixels++;

                            float normalized = (intensities[idx] - threshold) / (1.0f - threshold);
                            normalized = Math.Max(0.0f, Math.Min(1.0f, normalized));

                            byte intensity = (byte)(normalized * 255);

                            dstPtr[bmpIdx + 0] = 0;
                            dstPtr[bmpIdx + 1] = 0;
                            dstPtr[bmpIdx + 2] = intensity;
                            dstPtr[bmpIdx + 3] = 255;
                        }
                    }
                }
            }

            result.UnlockBits(dstData);

            float visiblePercent = (visiblePixels / (float)(width * height)) * 100f;
            ConsoleWrite($"  표시된 픽셀: {visiblePixels} ({visiblePercent:F2}%)");
            ConsoleWrite("[Filter] 완료!");

            return result;
        }

        private void CleanupResources()
        {
            if (_heightMapTexture != null && _heightMapTexture.TextureID != 0)
            {
                Gl.DeleteTextures(_heightMapTexture.TextureID);
                _heightMapTexture = null;
            }
        }

        public enum BlendMode
        {
            Alpha,
            Additive,
            Multiply,
            Screen,
            Overlay
        }

        public Bitmap BlendBitmaps(Bitmap bitmapA, Bitmap bitmapB, BlendMode blendMode = BlendMode.Alpha, float opacity = 0.5f)
        {
            if (bitmapA.Width != bitmapB.Width || bitmapA.Height != bitmapB.Height)
            {
                throw new ArgumentException("두 비트맵의 크기가 같아야 합니다!");
            }

            int width = bitmapA.Width;
            int height = bitmapA.Height;

            Bitmap result = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            BitmapData dataA = bitmapA.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            BitmapData dataB = bitmapB.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            BitmapData dataResult = result.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            unsafe
            {
                byte* ptrA = (byte*)dataA.Scan0;
                byte* ptrB = (byte*)dataB.Scan0;
                byte* ptrResult = (byte*)dataResult.Scan0;

                int totalPixels = width * height;

                for (int i = 0; i < totalPixels; i++)
                {
                    int idx = i * 4;

                    float bA = ptrA[idx + 0] / 255.0f;
                    float gA = ptrA[idx + 1] / 255.0f;
                    float rA = ptrA[idx + 2] / 255.0f;
                    float aA = ptrA[idx + 3] / 255.0f;

                    float bB = ptrB[idx + 0] / 255.0f;
                    float gB = ptrB[idx + 1] / 255.0f;
                    float rB = ptrB[idx + 2] / 255.0f;
                    float aB = ptrB[idx + 3] / 255.0f;

                    aB *= opacity;

                    float rOut, gOut, bOut, aOut;

                    switch (blendMode)
                    {
                        case BlendMode.Alpha:
                            rOut = rA * (1.0f - aB) + rB * aB;
                            gOut = gA * (1.0f - aB) + gB * aB;
                            bOut = bA * (1.0f - aB) + bB * aB;
                            aOut = aA + aB * (1.0f - aA);
                            break;

                        case BlendMode.Additive:
                            rOut = Math.Min(1.0f, rA + rB * aB);
                            gOut = Math.Min(1.0f, gA + gB * aB);
                            bOut = Math.Min(1.0f, bA + bB * aB);
                            aOut = Math.Max(aA, aB);
                            break;

                        case BlendMode.Multiply:
                            rOut = rA * (1.0f - aB) + (rA * rB) * aB;
                            gOut = gA * (1.0f - aB) + (gA * gB) * aB;
                            bOut = bA * (1.0f - aB) + (bA * bB) * aB;
                            aOut = aA;
                            break;

                        case BlendMode.Screen:
                            rOut = 1.0f - (1.0f - rA) * (1.0f - rB * aB);
                            gOut = 1.0f - (1.0f - gA) * (1.0f - gB * aB);
                            bOut = 1.0f - (1.0f - bA) * (1.0f - bB * aB);
                            aOut = aA;
                            break;

                        case BlendMode.Overlay:
                            rOut = BlendOverlay(rA, rB, aB);
                            gOut = BlendOverlay(gA, gB, aB);
                            bOut = BlendOverlay(bA, bB, aB);
                            aOut = aA;
                            break;

                        default:
                            rOut = rA;
                            gOut = gA;
                            bOut = bA;
                            aOut = aA;
                            break;
                    }

                    ptrResult[idx + 0] = (byte)(Math.Max(0, Math.Min(1, bOut)) * 255);
                    ptrResult[idx + 1] = (byte)(Math.Max(0, Math.Min(1, gOut)) * 255);
                    ptrResult[idx + 2] = (byte)(Math.Max(0, Math.Min(1, rOut)) * 255);
                    ptrResult[idx + 3] = (byte)(Math.Max(0, Math.Min(1, aOut)) * 255);
                }
            }

            bitmapA.UnlockBits(dataA);
            bitmapB.UnlockBits(dataB);
            result.UnlockBits(dataResult);

            return result;
        }

        private float BlendOverlay(float baseVal, float blendVal, float alpha)
        {
            float result;
            if (baseVal < 0.5f)
            {
                result = 2.0f * baseVal * blendVal;
            }
            else
            {
                result = 1.0f - 2.0f * (1.0f - baseVal) * (1.0f - blendVal);
            }

            return baseVal * (1.0f - alpha) + result * alpha;
        }
    }
}