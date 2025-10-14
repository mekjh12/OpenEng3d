using Common.Abstractions;
using OpenGL;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Ui3d
{
    /// <summary>
    /// 3D 공간에 표시되는 타격 마커
    /// 공격이 명중했을 때 시각적 피드백을 제공합니다.
    /// </summary>
    public class HitMarker : Billboard3D
    {
        // 기본 텍스처 크기
        private const int TEXTURE_WIDTH = 128;
        private const int TEXTURE_HEIGHT = 128;

        // 기본 색상
        private static readonly Color DEFAULT_NORMAL_COLOR = Color.FromArgb(255, 255, 255, 255);
        private static readonly Color DEFAULT_CRITICAL_COLOR = Color.FromArgb(255, 255, 50, 50);
        private static readonly Color DEFAULT_BACKGROUND_COLOR = Color.FromArgb(0, 0, 0, 0);

        // 기본 빌보드 크기
        private const float DEFAULT_WIDTH = 0.2f;
        private const float DEFAULT_HEIGHT = 0.2f;

        // 애니메이션 설정
        private const int LIFETIME_MS = 500;
        private const int FADE_START_MS = 200;
        private const float EXPAND_SCALE = 1.5f;
        private const float LINE_WIDTH = 4f;
        private const float LINE_LENGTH = 20f;
        private const float LINE_GAP = 8f;

        private Color _markerColor;
        private bool _isCritical;
        private Bitmap _reusableBitmap;
        private Graphics _reusableGraphics;
        private int _elapsedTime;
        private bool _isComplete;
        private float _currentScale;

        /// <summary>마커 색상</summary>
        public Color MarkerColor
        {
            get => _markerColor;
            set
            {
                if (_markerColor != value)
                {
                    _markerColor = value;
                    _isDirty = true;
                }
            }
        }

        /// <summary>크리티컬 히트 여부</summary>
        public bool IsCritical
        {
            get => _isCritical;
            set
            {
                if (_isCritical != value)
                {
                    _isCritical = value;
                    _markerColor = value ? DEFAULT_CRITICAL_COLOR : DEFAULT_NORMAL_COLOR;
                    _isDirty = true;
                }
            }
        }

        /// <summary>애니메이션이 완료되었는지 여부</summary>
        public bool IsComplete => _isComplete;

        /// <summary>
        /// 타격 마커를 생성합니다.
        /// </summary>
        /// <param name="camera">참조할 카메라</param>
        /// <param name="isCritical">크리티컬 히트 여부</param>
        public HitMarker(Camera camera, bool isCritical = false)
            : base(camera)
        {
            _isCritical = isCritical;
            _markerColor = isCritical ? DEFAULT_CRITICAL_COLOR : DEFAULT_NORMAL_COLOR;
            _width = DEFAULT_WIDTH;
            _height = DEFAULT_HEIGHT;
            _elapsedTime = 0;
            _isComplete = false;
            _currentScale = 1.0f;

            // 재사용할 그래픽 리소스 초기화
            _reusableBitmap = new Bitmap(TEXTURE_WIDTH, TEXTURE_HEIGHT);
            _reusableGraphics = Graphics.FromImage(_reusableBitmap);
            _reusableGraphics.SmoothingMode = SmoothingMode.AntiAlias;
        }

        /// <summary>
        /// 업데이트 (애니메이션 처리)
        /// </summary>
        /// <param name="deltaTime">프레임 간 시간 (밀리초)</param>
        public override void Update(int deltaTime)
        {
            if (_isComplete)
            {
                _isActive = false;
                return;
            }

            _elapsedTime += deltaTime;

            // 수명 체크
            if (_elapsedTime >= LIFETIME_MS)
            {
                _isComplete = true;
                _isActive = false;
                return;
            }

            // 확장 애니메이션
            float progress = (float)_elapsedTime / LIFETIME_MS;
            _currentScale = 1.0f + (EXPAND_SCALE - 1.0f) * progress;
            _isDirty = true;

            // 페이드 아웃 처리
            if (_elapsedTime >= FADE_START_MS)
            {
                float fadeProgress = (float)(_elapsedTime - FADE_START_MS) / (LIFETIME_MS - FADE_START_MS);
                _alpha = 1.0f - fadeProgress;
            }

            base.Update(deltaTime);
        }

        /// <summary>
        /// 텍스처를 업데이트합니다.
        /// </summary>
        protected override void UpdateTexture()
        {
            // 배경 클리어 (투명)
            _reusableGraphics.Clear(DEFAULT_BACKGROUND_COLOR);

            // 중심점
            float centerX = TEXTURE_WIDTH / 2f;
            float centerY = TEXTURE_HEIGHT / 2f;

            // 스케일 적용된 길이와 간격
            float scaledLength = LINE_LENGTH * _currentScale;
            float scaledGap = LINE_GAP * _currentScale;

            using (Pen markerPen = new Pen(_markerColor, LINE_WIDTH))
            {
                markerPen.StartCap = LineCap.Round;
                markerPen.EndCap = LineCap.Round;

                // 상단 라인
                _reusableGraphics.DrawLine(markerPen,
                    centerX, centerY - scaledGap,
                    centerX, centerY - scaledGap - scaledLength);

                // 하단 라인
                _reusableGraphics.DrawLine(markerPen,
                    centerX, centerY + scaledGap,
                    centerX, centerY + scaledGap + scaledLength);

                // 좌측 라인
                _reusableGraphics.DrawLine(markerPen,
                    centerX - scaledGap, centerY,
                    centerX - scaledGap - scaledLength, centerY);

                // 우측 라인
                _reusableGraphics.DrawLine(markerPen,
                    centerX + scaledGap, centerY,
                    centerX + scaledGap + scaledLength, centerY);
            }

            // GPU에 업로드
            UploadTextureToGPU(_reusableBitmap);
        }

        /// <summary>
        /// 애니메이션을 리셋합니다.
        /// </summary>
        public void Reset()
        {
            _elapsedTime = 0;
            _isComplete = false;
            _isActive = true;
            _alpha = 1.0f;
            _currentScale = 1.0f;
            _isDirty = true;
        }

        /// <summary>
        /// 리소스를 정리합니다.
        /// </summary>
        public override void Dispose()
        {
            _reusableGraphics?.Dispose();
            _reusableBitmap?.Dispose();
            base.Dispose();
        }
    }
}