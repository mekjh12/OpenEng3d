using Common.Abstractions;
using OpenGL;
using System;
using ZetaExt;

namespace Ui3d
{
    /// <summary>
    /// 3D 공간에 텍스처를 표시하는 빌보드의 기본 클래스
    /// <br/>
    /// UI2d와 완전히 분리된 독립 시스템
    /// </summary>
    public abstract class Billboard3D : IDisposable
    {
        // -----------------------------------------------------------------------
        // 멤버 변수
        // -----------------------------------------------------------------------

        protected Vertex3f _worldPosition;              // 월드 좌표
        protected Vertex3f _offset;                     // 로컬 오프셋
        protected Camera _camera;                       // 카메라 참조

        // 텍스처w
        protected uint _textureId;                      // GPU 텍스처 ID
        protected int _textureWidth;                    // 텍스처 너비
        protected int _textureHeight;                   // 텍스처 높이
        protected bool _isDirty = true;                 // 텍스처 갱신 필요 여부

        // 렌더링 옵션
        protected bool _faceCamera = true;              // 카메라를 향할지 여부
        protected bool _scaleWithDistance = false;       // 거리에 따른 스케일 조절
        protected bool _fadeWithDistance = true;        // 거리에 따른 페이드 효과

        // 거리 설정
        protected float _minDistance = 0.1f;            // 최소 표시 거리
        protected float _maxDistance = 100.0f;          // 최대 표시 거리
        protected float _fadeStartDistance = 20.0f;     // 페이드 시작 거리

        // 크기 설정
        protected float _width = 1.0f;                  // 너비 (월드에서의 그대로의 크기)
        protected float _height = 1.0f;                 // 높이 (월드에서의 그대로의 크기)
        protected float _baseScale = 1.0f;              // 기본 스케일
        protected float _minScale = 1.0f;               // 최소 스케일
        protected float _maxScale = 10.0f;               // 최대 스케일

        // 상태
        protected bool _isVisible = true;               // 표시 여부(활성화 되어도 뷰프러스텀 안에 없으면 표시하지 않는 용도)
        protected bool _isActive = true;                // 활성화 여부
        protected float _alpha = 1.0f;                  // 투명도 (0.0 ~ 1.0)

        // 렌더링용 버퍼
        protected uint _vao;                            // Vertex Array Object
        protected uint _vbo;                            // Vertex Buffer Object

        // 최적화용 임시변수
        protected Vertex3f _finalWorldPosition;           // 월드 좌표 (오프셋 적용)
        protected Vertex3f _finalTextWorldPosition;       // 텍스처가 그려질 최종 월드 좌표 (오프셋 적용)
        protected float _distanceToCamera;                // 카메라와의 거리
        protected Matrix4x4f _modelMatrix;                // 모델 행렬
        protected Matrix4x4f _mvp;                        // MVP 행렬

        // 빌보드 행렬 계산용 캐시
        protected Vertex3f _cachedCameraPosition;         // 이전 프레임 카메라 위치
        protected Vertex3f _cachedToCamera;               // 카메라 방향 벡터 (정규화됨)
        protected Vertex3f _cachedRight;                  // Right 벡터 (정규화됨)
        protected Vertex3f _cachedUp;                     // Up 벡터 (정규화됨)
        protected bool _needRecalculateBasis = true;      // 기저 벡터 재계산 필요 여부

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        public Vertex3f WorldPosition { get => _worldPosition; set => _worldPosition = value; }
        public Vertex3f Offset { get => _offset; set => _offset = value; }
        public bool IsVisible => _isVisible && _isActive;
        public bool IsActive { get => _isActive; set => _isActive = value; }
        public float Width { get => _width; set => _width = value; }
        public float Height { get => _height; set => _height = value; }
        public float Alpha { get => _alpha; set => _alpha = Math.Max(0, Math.Min(1, value)); }

        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        /// <summary>
        /// 빌보드 기본 클래스 생성자
        /// </summary>
        /// <param name="camera">카메라</param>
        protected Billboard3D(Camera camera)
        {
            _camera = camera;
            _worldPosition = Vertex3f.Zero;
            _offset = Vertex3f.Zero;

            // 임시 변수 초기화
            _finalWorldPosition = Vertex3f.Zero;
            _finalTextWorldPosition = Vertex3f.Zero;
            _modelMatrix = Matrix4x4f.Identity;
            _mvp = Matrix4x4f.Identity;

            // 캐시 변수 초기화
            _cachedCameraPosition = Vertex3f.Zero;
            _cachedToCamera = Vertex3f.Zero;
            _cachedRight = Vertex3f.Zero;
            _cachedUp = Vertex3f.Zero;

            CreateQuad();
        }

        // -----------------------------------------------------------------------
        // 공개 메서드
        // -----------------------------------------------------------------------

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
        /// 업데이트
        /// </summary>
        /// <param name="deltaTime">프레임 간 시간 (밀리초)</param>
        public virtual void Update(int deltaTime)
        {
            if (!_isActive)
                return;

            CalculateDistanceToCamera();
            CalculateFinalWorldPosition();

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
        public virtual void Render()
        {
            if (!IsVisible) return;

            if (_isDirty) UpdateTexture();

            if (_textureId == 0) return;

            float alpha = CalculateAlpha(_distanceToCamera);

            if (alpha <= 0) return;

            // 행렬 계산
            CalculateBillboardMatrix(ref _modelMatrix);
            _mvp = _camera.ProjectiveMatrix * _camera.ViewMatrix * _modelMatrix;

            // 셰이더 설정 (static 메서드 호출)
            BillboardShader.Use();
            BillboardShader.SetMVPMatrix(_mvp);
            BillboardShader.SetAlpha(alpha);
            BillboardShader.SetBillboardTexture(_textureId, 0);

            // 렌더링
            Gl.BindVertexArray(_vao);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
            Gl.BindVertexArray(0);
        }

        /// <summary>
        /// 리소스 정리
        /// </summary>
        public virtual void Dispose()
        {
            if (_textureId != 0)
            {
                Gl.DeleteTextures(_textureId);
                _textureId = 0;
            }

            if (_vao != 0)
            {
                Gl.DeleteVertexArrays(_vao);
                _vao = 0;
            }

            if (_vbo != 0)
            {
                Gl.DeleteBuffers(_vbo);
                _vbo = 0;
            }
        }

        // -----------------------------------------------------------------------
        // 보호된 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// 텍스처 업데이트 (자식 클래스에서 구현)
        /// </summary>
        protected abstract void UpdateTexture();

        /// <summary>
        /// 텍스처를 GPU에 업로드
        /// </summary>
        /// <param name="bitmap">업로드할 비트맵</param>
        protected void UploadTextureToGPU(System.Drawing.Bitmap bitmap)
        {
            if (_textureId != 0)
            {
                Gl.DeleteTextures(_textureId);
            }

            _textureWidth = bitmap.Width;
            _textureHeight = bitmap.Height;

            _textureId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _textureId);

            // 텍스처 파라미터 설정
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureMinFilter, Gl.LINEAR);
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureMagFilter, Gl.LINEAR);
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);

            // 비트맵 데이터 잠금
            System.Drawing.Imaging.BitmapData bmpData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            // GPU에 업로드
            Gl.TexImage2D(
                TextureTarget.Texture2d,
                0,
                InternalFormat.Rgba,
                bitmap.Width,
                bitmap.Height,
                0,
                PixelFormat.Bgra,
                PixelType.UnsignedByte,
                bmpData.Scan0
            );

            bitmap.UnlockBits(bmpData);
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            _isDirty = false;
        }

        /// <summary>
        /// 거리에 따른 스케일 계산
        /// </summary>
        /// <param name="distance">카메라와의 거리</param>
        /// <returns>적용할 스케일 값</returns>
        protected float CalculateScale(float distance)
        {
            // 스케일 조절 안함
            if (!_scaleWithDistance)
                return _baseScale;

            // 거리 기반 스케일 계산
            float scale = _baseScale * (-100f / (distance + 10f)) + 10.1f;
            return Math.Max(_minScale, Math.Min(_maxScale, scale));
        }

        /// <summary>
        /// 거리에 따른 알파값 계산
        /// </summary>
        /// <param name="distance">카메라와의 거리</param>
        /// <returns>적용할 알파 값</returns>
        protected float CalculateAlpha(float distance)
        {
            if (!_fadeWithDistance)
                return _alpha;

            if (distance < _fadeStartDistance)
                return _alpha;

            if (distance >= _maxDistance)
                return 0.0f;

            float fadeRange = _maxDistance - _fadeStartDistance;
            float fadeAmount = (distance - _fadeStartDistance) / fadeRange;

            return _alpha * (1.0f - fadeAmount);
        }

        /// <summary>
        /// 빌보드 행렬 계산 (카메라를 향하도록 회전)
        /// <br/>
        /// Z-up 좌표계용
        /// </summary>
        /// <returns>빌보드 모델 행렬</returns>
        protected void CalculateBillboardMatrix(ref Matrix4x4f modelMatrix)
        {
            // 스케일 계산
            float scale = CalculateScale(_distanceToCamera);
            float finalWidth = _width * scale;
            float finalHeight = _height * scale;
            float offsetScale = 0.15f * scale + 0.85f;

            // 오프셋 적용된 텍스트 최종 위치 계산
            _finalTextWorldPosition.x = _finalWorldPosition.x + _offset.x * (offsetScale - 1.0f);
            _finalTextWorldPosition.y = _finalWorldPosition.y + _offset.y * (offsetScale - 1.0f);
            _finalTextWorldPosition.z = _finalWorldPosition.z + _offset.z * (offsetScale - 1.0f);

            modelMatrix = Matrix4x4f.Identity;

            if (_faceCamera)
            {
                // ✅ 개선: 기저 벡터가 변경된 경우에만 재계산
                if (_needRecalculateBasis)
                {
                    // 카메라 방향 벡터 계산
                    _cachedToCamera.x = _camera.Position.x - _finalWorldPosition.x;
                    _cachedToCamera.y = _camera.Position.y - _finalWorldPosition.y;
                    _cachedToCamera.z = _camera.Position.z - _finalWorldPosition.z;
                    _cachedToCamera.Normalize();

                    // Z-up 좌표계: Up 벡터는 (0, 0, 1)
                    Vertex3f worldUp = new Vertex3f(0, 0, 1);

                    // Right 벡터 계산 (외적)
                    _cachedRight = worldUp.Cross(_cachedToCamera);
                    _cachedRight.Normalize();

                    // Up 벡터 재계산
                    _cachedUp = _cachedToCamera.Cross(_cachedRight);
                    _cachedUp.Normalize();

                    _needRecalculateBasis = false;
                }

                // 빌보드 행렬 구성 (캐시된 벡터 사용)
                modelMatrix[0, 0] = _cachedRight.x * finalWidth;
                modelMatrix[0, 1] = _cachedRight.y * finalWidth;
                modelMatrix[0, 2] = _cachedRight.z * finalWidth;

                modelMatrix[1, 0] = _cachedUp.x * finalHeight;
                modelMatrix[1, 1] = _cachedUp.y * finalHeight;
                modelMatrix[1, 2] = _cachedUp.z * finalHeight;

                modelMatrix[2, 0] = _cachedToCamera.x;
                modelMatrix[2, 1] = _cachedToCamera.y;
                modelMatrix[2, 2] = _cachedToCamera.z;
            }

            // 위치 적용
            modelMatrix[3, 0] = _finalTextWorldPosition.x;
            modelMatrix[3, 1] = _finalTextWorldPosition.y;
            modelMatrix[3, 2] = _finalTextWorldPosition.z;
        }

        /// <summary>
        /// 프러스텀 컬링 (간단한 버전)
        /// </summary>
        /// <param name="position">검사할 위치</param>
        /// <returns>프러스텀 안에 있으면 true</returns>
        protected bool IsInFrustum(Vertex3f position)
        {
            // 카메라 뒤에 있으면 제외
            Vertex3f toPoint = new Vertex3f(
                position.x - _camera.Position.x,
                position.y - _camera.Position.y,
                position.z - _camera.Position.z
            );

            Vertex3f forward = _camera.Forward;
            float forwardDot = toPoint.Dot(forward);

            // 카메라 뒤에 있으면 false
            if (forwardDot <= 0) return false;

            // 거리가 너무 가까우면 무조건 true (FOV 계산 불필요)
            if (forwardDot < 1.0f) return true;

            // 정규화된 방향 벡터
            float invLength = 1.0f / (float)Math.Sqrt(
                toPoint.x * toPoint.x +
                toPoint.y * toPoint.y +
                toPoint.z * toPoint.z
            );

            Vertex3f normalizedDir = new Vertex3f(
                toPoint.x * invLength,
                toPoint.y * invLength,
                toPoint.z * invLength
            );

            // 카메라 Right 벡터와의 내적 (수평 FOV 체크)
            Vertex3f right = _camera.Right;
            float rightDot = Math.Abs(normalizedDir.Dot(right));

            // FOV를 고려한 컬링 (60도 FOV 가정, cos(30도) ≈ 0.866)
            // rightDot > 0.866 이면 화면 밖
            if (rightDot > 0.866f) return false;

            // 카메라 Up 벡터와의 내적 (수직 FOV 체크)
            Vertex3f up = _camera.Up;
            float upDot = Math.Abs(normalizedDir.Dot(up));

            // 수직 FOV 체크 (일반적으로 수평보다 좁음, 45도 가정, cos(22.5도) ≈ 0.924)
            if (upDot > 0.924f) return false;

            return true;
        }

        // -----------------------------------------------------------------------
        // 내부 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// 사각형 메쉬 생성
        /// </summary>
        private void CreateQuad()
        {
            // 정점 데이터 (위치 + UV)
            // x, y, z, u, v
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

            _vao = Gl.GenVertexArray();
            Gl.BindVertexArray(_vao);

            _vbo = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
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
        }

    }
}