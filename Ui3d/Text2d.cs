using Common.Abstractions;
using OpenGL;
using System;
using System.Drawing;

namespace Ui3d
{
    /// <summary>
    /// 화면 2D 공간에 텍스트를 표시하는 클래스 (인스턴싱 기반)
    /// 화면 좌표계(픽셀)를 사용하여 UI 텍스트를 렌더링합니다.
    /// </summary>
    public class Text2d : IDisposable
    {
        // 정적 공유 쿼드 (모든 Text2D가 공유)
        private static uint _sharedQuadVBO;
        private static int _sharedQuadRefCount = 0;

        // 인스턴스가 자신만의 VAO를 가짐
        private uint _vao;

        // 인스턴스 빌더
        private TextInstanceBuilder _instanceBuilder;

        // 화면 크기 (정투영 행렬 계산용)
        private int _screenWidth;
        private int _screenHeight;

        // 위치 및 정렬
        private float _x;  // 화면 X 좌표 (픽셀)
        private float _y;  // 화면 Y 좌표 (픽셀)
        private TextAlignment _alignment;

        // 텍스트 속성
        private string _text;
        private Color _color;
        private float _scale;  // 텍스트 크기 배율

        // 상태
        private bool _isVisible;
        private float _alpha;

        // 행렬
        private Matrix4x4f _projectionMatrix;
        private Matrix4x4f _viewMatrix;
        private Matrix4x4f _modelMatrix;
        private Matrix4x4f _mvp;

        // 기본 색상
        private static readonly Color DEFAULT_COLOR = Color.FromArgb(255, 255, 255, 255);

        // 캐시된 문자 높이
        private static float _cachedCharHeight = -1f;

        /// <summary>텍스트 정렬 방식 (플래그 조합 가능)</summary>
        [Flags]
        public enum TextAlignment
        {
            // 수평 정렬
            Left = 1 << 0,    // 0x01
            Center = 1 << 1,    // 0x02
            Right = 1 << 2,    // 0x04

            // 수직 정렬
            Top = 1 << 3,    // 0x08
            Middle = 1 << 4,    // 0x10
            Bottom = 1 << 5,    // 0x20

            // 기본 조합 (하위 호환성)
            TopLeft = Top | Left,
            TopCenter = Top | Center,
            TopRight = Top | Right,
            MiddleLeft = Middle | Left,
            MiddleCenter = Middle | Center,
            MiddleRight = Middle | Right,
        }

        /// <summary>표시할 텍스트</summary>
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    UpdateInstanceData();
                }
            }
        }

        /// <summary>텍스트 색상</summary>
        public Color Color
        {
            get => _color;
            set => _color = value;
        }

        /// <summary>X 좌표 (픽셀)</summary>
        public float X
        {
            get => _x;
            set => _x = value;
        }

        /// <summary>Y 좌표 (픽셀)</summary>
        public float Y
        {
            get => _y;
            set => _y = value;
        }

        /// <summary>텍스트 크기 배율</summary>
        public float Scale
        {
            get => _scale;
            set
            {
                if (_scale != value)
                {
                    _scale = value;
                    UpdateInstanceData();
                }
            }
        }

        /// <summary>정렬 방식</summary>
        public TextAlignment Alignment
        {
            get => _alignment;
            set
            {
                if (_alignment != value)
                {
                    _alignment = value;
                    UpdateInstanceData();
                }
            }
        }

        /// <summary>표시 여부</summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => _isVisible = value;
        }

        /// <summary>투명도 (0.0 ~ 1.0)</summary>
        public float Alpha
        {
            get => _alpha;
            set => _alpha = Math.Max(0, Math.Min(1, value));
        }

        /// <summary>
        /// 생성자
        /// <code>
        /// 
        ///  TopRight |  TopLeft
        ///           |
        /// --------기준점---------
        ///           |
        ///     Right |  Left
        /// 
        /// </code>
        /// </summary>
        /// <param name="text"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="screenWidth"></param>
        /// <param name="screenHeight"></param>
        /// <param name="alignment"></param>
        /// <param name="heightInPixels"></param>
        public Text2d(string text, float x, float y, int screenWidth, int screenHeight,
                     TextAlignment alignment = TextAlignment.Left | TextAlignment.Top,
                     float heightInPixels = 24.0f)
        {
            float fpsScale = Text2d.CalculateScaleForHeight(heightInPixels);
            _text = text;
            _x = x;
            _y = y;
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            _alignment = alignment;
            _scale = fpsScale;
            _color = DEFAULT_COLOR;
            _isVisible = true;
            _alpha = 1.0f;

            _instanceBuilder = new TextInstanceBuilder();

            _vao = Gl.GenVertexArray();

            InitializeSharedQuad();

            CreateOrthographicProjection();

            _viewMatrix = Matrix4x4f.Identity;
            _modelMatrix = Matrix4x4f.Identity;

            UpdateInstanceData();
        }

        /// <summary>
        /// 화면 크기가 변경되었을 때 호출
        /// </summary>
        public void OnScreenResize(int width, int height)
        {
            _screenWidth = width;
            _screenHeight = height;
            CreateOrthographicProjection();
        }

        /// <summary>
        /// 정투영 행렬 생성 (화면 픽셀 좌표계)
        /// 좌상단이 (0, 0), 우하단이 (width, height)
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
        /// 공유 쿼드 초기화
        /// </summary>
        private void InitializeSharedQuad()
        {
            if (_sharedQuadRefCount == 0)
            {
                CreateSharedQuad();
            }
            _sharedQuadRefCount++;
        }

        /// <summary>
        /// 공유 쿼드 생성
        /// </summary>
        private static void CreateSharedQuad()
        {
            float[] vertices = new float[]
            {
                -0.5f,  0.5f, 0.0f,   0.0f, 1.0f,
                -0.5f, -0.5f, 0.0f,   0.0f, 0.0f,
                 0.5f, -0.5f, 0.0f,   1.0f, 0.0f,
                -0.5f,  0.5f, 0.0f,   0.0f, 1.0f,
                 0.5f, -0.5f, 0.0f,   1.0f, 0.0f,
                 0.5f,  0.5f, 0.0f,   1.0f, 1.0f
            };

            _sharedQuadVBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _sharedQuadVBO);
            Gl.BufferData(BufferTarget.ArrayBuffer,
                (uint)(vertices.Length * sizeof(float)),
                vertices, BufferUsage.StaticDraw);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);

            Console.WriteLine("Shared quad VBO created for Text2D");
        }

        /// <summary>
        /// 자신의 VAO를 설정하는 메서드
        /// </summary>
        private void SetupVAO()
        {
            Gl.BindVertexArray(_vao);

            // 공유 VBO 바인딩 (위치 + UV)
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _sharedQuadVBO);

            // 위치 속성 (location = 0)
            Gl.EnableVertexAttribArray(0);
            Gl.VertexAttribPointer(0, 3, VertexAttribType.Float, false,
                5 * sizeof(float), IntPtr.Zero);

            // UV 속성 (location = 1)
            Gl.EnableVertexAttribArray(1);
            Gl.VertexAttribPointer(1, 2, VertexAttribType.Float, false,
                5 * sizeof(float), (IntPtr)(3 * sizeof(float)));

            // 자신의 인스턴스 속성 설정
            _instanceBuilder.SetupVAOAttributes(_vao);

            Gl.BindVertexArray(0);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        /// <summary>
        /// 수평 정렬 오프셋 계산
        /// </summary>
        private float CalculateHorizontalOffset(float textWidth)
        {
            if ((_alignment & TextAlignment.Right) != 0)
            {
                return -textWidth;
            }
            else if ((_alignment & TextAlignment.Center) != 0)
            {
                return -textWidth * 0.5f;
            }
            // Left 또는 수평 정렬 없음
            return 0f;
        }

        /// <summary>
        /// 수직 정렬 오프셋 계산
        /// </summary>
        private float CalculateVerticalOffset(float textHeight)
        {
            if ((_alignment & TextAlignment.Top) != 0)
            {
                return -textHeight;
            }
            else if ((_alignment & TextAlignment.Middle) != 0)
            {
                return -textHeight * 0.5f;
            }
            // Top 또는 수직 정렬 없음
            return 0f;
        }

        /// <summary>
        /// 텍스트 높이 계산 (스케일 적용 전 기준)
        /// </summary>
        private float CalculateTextHeight()
        {
            return GetStandardCharHeight();
        }

        private void UpdateInstanceData()
        {
            if (!CharacterTextureAtlas.IsInitialized)
            {
                Console.WriteLine("❌ CharacterTextureAtlas not initialized");
                return;
            }

            if (string.IsNullOrEmpty(_text))
            {
                Console.WriteLine("⚠️ 텍스트가 비어있음");
                _instanceBuilder.UpdateInstanceBuffer("", CharacterTextureAtlas.Instance);
                SetupVAO();
                return;
            }

            // 수평 정렬 오프셋 계산
            float textWidth = CharacterTextureAtlas.Instance.CalculateTextWidth(_text);
            float alignmentOffsetX = CalculateHorizontalOffset(textWidth);

            // 수직 정렬 오프셋 계산
            float textHeight = CalculateTextHeight();
            float alignmentOffsetY = CalculateVerticalOffset(textHeight);

            // 정렬 오프셋을 적용하여 인스턴스 데이터 생성
            var instances = _instanceBuilder.GenerateInstanceData(
                _text,
                CharacterTextureAtlas.Instance,
                alignmentOffsetX, alignmentOffsetY, 0f);

            // 스케일 적용
            if (Math.Abs(_scale - 1.0f) > 0.001f)
            {
                for (int i = 0; i < instances.Length; i++)
                {
                    instances[i].offsetX *= _scale;
                    instances[i].offsetY *= _scale;
                    instances[i].charWidth *= _scale;
                    instances[i].charHeight *= _scale;
                }
            }

            // 버퍼 업데이트
            float[] data = _instanceBuilder.ConvertToFloatArray(instances);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _instanceBuilder.InstanceVBO);
            int requiredSize = instances.Length * CharInstanceData.FloatCount * sizeof(float);

            if (requiredSize > 0)
            {
                Gl.BufferData(BufferTarget.ArrayBuffer,
                    (uint)requiredSize,
                    data,
                    BufferUsage.DynamicDraw);
            }

            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);

            // 인스턴스 개수 업데이트
            _instanceBuilder.SetInstanceCount(instances.Length);

            SetupVAO();
        }

        /// <summary>
        /// 렌더링
        /// </summary>
        public void Render()
        {
            if (!_isVisible) return;
            if (_instanceBuilder.InstanceCount == 0) return;
            if (!TextBillboardShader.IsInitialized) return;
            if (!CharacterTextureAtlas.IsInitialized) return;
            if (_alpha <= 0) return;

            // 모델 행렬 - Y 좌표를 화면 좌표계에서 OpenGL 좌표계로 변환
            _modelMatrix = Matrix4x4f.Identity;
            _modelMatrix[3, 0] = _x;
            _modelMatrix[3, 1] = _y;
            _modelMatrix[3, 2] = 0;

            _mvp = _projectionMatrix * _viewMatrix * _modelMatrix;

            TextBillboardShader.Use();
            TextBillboardShader.SetMVPMatrix(_mvp);
            TextBillboardShader.SetAtlasTexture(CharacterTextureAtlas.Instance.TextureId, 0);
            TextBillboardShader.SetTextColor(_color);
            TextBillboardShader.SetAlpha(_alpha);
            TextBillboardShader.SetDistanceRange(0.0f, 1.0f);

            Gl.BindVertexArray(_vao);
            Gl.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, _instanceBuilder.InstanceCount);
            Gl.BindVertexArray(0);
        }

        /// <summary>
        /// 위치 설정
        /// </summary>
        public void SetPosition(float x, float y)
        {
            _x = x;
            _y = y;
        }

        /// <summary>
        /// 리소스를 정리합니다.
        /// </summary>
        public void Dispose()
        {
            _instanceBuilder?.Dispose();

            // 자신의 VAO 삭제
            if (_vao != 0)
            {
                Gl.DeleteVertexArrays(_vao);
                _vao = 0;
            }

            // 공유 쿼드 참조 카운트 감소
            _sharedQuadRefCount--;
            if (_sharedQuadRefCount == 0)
            {
                CleanupSharedQuad();
            }
        }

        /// <summary>
        /// 공유 쿼드 정리
        /// </summary>
        private static void CleanupSharedQuad()
        {
            if (_sharedQuadVBO != 0)
            {
                Gl.DeleteBuffers(_sharedQuadVBO);
                _sharedQuadVBO = 0;
            }

            Console.WriteLine("Shared quad cleaned up for Text2D");
        }

        /// <summary>
        /// CharacterTextureAtlas에서 표준 문자 높이를 가져옵니다.
        /// </summary>
        private static float GetStandardCharHeight()
        {
            if (_cachedCharHeight > 0)
                return _cachedCharHeight;

            if (!CharacterTextureAtlas.IsInitialized)
                return 0.0882f; // 기본값

            // 'A' 문자로 높이를 측정
            var tempBuilder = new TextInstanceBuilder();
            var instances = tempBuilder.GenerateInstanceData(
                "A",
                CharacterTextureAtlas.Instance,
                0, 0, 0);

            if (instances.Length > 0)
            {
                _cachedCharHeight = instances[0].charHeight;
                Console.WriteLine($"표준 문자 높이 감지: {_cachedCharHeight}");
            }
            else
            {
                _cachedCharHeight = 0.0882f;
            }

            tempBuilder.Dispose();
            return _cachedCharHeight;
        }

        /// <summary>
        /// 원하는 픽셀 높이에 맞는 스케일 값을 계산합니다.
        /// </summary>
        /// <param name="desiredHeightInPixels">원하는 텍스트 높이 (픽셀)</param>
        /// <returns>적용할 스케일 값</returns>
        public static float CalculateScaleForHeight(float desiredHeightInPixels)
        {
            float charHeight = GetStandardCharHeight();
            return desiredHeightInPixels / charHeight;
        }
    }
}