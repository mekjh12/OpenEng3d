using Common.Abstractions;
using OpenGL;
using System;
using System.Drawing;

namespace Ui3d
{
    /// <summary>
    /// 3D 공간에 표시되는 데미지/힐량 숫자 (인스턴싱 기반)
    /// 위로 떠오르면서 페이드아웃 애니메이션이 적용됩니다.
    /// </summary>
    public class DamageNumber : Billboard3D
    {
        // 정적 공유 쿼드 (모든 DamageNumber가 공유)
        private static uint _sharedQuadVAO;
        private static uint _sharedQuadVBO;
        private static int _sharedQuadRefCount = 0;

        // 인스턴스 빌더 (각 DamageNumber마다)
        private TextInstanceBuilder _instanceBuilder;

        // 애니메이션 설정
        private const float RISE_SPEED = 0.5f;              // 초당 상승 속도 (월드 단위)
        private const int DISPLAY_DURATION = 1500;          // 표시 시간 (밀리초)
        private const int FADE_START_TIME = 500;            // 페이드 시작 시간 (밀리초)

        // 기본 색상
        private static readonly Color DEFAULT_NORMAL_COLOR = Color.FromArgb(255, 255, 255, 255);
        private static readonly Color DEFAULT_CRITICAL_COLOR = Color.FromArgb(255, 255, 50, 50);
        private static readonly Color DEFAULT_HEAL_COLOR = Color.FromArgb(255, 100, 255, 100);

        // 크기 설정
        private const float CRITICAL_SCALE = 1.5f;

        private string _displayText;
        private Color _textColor;
        private bool _isCritical;
        private bool _isHeal;
        private int _elapsedTime;
        private float _startZ;
        private bool _isFinished;

        /// <summary>표시할 텍스트</summary>
        public string DisplayText => _displayText;

        /// <summary>애니메이션이 완료되었는지 여부</summary>
        public bool IsFinished => _isFinished;

        /// <summary>크리티컬 여부</summary>
        public bool IsCritical => _isCritical;

        /// <summary>힐 여부</summary>
        public bool IsHeal => _isHeal;

        /// <summary>
        /// 데미지 숫자를 생성합니다.
        /// </summary>
        /// <param name="camera">참조할 카메라</param>
        /// <param name="damage">표시할 데미지 값</param>
        /// <param name="position">표시할 월드 위치</param>
        /// <param name="isCritical">크리티컬 여부</param>
        /// <param name="isHeal">힐 여부</param>
        public DamageNumber(Camera camera, float damage, Vertex3f position, bool isCritical = false, bool isHeal = false)
            : base(camera)
        {
            _displayText = isHeal ? $"+{(int)damage}" : $"{(int)damage}";
            _isCritical = isCritical;
            _isHeal = isHeal;
            _worldPosition = position;
            _startZ = position.z;
            _offset = Vertex3f.Zero;
            _elapsedTime = 0;
            _isFinished = false;

            // 색상 설정
            if (_isHeal)
                _textColor = DEFAULT_HEAL_COLOR;
            else if (_isCritical)
                _textColor = DEFAULT_CRITICAL_COLOR;
            else
                _textColor = DEFAULT_NORMAL_COLOR;

            // 크기 설정
            _baseScale = _isCritical ? CRITICAL_SCALE : 1.0f;

            // 인스턴스 빌더 생성
            _instanceBuilder = new TextInstanceBuilder();

            // 공유 쿼드 초기화 (최초 1회)
            InitializeSharedQuad();

            // 인스턴스 데이터 초기 생성
            UpdateInstanceData();
        }

        /// <summary>
        /// 공유 쿼드 초기화 (모든 DamageNumber가 공유하는 단일 쿼드)
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
            // 정점 데이터 (위치 + UV)
            float[] vertices = new float[]
            {
                // 위치                      UV
                -0.5f,  0.5f, 0.0f,   0.0f, 0.0f,  // 좌상
                -0.5f, -0.5f, 0.0f,   0.0f, 1.0f,  // 좌하
                 0.5f, -0.5f, 0.0f,   1.0f, 1.0f,  // 우하

                -0.5f,  0.5f, 0.0f,   0.0f, 0.0f,  // 좌상
                 0.5f, -0.5f, 0.0f,   1.0f, 1.0f,  // 우하
                 0.5f,  0.5f, 0.0f,   1.0f, 0.0f   // 우상
            };

            _sharedQuadVAO = Gl.GenVertexArray();
            Gl.BindVertexArray(_sharedQuadVAO);

            _sharedQuadVBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _sharedQuadVBO);
            Gl.BufferData(BufferTarget.ArrayBuffer,
                (uint)(vertices.Length * sizeof(float)),
                vertices, BufferUsage.StaticDraw);

            // 위치 속성 (location = 0)
            Gl.EnableVertexAttribArray(0);
            Gl.VertexAttribPointer(0, 3, VertexAttribType.Float, false,
                5 * sizeof(float), IntPtr.Zero);

            // UV 속성 (location = 1)
            Gl.EnableVertexAttribArray(1);
            Gl.VertexAttribPointer(1, 2, VertexAttribType.Float, false,
                5 * sizeof(float), (IntPtr)(3 * sizeof(float)));

            Gl.BindVertexArray(0);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);

            Console.WriteLine("Shared quad created for DamageNumber");
        }

        /// <summary>
        /// 인스턴스 데이터 업데이트
        /// </summary>
        private void UpdateInstanceData()
        {
            if (!CharacterTextureAtlas.IsInitialized)
            {
                Console.WriteLine("Warning: CharacterTextureAtlas not initialized");
                return;
            }

            if (string.IsNullOrEmpty(_displayText))
            {
                _instanceBuilder.UpdateInstanceBuffer("", CharacterTextureAtlas.Instance);
                return;
            }

            // 중앙 정렬로 인스턴스 버퍼 업데이트
            _instanceBuilder.UpdateInstanceBuffer(_displayText,
                CharacterTextureAtlas.Instance,
                true);  // 중앙 정렬

            // VAO에 인스턴스 속성 설정
            _instanceBuilder.SetupVAOAttributes(_sharedQuadVAO);
        }

        /// <summary>
        /// 업데이트 (애니메이션 처리)
        /// </summary>
        public override void Update(int deltaTime)
        {
            if (_isFinished)
            {
                _isVisible = false;
                return;
            }

            _elapsedTime += deltaTime;

            // 표시 시간이 지나면 완료 처리
            if (_elapsedTime >= DISPLAY_DURATION)
            {
                _isFinished = true;
                _isVisible = false;
                return;
            }

            // 위로 상승
            _worldPosition.z = _startZ + (RISE_SPEED * _elapsedTime / 1000.0f);

            // 페이드 아웃 계산
            if (_elapsedTime >= FADE_START_TIME)
            {
                int fadeTime = _elapsedTime - FADE_START_TIME;
                int fadeDuration = DISPLAY_DURATION - FADE_START_TIME;
                _alpha = 1.0f - ((float)fadeTime / fadeDuration);
            }
            else
            {
                _alpha = 1.0f;
            }

            base.Update(deltaTime);
        }

        /// <summary>
        /// 렌더링
        /// </summary>
        public override void Render()
        {
            if (!IsVisible) return;
            if (_instanceBuilder.InstanceCount == 0) return;
            if (!TextBillboardShader.IsInitialized) return;
            if (!CharacterTextureAtlas.IsInitialized) return;

            float alpha = CalculateAlpha(_distanceToCamera);
            if (alpha <= 0) return;

            // 빌보드 행렬 계산
            CalculateBillboardMatrix(ref _modelMatrix);
            _mvp = _camera.ProjectiveMatrix * _camera.ViewMatrix * _modelMatrix;

            // 셰이더 설정
            TextBillboardShader.Use();
            TextBillboardShader.SetMVPMatrix(_mvp);
            TextBillboardShader.SetAtlasTexture(CharacterTextureAtlas.Instance.TextureId, 0);
            TextBillboardShader.SetTextColor(_textColor);
            TextBillboardShader.SetAlpha(alpha);

            // ✅ 데미지 숫자는 가까운 거리에서만 표시되므로 거리 범위 조정
            TextBillboardShader.SetDistanceRange(2.0f, 15.0f);

            // 인스턴싱 렌더링
            Gl.BindVertexArray(_sharedQuadVAO);
            Gl.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, _instanceBuilder.InstanceCount);
            Gl.BindVertexArray(0);
        }

        /// <summary>
        /// 리소스를 정리합니다.
        /// </summary>
        public override void Dispose()
        {
            _instanceBuilder?.Dispose();

            // 공유 쿼드 참조 카운트 감소
            _sharedQuadRefCount--;
            if (_sharedQuadRefCount == 0)
            {
                CleanupSharedQuad();
            }

            base.Dispose();
        }

        /// <summary>
        /// 공유 쿼드 정리
        /// </summary>
        private static void CleanupSharedQuad()
        {
            if (_sharedQuadVAO != 0)
            {
                Gl.DeleteVertexArrays(_sharedQuadVAO);
                _sharedQuadVAO = 0;
            }

            if (_sharedQuadVBO != 0)
            {
                Gl.DeleteBuffers(_sharedQuadVBO);
                _sharedQuadVBO = 0;
            }

            Console.WriteLine("Shared quad cleaned up for DamageNumber");
        }

        // Billboard3D의 추상 메서드 구현 (더 이상 사용하지 않음)
        protected override void UpdateTexture()
        {
            // 인스턴싱 방식에서는 사용하지 않음
        }
    }
}