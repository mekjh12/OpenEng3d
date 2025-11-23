using Common.Abstractions;
using OpenGL;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Ui3d
{
    /// <summary>
    /// 3D 공간에 표시되는 채팅 말풍선
    /// 일정 시간 후 자동으로 사라집니다.
    /// </summary>
    public class ChatBubble : Billboard3D
    {
        // 기본 텍스처 크기
        private int TExTURE_WIDTH = 512;
        private int TExTURE_HEIGHT = 128;

        // 말풍선 설정
        private const float PADDING_HORIZONTAL = 20f;
        private const float PADDING_VERTICAL = 16f;
        private const float CORNER_RADIUS = 12f;
        private const float TAIL_WIDTH = 16f;
        private const float TAIL_HEIGHT = 12f;

        // 애니메이션 설정
        private const int DISPLAY_DURATION = 3000;          // 표시 시간 (밀리초)
        private const int FADE_OUT_DURATION = 500;          // 페이드아웃 시간 (밀리초)

        // 기본 색상
        private static readonly Color DEFAULT_TExT_COLOR = Color.FromArgb(255, 50, 50, 50);
        private static readonly Color DEFAULT_BUBBLE_COLOR = Color.FromArgb(255, 255, 255, 255);
        private static readonly Color DEFAULT_BORDER_COLOR = Color.FromArgb(255, 150, 150, 150);

        // 기본 크기
        private const float DEFAULT_WIDTH = 0.5f;
        private const float DEFAULT_HEIGHT = 0.15f;
        private const float DEFAULT_Z_OFFSET = 0.4f;

        // 기본 폰트
        private const string DEFAULT_FONT_FAMILY = "Consolas";

        private string _message;
        private Color _textColor;
        private Color _bubbleColor;
        private Color _borderColor;
        private string _fontFamily;
        private Font _messageFont;
        private int _elapsedTime;
        private int _displayDuration;
        private bool _isFinished;
        private bool _autoFit = true;
        private Bitmap _reusableBitmap;
        private Graphics _reusableGraphics;
        private static Bitmap _measureBitmap;
        private static Graphics _measureGraphics;

        /// <summary>표시할 메시지</summary>
        public string Message
        {
            get => _message;
            set
            {
                if (_message != value)
                {
                    _message = value;
                    _isDirty = true;
                }
            }
        }

        /// <summary>애니메이션이 완료되었는지 여부</summary>
        public bool IsFinished => _isFinished;

        /// <summary>표시 시간 (밀리초)</summary>
        public int DisplayDuration
        {
            get => _displayDuration;
            set => _displayDuration = Math.Max(0, value);
        }

        /// <summary>텍스트 색상</summary>
        public Color TextColor
        {
            get => _textColor;
            set
            {
                if (_textColor != value)
                {
                    _textColor = value;
                    _isDirty = true;
                }
            }
        }

        /// <summary>말풍선 배경 색상</summary>
        public Color BubbleColor
        {
            get => _bubbleColor;
            set
            {
                if (_bubbleColor != value)
                {
                    _bubbleColor = value;
                    _isDirty = true;
                }
            }
        }

        /// <summary>말풍선 테두리 색상</summary>
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                if (_borderColor != value)
                {
                    _borderColor = value;
                    _isDirty = true;
                }
            }
        }

        /// <summary>
        /// 채팅 말풍선을 생성합니다.
        /// </summary>
        /// <param name="camera">참조할 카메라</param>
        /// <param name="message">표시할 메시지</param>
        /// <param name="position">표시할 월드 위치</param>
        /// <param name="displayDuration">표시 시간 (밀리초, 0이면 무제한)</param>
        public ChatBubble(Camera camera, string message, Vertex3f position, int displayDuration = DISPLAY_DURATION)
            : base(camera)
        {
            _scaleWithDistance = true;

            _message = message;
            _worldPosition = position;
            _offset = new Vertex3f(0, 0, DEFAULT_Z_OFFSET);
            _displayDuration = displayDuration;
            _elapsedTime = 0;
            _isFinished = false;

            // 색상 설정
            _textColor = DEFAULT_TExT_COLOR;
            _bubbleColor = DEFAULT_BUBBLE_COLOR;
            _borderColor = DEFAULT_BORDER_COLOR;

            // 폰트 설정
            _fontFamily = DEFAULT_FONT_FAMILY;
            _messageFont = FontManager.GetFont(_fontFamily);

            // 크기 설정
            _width = DEFAULT_WIDTH;
            _height = DEFAULT_HEIGHT;

            // 재사용할 그래픽 리소스 초기화
            _reusableBitmap = new Bitmap(TExTURE_WIDTH, TExTURE_HEIGHT);
            _reusableGraphics = Graphics.FromImage(_reusableBitmap);
            _reusableGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            _reusableGraphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            // 초기 텍스처 생성
            UpdateTexture();
        }

        /// <summary>
        /// 업데이트 (애니메이션 처리)
        /// </summary>
        /// <param name="deltaTime">프레임 간 시간 (밀리초)</param>
        public override void Update(int deltaTime)
        {
            if (_isFinished)
            {
                _isVisible = false;
                return;
            }

            // 표시 시간이 0이면 무제한 표시
            if (_displayDuration > 0)
            {
                _elapsedTime += deltaTime;

                // 표시 시간이 지나면 완료 처리
                if (_elapsedTime >= _displayDuration)
                {
                    _isFinished = true;
                    _isVisible = false;
                    return;
                }

                // 페이드 아웃 계산
                int fadeStartTime = _displayDuration - FADE_OUT_DURATION;
                if (_elapsedTime >= fadeStartTime)
                {
                    int fadeTime = _elapsedTime - fadeStartTime;
                    _alpha = 1.0f - ((float)fadeTime / FADE_OUT_DURATION);
                }
                else
                {
                    _alpha = 1.0f;
                }
            }

            base.Update(deltaTime);
        }

        /// <summary>
        /// 말풍선을 즉시 갱신합니다.
        /// </summary>
        public void Refresh()
        {
            _isDirty = true;
        }

        /// <summary>
        /// 텍스처를 업데이트합니다.
        /// </summary>
        protected override void UpdateTexture()
        {
            // 정적 측정용 리소스 초기화 (최초 1회만)
            if (_measureBitmap == null)
            {
                _measureBitmap = new Bitmap(2048, 256);
                _measureGraphics = Graphics.FromImage(_measureBitmap);
                _measureGraphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            }

            // 텍스트 크기 측정
            SizeF textSize = SizeF.Empty;
            if (_messageFont != null && !string.IsNullOrEmpty(_message))
            {
                textSize = _measureGraphics.MeasureString(_message, _messageFont);
            }

            // AutoFit이 활성화된 경우 빌보드 크기 자동 조절
            if (_autoFit && !textSize.IsEmpty)
            {
                int newWidth = (int)(textSize.Width + PADDING_HORIZONTAL * 2);
                int newHeight = (int)(textSize.Height + PADDING_VERTICAL * 2 + TAIL_HEIGHT);

                // 텍스처 크기가 변경된 경우에만 재생성
                if (TExTURE_WIDTH != newWidth || TExTURE_HEIGHT != newHeight)
                {
                    TExTURE_WIDTH = newWidth;
                    TExTURE_HEIGHT = newHeight;

                    // 기존 리소스 해제 및 재생성
                    _reusableGraphics?.Dispose();
                    _reusableBitmap?.Dispose();
                    _reusableBitmap = new Bitmap(TExTURE_WIDTH, TExTURE_HEIGHT);
                    _reusableGraphics = Graphics.FromImage(_reusableBitmap);
                    _reusableGraphics.SmoothingMode = SmoothingMode.AntiAlias;
                    _reusableGraphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                }

                // 빌보드 크기 설정
                _width = 0.001f * TExTURE_WIDTH;
                _height = 0.001f * TExTURE_HEIGHT;
            }

            // 배경 투명하게
            _reusableGraphics.Clear(Color.Transparent);

            // 말풍선 영역 계산 (꼬리 제외)
            float bubblex = 0;
            float bubbleY = 0;
            float bubbleWidth = TExTURE_WIDTH;
            float bubbleHeight = TExTURE_HEIGHT - TAIL_HEIGHT;

            // 말풍선 경로 생성 (둥근 사각형 + 꼬리)
            GraphicsPath bubblePath = CreateBubblePath(bubblex, bubbleY, bubbleWidth, bubbleHeight);

            // 말풍선 배경 그리기
            using (SolidBrush bubbleBrush = new SolidBrush(_bubbleColor))
            {
                _reusableGraphics.FillPath(bubbleBrush, bubblePath);
            }

            // 말풍선 테두리 그리기
            using (Pen borderPen = new Pen(_borderColor, 2f))
            {
                _reusableGraphics.DrawPath(borderPen, bubblePath);
            }

            bubblePath.Dispose();

            // 텍스트 그리기
            if (_messageFont != null && !string.IsNullOrEmpty(_message))
            {
                using (SolidBrush textBrush = new SolidBrush(_textColor))
                using (StringFormat format = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                })
                {
                    RectangleF textRect = new RectangleF(
                        PADDING_HORIZONTAL / 2,
                        PADDING_VERTICAL / 2,
                        TExTURE_WIDTH - PADDING_HORIZONTAL,
                        bubbleHeight - PADDING_VERTICAL
                    );
                    _reusableGraphics.DrawString(_message, _messageFont, textBrush, textRect, format);
                }
            }

            // GPU에 업로드
            UploadTextureToGPU(_reusableBitmap);
        }

        /// <summary>
        /// 말풍선 경로 생성 (둥근 사각형 + 아래쪽 꼬리)
        /// </summary>
        private GraphicsPath CreateBubblePath(float x, float y, float width, float height)
        {
            GraphicsPath path = new GraphicsPath();

            // 둥근 사각형
            float diameter = CORNER_RADIUS * 2;
            RectangleF arc = new RectangleF(x, y, diameter, diameter);

            // 좌상단
            path.AddArc(arc, 180, 90);

            // 우상단
            arc.X = x + width - diameter;
            path.AddArc(arc, 270, 90);

            // 우하단
            arc.Y = y + height - diameter;
            path.AddArc(arc, 0, 90);

            // 꼬리 오른쪽
            float tailCenterx = x + width / 2;
            float tailBottomY = y + height + TAIL_HEIGHT;
            path.AddLine(x + width - CORNER_RADIUS, y + height, tailCenterx + TAIL_WIDTH / 2, y + height);

            // 꼬리 끝
            path.AddLine(tailCenterx + TAIL_WIDTH / 2, y + height, tailCenterx, tailBottomY);

            // 꼬리 왼쪽
            path.AddLine(tailCenterx, tailBottomY, tailCenterx - TAIL_WIDTH / 2, y + height);

            // 좌하단
            arc.X = x;
            path.AddLine(tailCenterx - TAIL_WIDTH / 2, y + height, x + CORNER_RADIUS, y + height);
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
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

        /// <summary>
        /// 정적 리소스를 정리합니다. (앱 종료 시 호출)
        /// </summary>
        public static void CleanupStaticResources()
        {
            _measureGraphics?.Dispose();
            _measureBitmap?.Dispose();
            _measureGraphics = null;
            _measureBitmap = null;
        }
    }
}