using Common.Abstractions;
using OpenGL;
using System;
using System.Drawing;

namespace Ui3d
{
    /// <summary>
    /// 3D 공간에 표시되는 캐릭터 이름표 (인스턴싱 기반)
    /// GPU에서 글자별로 렌더링하여 최고 성능 제공
    /// </summary>
    public class TextNamePlate : Billboard3D
    {
        // 정적 공유 쿼드 (모든 TextNamePlate가 공유)
        private static uint _sharedQuadVBO;
        private static int _sharedQuadRefCount = 0;

        // 인스턴스가 자신만의 VAO를 가짐
        private uint _vao;

        // 인스턴스 빌더 (각 NamePlate마다)
        private TextInstanceBuilder _instanceBuilder;

        // 기본 설정
        private static readonly Color DEFAULT_NAME_COLOR = Color.FromArgb(255, 255, 220, 20);
        private const float DEFAULT_Z_OFFSET = 0.1f;

        private string _characterName;
        private Color _nameColor;
        private bool _centerAlign;

        /// <summary>표시할 캐릭터 이름</summary>
        public string CharacterName
        {
            get => _characterName;
            set
            {
                if (_characterName != value)
                {
                    _characterName = value;
                    UpdateInstanceData();
                }
            }
        }

        /// <summary>이름 텍스트 색상</summary>
        public Color NameColor
        {
            get => _nameColor;
            set
            {
                if (_nameColor != value)
                {
                    _nameColor = value;
                }
            }
        }

        /// <summary>중앙 정렬 여부</summary>
        public bool CenterAlign
        {
            get => _centerAlign;
            set
            {
                if (_centerAlign != value)
                {
                    _centerAlign = value;
                    UpdateInstanceData();
                }
            }
        }

        /// <summary>
        /// 캐릭터 이름표를 생성합니다.
        /// </summary>
        /// <param name="camera">참조할 카메라</param>
        /// <param name="characterName">표시할 캐릭터 이름</param>
        public TextNamePlate(Camera camera, string characterName)
            : base(camera)
        {
            _characterName = characterName;
            _nameColor = DEFAULT_NAME_COLOR;
            _centerAlign = true;
            _offset = new Vertex3f(0, 0, DEFAULT_Z_OFFSET);

            // 인스턴스 빌더 생성
            _instanceBuilder = new TextInstanceBuilder();

            // 자신만의 VAO 생성
            _vao = Gl.GenVertexArray();

            // 공유 쿼드 초기화 (최초 1회)
            InitializeSharedQuad();

            // 인스턴스 데이터 초기 생성
            UpdateInstanceData();
        }

        /// <summary>
        /// 공유 쿼드 초기화 (모든 TextNamePlate가 공유하는 단일 쿼드)
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
        /// 이름표를 즉시 갱신합니다.
        /// </summary>
        public void Refresh()
        {
            _isDirty = true;
        }

        /// <summary>
        /// 공유 쿼드 생성
        /// </summary>
        private static void CreateSharedQuad()
        {
            float[] vertices = new float[]
            {
        -0.5f,  0.5f, 0.0f,   0.0f, 0.0f,
        -0.5f, -0.5f, 0.0f,   0.0f, 1.0f,
         0.5f, -0.5f, 0.0f,   1.0f, 1.0f,
        -0.5f,  0.5f, 0.0f,   0.0f, 0.0f,
         0.5f, -0.5f, 0.0f,   1.0f, 1.0f,
         0.5f,  0.5f, 0.0f,   1.0f, 0.0f
            };

            // ✅ VAO 생성 제거, VBO만 생성
            _sharedQuadVBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _sharedQuadVBO);
            Gl.BufferData(BufferTarget.ArrayBuffer,
                (uint)(vertices.Length * sizeof(float)),
                vertices, BufferUsage.StaticDraw);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);

            Console.WriteLine("Shared quad VBO created for TextNamePlate");
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
        /// 인스턴스 데이터 업데이트
        /// </summary>
        private void UpdateInstanceData()
        {
            if (!CharacterTextureAtlas.IsInitialized)
            {
                Console.WriteLine("Warning: CharacterTextureAtlas not initialized");
                return;
            }

            // 텍스트가 비어있으면 인스턴스 0개
            if (string.IsNullOrEmpty(_characterName))
            {
                _instanceBuilder.UpdateInstanceBuffer("", CharacterTextureAtlas.Instance);
                return;
            }

            // 인스턴스 버퍼 업데이트
            _instanceBuilder.UpdateInstanceBuffer(_characterName,
                CharacterTextureAtlas.Instance,
                _centerAlign);

            // 자신의 VAO 설정
            SetupVAO();
        }

        /// <summary>
        /// 업데이트 (베이스 클래스 오버라이드)
        /// </summary>
        public override void Update(int deltaTime)
        {
            if (!_isActive)
                return;

            CalculateFinalWorldPosition();
            CalculateDistanceToCamera();

            // 카메라 위치 변경 감지
            if (_cachedCameraPosition.x != _camera.Position.x ||
                _cachedCameraPosition.y != _camera.Position.y ||
                _cachedCameraPosition.z != _camera.Position.z)
            {
                _cachedCameraPosition.x = _camera.Position.x;
                _cachedCameraPosition.y = _camera.Position.y;
                _cachedCameraPosition.z = _camera.Position.z;
                _needRecalculateBasis = true;
            }

            // 거리 체크
            if (_distanceToCamera < _minDistance || _distanceToCamera > _maxDistance)
            {
                _isVisible = false;
                return;
            }

            // 프러스텀 컬링
            _isVisible = IsInFrustum(_finalWorldPosition);
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
            TextBillboardShader.SetTextColor(_nameColor);
            TextBillboardShader.SetAlpha(alpha);

            // ✅ 커스텀 거리 범위 설정 (선택사항)
            // TextBillboardShader.SetDistanceRange(3.0f, 50.0f);

            // ✅ 자신의 VAO로 렌더링
            Gl.BindVertexArray(_vao);
            Gl.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, _instanceBuilder.InstanceCount);
            Gl.BindVertexArray(0);
        }

        /// <summary>
        /// 최종 월드 좌표 계산 (오프셋 적용)
        /// </summary>
        private void CalculateFinalWorldPosition()
        {
            _finalWorldPosition.x = _worldPosition.x + _offset.x;
            _finalWorldPosition.y = _worldPosition.y + _offset.y;
            _finalWorldPosition.z = _worldPosition.z + _offset.z;
        }

        /// <summary>
        /// 카메라와의 거리 계산
        /// </summary>
        private void CalculateDistanceToCamera()
        {
            float dx = _finalWorldPosition.x - _camera.Position.x;
            float dy = _finalWorldPosition.y - _camera.Position.y;
            float dz = _finalWorldPosition.z - _camera.Position.z;

            _distanceToCamera = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// 리소스를 정리합니다.
        /// </summary>
        public override void Dispose()
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

            base.Dispose();
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

            Console.WriteLine("Shared quad cleaned up");
        }

        // Billboard3D의 추상 메서드 구현 (더 이상 사용하지 않음)
        protected override void UpdateTexture()
        {
            // 인스턴싱 방식에서는 사용하지 않음
        }
    }
}