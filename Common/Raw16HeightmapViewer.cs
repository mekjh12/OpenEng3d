using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;


namespace Common
{
    /// <summary>
    /// RAW 16bit 하이트맵 뷰어 (1024x1024 고정, PictureBox용)
    /// </summary>
    public class Raw16HeightmapViewer
    {
        private const int SIZE = 1025;

        private ushort[] _heightData;
        private float[] _normalizedData; // 0~1 정규화

        private bool _isLoaded = false;
        private string _currentFile = "";

        // 통계
        private ushort _minHeightRaw;
        private ushort _maxHeightRaw;
        private float _minHeight;
        private float _maxHeight;

        // 뷰어 설정
        private ColorMode _colorMode = ColorMode.Grayscale;
        private bool _autoContrast = false;

        public enum ColorMode
        {
            Grayscale,
            Terrain,      // 녹색-갈색-흰색
            Rainbow,      // 무지개 스펙트럼
            Heatmap       // 파란색-빨간색
        }

        public bool AutoContrast { get => _autoContrast; set => _autoContrast = value; }
        public bool IsLoaded => _isLoaded;
        public string CurrentFile => _currentFile;
        public ColorMode CurrentColorMode => _colorMode;
        public float MinHeight => _minHeight;
        public float MaxHeight => _maxHeight;

        public Raw16HeightmapViewer()
        {
        }

        /// <summary>
        /// RAW 16bit 파일 로드
        /// </summary>
        public bool LoadRaw16File(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[Viewer] 파일 없음: {filePath}");
                return false;
            }

            try
            {
                //[Viewer] 파일 크기 오류: 33282 bytes (expected 2097152)
                //[Viewer] 파일 크기 오류: 2101250 bytes(expected 2097152)

                // 파일 읽기
                byte[] rawBytes = File.ReadAllBytes(filePath);

                // 저해상도 판별
                _size = (rawBytes.Length == 129*129*2) ? 129 : SIZE;    // 저해상도 크기

                int expectedSize = _size * _size * 2; // 16bit = 2 bytes

                if (rawBytes.Length != expectedSize)
                {
                    Console.WriteLine($"[Viewer] 파일 크기 오류: {rawBytes.Length} bytes (expected {expectedSize})");
                    return false;
                }

                // ushort 배열로 변환
                _heightData = new ushort[_size * _size];
                Buffer.BlockCopy(rawBytes, 0, _heightData, 0, rawBytes.Length);

                // float 배열로 정규화
                _normalizedData = new float[_size * _size];
                for (int i = 0; i < _heightData.Length; i++)
                {
                    _normalizedData[i] = _heightData[i] / 65535.0f;
                }

                // 통계 계산
                CalculateHeightStats();

                _currentFile = filePath;
                _isLoaded = true;

                Console.WriteLine($"[Viewer] 로드 완료: {Path.GetFileName(filePath)}");
                Console.WriteLine($"[Viewer] 높이 범위: {_minHeightRaw} ~ {_maxHeightRaw} (0x{_minHeightRaw:X4} ~ 0x{_maxHeightRaw:X4})");

                return true;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Viewer] 로드 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 통계 계산
        /// </summary>
        private void CalculateHeightStats()
        {
            _minHeightRaw = ushort.MaxValue;
            _maxHeightRaw = ushort.MinValue;

            foreach (ushort h in _heightData)
            {
                if (h < _minHeightRaw) _minHeightRaw = h;
                if (h > _maxHeightRaw) _maxHeightRaw = h;
            }

            _minHeight = _minHeightRaw / 65535.0f;
            _maxHeight = _maxHeightRaw / 65535.0f;
        }

        private int _size = 0;

        /// <summary>
        /// Bitmap 생성
        /// </summary>
        public Bitmap CreateBitmap()
        {
            if (!_isLoaded) return null;

            Bitmap bitmap = new Bitmap(_size, _size, PixelFormat.Format24bppRgb);

            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, _size, _size),
                ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int stride = bmpData.Stride;

                for (int y = 0; y < _size; y++)
                {
                    for (int x = 0; x < _size; x++)
                    {
                        int index = y * _size + x;
                        float height = _normalizedData[index];

                        // Auto Contrast 적용
                        if (_autoContrast && (_maxHeight - _minHeight) > 0.001f)
                        {
                            height = (height - _minHeight) / (_maxHeight - _minHeight);
                        }

                        // 색상 계산
                        Color color = GetColorForHeight(height);

                        // BGR 순서로 저장 (Bitmap은 BGR)
                        int offset = y * stride + x * 3;
                        ptr[offset + 0] = color.B;
                        ptr[offset + 1] = color.G;
                        ptr[offset + 2] = color.R;
                    }
                }
            }

            bitmap.UnlockBits(bmpData);
            return bitmap;
        }

        /// <summary>
        /// 특정 크기로 리사이즈된 Bitmap 생성
        /// </summary>
        public Bitmap CreateResizedBitmap(int width, int height)
        {
            Bitmap original = CreateBitmap();
            if (original == null) return null;

            Bitmap resized = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(original, 0, 0, width, height);
            }

            original.Dispose();
            return resized;
        }

        /// <summary>
        /// 높이값에 따른 색상 계산
        /// </summary>
        private Color GetColorForHeight(float height)
        {
            height = Math.Max(0, Math.Min(1, height));

            switch (_colorMode)
            {
                case ColorMode.Grayscale:
                    byte gray = (byte)(height * 255);
                    return Color.FromArgb(gray, gray, gray);

                case ColorMode.Terrain:
                    return GetTerrainColor(height);

                case ColorMode.Rainbow:
                    return GetRainbowColor(height);

                case ColorMode.Heatmap:
                    return GetHeatmapColor(height);

                default:
                    return Color.White;
            }
        }

        /// <summary>
        /// Terrain 컬러맵 (물-해변-평지-산-설산)
        /// </summary>
        private Color GetTerrainColor(float h)
        {
            if (h < 0.3f)      // 물
            {
                float t = h / 0.3f;
                return LerpColor(Color.FromArgb(0, 50, 100), Color.FromArgb(0, 100, 150), t);
            }
            else if (h < 0.35f) // 해변
            {
                float t = (h - 0.3f) / 0.05f;
                return LerpColor(Color.FromArgb(194, 178, 128), Color.FromArgb(240, 220, 130), t);
            }
            else if (h < 0.6f)  // 평지
            {
                float t = (h - 0.35f) / 0.25f;
                return LerpColor(Color.FromArgb(50, 150, 50), Color.FromArgb(100, 200, 100), t);
            }
            else if (h < 0.8f)  // 산
            {
                float t = (h - 0.6f) / 0.2f;
                return LerpColor(Color.FromArgb(139, 90, 43), Color.FromArgb(180, 140, 90), t);
            }
            else                // 설산
            {
                float t = (h - 0.8f) / 0.2f;
                return LerpColor(Color.FromArgb(200, 200, 200), Color.FromArgb(255, 255, 255), t);
            }
        }

        /// <summary>
        /// Rainbow 컬러맵
        /// </summary>
        private Color GetRainbowColor(float h)
        {
            h = h * 6.0f; // 0~6
            int sector = (int)h;
            float t = h - sector;

            switch (sector)
            {
                case 0: return LerpColor(Color.Purple, Color.Blue, t);
                case 1: return LerpColor(Color.Blue, Color.Cyan, t);
                case 2: return LerpColor(Color.Cyan, Color.Green, t);
                case 3: return LerpColor(Color.Green, Color.Yellow, t);
                case 4: return LerpColor(Color.Yellow, Color.Red, t);
                default: return Color.Red;
            }
        }

        /// <summary>
        /// Heatmap 컬러맵 (파란색-빨간색)
        /// </summary>
        private Color GetHeatmapColor(float h)
        {
            if (h < 0.25f)
            {
                float t = h / 0.25f;
                return LerpColor(Color.Blue, Color.Cyan, t);
            }
            else if (h < 0.5f)
            {
                float t = (h - 0.25f) / 0.25f;
                return LerpColor(Color.Cyan, Color.Green, t);
            }
            else if (h < 0.75f)
            {
                float t = (h - 0.5f) / 0.25f;
                return LerpColor(Color.Green, Color.Yellow, t);
            }
            else
            {
                float t = (h - 0.75f) / 0.25f;
                return LerpColor(Color.Yellow, Color.Red, t);
            }
        }

        /// <summary>
        /// 색상 보간
        /// </summary>
        private Color LerpColor(Color a, Color b, float t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t)
            );
        }

        /// <summary>
        /// 특정 픽셀의 높이값 가져오기
        /// </summary>
        public float? GetHeightAt(int x, int y)
        {
            if (!_isLoaded) return null;
            if (x < 0 || x >= _size || y < 0 || y >= _size) return null;

            return _normalizedData[y * _size + x];
        }

        /// <summary>
        /// 특정 픽셀의 RAW 높이값 가져오기
        /// </summary>
        public ushort? GetRawHeightAt(int x, int y)
        {
            if (!_isLoaded) return null;
            if (x < 0 || x >= _size || y < 0 || y >= _size) return null;

            return _heightData[y * _size + x];
        }

        /// <summary>
        /// 컬러 모드 변경
        /// </summary>
        public void SetColorMode(ColorMode mode)
        {
            _colorMode = mode;
            Console.WriteLine($"[Viewer] Color Mode: {mode}");
        }

        /// <summary>
        /// Auto Contrast 토글
        /// </summary>
        public void ToggleAutoContrast()
        {
            _autoContrast = !_autoContrast;
            Console.WriteLine($"[Viewer] Auto Contrast: {_autoContrast}");
        }

        /// <summary>
        /// 정보 출력
        /// </summary>
        public void PrintInfo()
        {
            if (!_isLoaded)
            {
                Console.WriteLine("[Viewer] 로드된 파일 없음");
                return;
            }

            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine($"파일: {Path.GetFileName(_currentFile)}");
            Console.WriteLine($"크기: {SIZE}x{SIZE}");
            Console.WriteLine($"RAW 범위: {_minHeightRaw} ~ {_maxHeightRaw}");
            Console.WriteLine($"      (0x{_minHeightRaw:X4} ~ 0x{_maxHeightRaw:X4})");
            Console.WriteLine($"정규화: {_minHeight:F6} ~ {_maxHeight:F6}");
            Console.WriteLine($"컬러 모드: {_colorMode}");
            Console.WriteLine($"Auto Contrast: {_autoContrast}");
            Console.WriteLine("═══════════════════════════════════════");
        }
    }
}