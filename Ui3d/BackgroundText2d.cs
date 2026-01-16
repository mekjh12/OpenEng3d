using Common.Abstractions;
using OpenGL;
using System;
using System.Drawing;

namespace Ui3d
{
    /// <summary>
    /// 배경이 있는 2D 텍스트 렌더링 클래스
    /// Text2d를 상속받아 텍스트 뒤에 배경 사각형을 렌더링합니다.
    /// </summary>
    public class BackgroundText2d : Text2d
    {
        // 배경 렌더링을 위한 VAO, VBO
        private uint _backgroundVAO;
        private uint _backgroundVBO;

        // 배경 속성
        private Color _backgroundColor;
        private float _backgroundAlpha;
        private bool _showBackground;

        // 패딩 (픽셀 단위)
        private float _paddingLeft;
        private float _paddingRight;
        private float _paddingTop;
        private float _paddingBottom;

        // 배경 크기 캐시
        private float _backgroundWidth;
        private float _backgroundHeight;
        private bool _backgroundDirty;

        // 화면 크기 저장 (projection matrix 계산용)
        private int _screenWidth;
        private int _screenHeight;
        private Matrix4x4f _projectionMatrix;

        // 텍스트 높이 저장 (배경 크기 계산용)
        private float _heightInPixels;

        // 프레임 카운터 기능
        private int _frameCounter;
        private int _maxFrames;
        private bool _useFrameCounter;

        // 기본 배경색
        private static readonly Color DEFAULT_BACKGROUND_COLOR = Color.FromArgb(180, 200, 200, 200); // 반투명 검정

        /// <summary>
        /// 배경 색상
        /// </summary>
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set => _backgroundColor = value;
        }

        /// <summary>
        /// 배경 투명도 (0.0 ~ 1.0)
        /// </summary>
        public float BackgroundAlpha
        {
            get => _backgroundAlpha;
            set => _backgroundAlpha = Math.Max(0, Math.Min(1, value));
        }

        /// <summary>
        /// 배경 표시 여부
        /// </summary>
        public bool ShowBackground
        {
            get => _showBackground;
            set => _showBackground = value;
        }

        /// <summary>
        /// 왼쪽 패딩 (픽셀)
        /// </summary>
        public float PaddingLeft
        {
            get => _paddingLeft;
            set
            {
                if (_paddingLeft != value)
                {
                    _paddingLeft = value;
                    _backgroundDirty = true;
                }
            }
        }

        /// <summary>
        /// 오른쪽 패딩 (픽셀)
        /// </summary>
        public float PaddingRight
        {
            get => _paddingRight;
            set
            {
                if (_paddingRight != value)
                {
                    _paddingRight = value;
                    _backgroundDirty = true;
                }
            }
        }

        /// <summary>
        /// 위쪽 패딩 (픽셀)
        /// </summary>
        public float PaddingTop
        {
            get => _paddingTop;
            set
            {
                if (_paddingTop != value)
                {
                    _paddingTop = value;
                    _backgroundDirty = true;
                }
            }
        }

        /// <summary>
        /// 아래쪽 패딩 (픽셀)
        /// </summary>
        public float PaddingBottom
        {
            get => _paddingBottom;
            set
            {
                if (_paddingBottom != value)
                {
                    _paddingBottom = value;
                    _backgroundDirty = true;
                }
            }
        }

        /// <summary>
        /// 생성자
        /// </summary>
        public BackgroundText2d(
            string text,
            float x,
            float y,
            int screenWidth,
            int screenHeight,
            TextAlignment alignment = TextAlignment.Left | TextAlignment.Top,
            float heightInPixels = 24.0f,
            float padding = 8.0f)
            : base(text, x, y, screenWidth, screenHeight, alignment, heightInPixels)
        {
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            _heightInPixels = heightInPixels;

            _backgroundColor = DEFAULT_BACKGROUND_COLOR;
            _backgroundAlpha = 0.8f;
            _showBackground = true;
            _backgroundDirty = true;

            // 프레임 카운터 초기화
            _frameCounter = 0;
            _maxFrames = 0;
            _useFrameCounter = false;

            // 균일한 패딩 적용
            _paddingLeft = padding;
            _paddingRight = padding;
            _paddingTop = padding;
            _paddingBottom = padding;

            CreateOrthographicProjection();
            InitializeBackground();
        }

        /// <summary>
        /// 생성자 (개별 패딩)
        /// </summary>
        public BackgroundText2d(
            string text,
            float x,
            float y,
            int screenWidth,
            int screenHeight,
            TextAlignment alignment,
            float heightInPixels,
            float paddingLeft,
            float paddingRight,
            float paddingTop,
            float paddingBottom)
            : base(text, x, y, screenWidth, screenHeight, alignment, heightInPixels)
        {
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            _heightInPixels = heightInPixels;

            _backgroundColor = DEFAULT_BACKGROUND_COLOR;
            _backgroundAlpha = 0.8f;
            _showBackground = true;
            _backgroundDirty = true;

            // 프레임 카운터 초기화
            _frameCounter = 0;
            _maxFrames = 0;
            _useFrameCounter = false;

            _paddingLeft = paddingLeft;
            _paddingRight = paddingRight;
            _paddingTop = paddingTop;
            _paddingBottom = paddingBottom;

            CreateOrthographicProjection();
            InitializeBackground();
        }

        /// <summary>
        /// 화면 크기 변경 시 호출
        /// </summary>
        public new void OnScreenResize(int width, int height)
        {
            _screenWidth = width;
            _screenHeight = height;
            CreateOrthographicProjection();
            base.OnScreenResize(width, height);
        }

        /// <summary>
        /// 정투영 행렬 생성
        /// </summary>
        private void CreateOrthographicProjection()
        {
            float left = 0;
            float right = _screenWidth;
            float top = 0;
            float bottom = _screenHeight;
            float near = -1.0f;
            float far = 1.0f;

            _projectionMatrix = Matrix4x4f.Identity;
            _projectionMatrix[0, 0] = 2.0f / (right - left);
            _projectionMatrix[1, 1] = 2.0f / (top - bottom);
            _projectionMatrix[2, 2] = -2.0f / (far - near);
            _projectionMatrix[3, 0] = -(right + left) / (right - left);
            _projectionMatrix[3, 1] = -(top + bottom) / (top - bottom);
            _projectionMatrix[3, 2] = -(far + near) / (far - near);
        }

        /// <summary>
        /// 모든 패딩을 한번에 설정
        /// </summary>
        public void SetPadding(float padding)
        {
            _paddingLeft = padding;
            _paddingRight = padding;
            _paddingTop = padding;
            _paddingBottom = padding;
            _backgroundDirty = true;
        }

        /// <summary>
        /// 프레임 카운터 활성화 및 설정
        /// 지정된 프레임 수만큼 렌더링 후 자동으로 숨김 처리
        /// </summary>
        /// <param name="maxFrames">최대 렌더링 프레임 수 (예: 120)</param>
        public void EnableFrameCounter(int maxFrames)
        {
            _useFrameCounter = true;
            _maxFrames = maxFrames;
            _frameCounter = 0;
            IsVisible = true; // 카운터 시작 시 다시 표시
        }

        /// <summary>
        /// 프레임 카운터 비활성화
        /// </summary>
        public void DisableFrameCounter()
        {
            _useFrameCounter = false;
            _frameCounter = 0;
        }

        /// <summary>
        /// 프레임 카운터 리셋 (다시 표시)
        /// </summary>
        public void ResetFrameCounter()
        {
            _frameCounter = 0;
            IsVisible = true;
        }

        /// <summary>
        /// 현재 프레임 카운터 값 가져오기
        /// </summary>
        public int CurrentFrameCount => _frameCounter;

        /// <summary>
        /// 최대 프레임 수 가져오기
        /// </summary>
        public int MaxFrameCount => _maxFrames;

        /// <summary>
        /// 프레임 카운터 사용 여부
        /// </summary>
        public bool IsFrameCounterEnabled => _useFrameCounter;


        /// <summary>
        /// 배경 초기화
        /// </summary>
        private void InitializeBackground()
        {
            _backgroundVAO = Gl.GenVertexArray();
            _backgroundVBO = Gl.GenBuffer();

            Gl.BindVertexArray(_backgroundVAO);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _backgroundVBO);

            // 위치만 사용 (2D 좌표)
            Gl.EnableVertexAttribArray(0);
            Gl.VertexAttribPointer(0, 2, VertexAttribType.Float, false,
                2 * sizeof(float), IntPtr.Zero);

            Gl.BindVertexArray(0);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        /// <summary>
        /// 텍스트 크기를 기반으로 배경 크기 계산
        /// </summary>
        private void UpdateBackgroundSize()
        {
            if (!CharacterTextureAtlas.IsInitialized || string.IsNullOrEmpty(Text))
            {
                _backgroundWidth = 0;
                _backgroundHeight = 0;
                return;
            }

            // 텍스트 실제 크기 계산
            string[] lines = Text.Split('\n');
            float maxLineWidth = 0;

            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;
                float lineWidth = CharacterTextureAtlas.Instance.CalculateTextWidth(line) * Scale;
                maxLineWidth = Math.Max(maxLineWidth, lineWidth);
            }

            // 문자 높이는 생성 시 지정한 heightInPixels 사용
            float actualCharHeight = _heightInPixels;
            float lineSpacing = actualCharHeight * 1.2f;
            float totalHeight = actualCharHeight + (lines.Length - 1) * lineSpacing;

            // 패딩 추가
            _backgroundWidth = maxLineWidth + _paddingLeft + _paddingRight;
            _backgroundHeight = totalHeight + _paddingTop + _paddingBottom;

            _backgroundDirty = false;
        }

        /// <summary>
        /// 배경 버퍼 업데이트
        /// </summary>
        private void UpdateBackgroundBuffer()
        {
            if (_backgroundDirty)
            {
                UpdateBackgroundSize();
            }

            if (_backgroundWidth <= 0 || _backgroundHeight <= 0)
                return;

            // 정렬에 따른 배경 오프셋 계산
            float bgOffsetX = CalculateBackgroundOffsetX();
            float bgOffsetY = CalculateBackgroundOffsetY();

            // 배경 사각형 정점 (시계 반대 방향)
            float[] vertices = new float[]
            {
                bgOffsetX, bgOffsetY,  // 좌하단
                bgOffsetX + _backgroundWidth, bgOffsetY,  // 우하단
                bgOffsetX + _backgroundWidth, bgOffsetY + _backgroundHeight,  // 우상단
                bgOffsetX, bgOffsetY,  // 좌하단
                bgOffsetX + _backgroundWidth, bgOffsetY + _backgroundHeight,  // 우상단
                bgOffsetX, bgOffsetY + _backgroundHeight   // 좌상단
            };

            Gl.BindBuffer(BufferTarget.ArrayBuffer, _backgroundVBO);
            Gl.BufferData(BufferTarget.ArrayBuffer,
                (uint)(vertices.Length * sizeof(float)),
                vertices,
                BufferUsage.DynamicDraw);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        /// <summary>
        /// 정렬에 따른 배경 X 오프셋
        /// </summary>
        private float CalculateBackgroundOffsetX()
        {
            if ((Alignment & TextAlignment.Right) != 0)
            {
                return -_backgroundWidth + _paddingRight;
            }
            else if ((Alignment & TextAlignment.Center) != 0)
            {
                return -_backgroundWidth * 0.5f;
            }
            // Left
            return -_paddingLeft;
        }

        /// <summary>
        /// 정렬에 따른 배경 Y 오프셋
        /// </summary>
        private float CalculateBackgroundOffsetY()
        {
            if ((Alignment & TextAlignment.Top) != 0)
            {
                return -_backgroundHeight + _paddingBottom;
            }
            else if ((Alignment & TextAlignment.Middle) != 0)
            {
                return -_backgroundHeight * 0.5f;
            }
            // Bottom
            return -_paddingTop;
        }

        /// <summary>
        /// 렌더링 (배경 + 텍스트)
        /// </summary>
        public new void Render()
        {
            // 프레임 카운터 체크
            if (_useFrameCounter)
            {
                _frameCounter++;
                if (_frameCounter >= _maxFrames)
                {
                    IsVisible = false;
                    return;
                }
            }

            if (!IsVisible) return;

            // 1. 배경 렌더링
            if (_showBackground && _backgroundAlpha > 0)
            {
                RenderBackground();
            }

            // 블렌딩 상태 초기화
            Gl.Disable(EnableCap.Blend);

            // 2. 텍스트 렌더링
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            base.Render();

        }

        /// <summary>
        /// 배경만 렌더링
        /// </summary>
        private void RenderBackground()
        {
            UpdateBackgroundBuffer();

            if (_backgroundWidth <= 0 || _backgroundHeight <= 0)
                return;

            if (!SimpleColorShader.IsInitialized)
            {
                Console.WriteLine("⚠️ SimpleColorShader가 초기화되지 않음");
                return;
            }

            // 모델 행렬 (텍스트와 동일)
            Matrix4x4f modelMatrix = Matrix4x4f.Identity;
            modelMatrix[3, 0] = X;
            modelMatrix[3, 1] = Y;
            modelMatrix[3, 2] = 0;

            // MVP 계산
            Matrix4x4f mvp = _projectionMatrix * modelMatrix;

            SimpleColorShader.Use();
            SimpleColorShader.SetMVPMatrix(mvp);
            SimpleColorShader.SetColor(_backgroundColor);
            SimpleColorShader.SetAlpha(_backgroundAlpha);

            // 블렌딩 활성화
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            Gl.BindVertexArray(_backgroundVAO);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
            Gl.BindVertexArray(0);

            Gl.Disable(EnableCap.Blend);
        }

        /// <summary>
        /// 텍스트 변경 시 배경 업데이트 필요 표시
        /// </summary>
        public new string Text
        {
            get => base.Text;
            set
            {
                base.Text = value;
                EnableFrameCounter(_maxFrames);
                _backgroundDirty = true;
            }
        }

        /// <summary>
        /// 스케일 변경 시 배경 업데이트 필요 표시
        /// </summary>
        public new float Scale
        {
            get => base.Scale;
            set
            {
                base.Scale = value;
                _backgroundDirty = true;
            }
        }

        /// <summary>
        /// 정렬 변경 시 배경 업데이트 필요 표시
        /// </summary>
        public new TextAlignment Alignment
        {
            get => base.Alignment;
            set
            {
                base.Alignment = value;
                _backgroundDirty = true;
            }
        }

        /// <summary>
        /// 리소스 정리
        /// </summary>
        public new void Dispose()
        {
            if (_backgroundVAO != 0)
            {
                Gl.DeleteVertexArrays(_backgroundVAO);
                _backgroundVAO = 0;
            }

            if (_backgroundVBO != 0)
            {
                Gl.DeleteBuffers(_backgroundVBO);
                _backgroundVBO = 0;
            }

            base.Dispose();
        }
    }

}