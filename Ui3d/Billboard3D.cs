using Common.Abstractions;
using OpenGL;
using System;

namespace Ui3d
{
    /// <summary>
    /// 3D 공간에 텍스처를 표시하는 빌보드의 기본 클래스
    /// UI2d와 완전히 분리된 독립 시스템
    /// </summary>
    public abstract class Billboard3D : IDisposable
    {
        protected Vertex3f _worldPosition;
        protected Vertex3f _offset;
        protected Camera _camera;

        // 텍스처
        protected uint _textureId;
        protected int _textureWidth;
        protected int _textureHeight;
        protected bool _isDirty = true;

        // 렌더링 옵션
        protected bool _faceCamera = true;
        protected bool _scaleWithDistance = true;
        protected bool _fadeWithDistance = true;

        // 거리 설정
        protected float _minDistance = 1.0f;
        protected float _maxDistance = 100.0f;
        protected float _fadeStartDistance = 70.0f;

        // 크기 설정
        protected float _width = 1.0f;   // 월드 스페이스 크기
        protected float _height = 0.5f;
        protected float _baseScale = 1.0f;
        protected float _minScale = 0.3f;
        protected float _maxScale = 2.0f;

        // 상태
        protected bool _isVisible = true;
        protected bool _isActive = true;
        protected float _alpha = 1.0f;

        // 렌더링용 버퍼
        protected uint _vao;
        protected uint _vbo;

        #region Properties

        public Vertex3f WorldPosition
        {
            get => _worldPosition;
            set => _worldPosition = value;
        }

        public Vertex3f Offset
        {
            get => _offset;
            set => _offset = value;
        }

        public bool IsVisible => _isVisible && _isActive;

        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        public float Width
        {
            get => _width;
            set => _width = value;
        }

        public float Height
        {
            get => _height;
            set => _height = value;
        }

        public float Alpha
        {
            get => _alpha;
            set => _alpha = Math.Max(0, Math.Min(1, value));
        }

        #endregion

        protected Billboard3D(Camera camera)
        {
            _camera = camera;
            _worldPosition = Vertex3f.Zero;
            _offset = Vertex3f.Zero;

            CreateQuad();
        }

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

            // 위치 속성
            Gl.EnableVertexAttribArray(0);
            Gl.VertexAttribPointer(0, 3, VertexAttribType.Float, false,
                5 * sizeof(float), IntPtr.Zero);

            // UV 속성
            Gl.EnableVertexAttribArray(1);
            Gl.VertexAttribPointer(1, 2, VertexAttribType.Float, false,
                5 * sizeof(float), (IntPtr)(3 * sizeof(float)));

            Gl.BindVertexArray(0);
        }

        /// <summary>
        /// 최종 월드 좌표 계산
        /// </summary>
        public Vertex3f GetFinalWorldPosition()
        {
            return new Vertex3f(
                _worldPosition.x + _offset.x,
                _worldPosition.y + _offset.y,
                _worldPosition.z + _offset.z
            );
        }

        /// <summary>
        /// 카메라와의 거리
        /// </summary>
        public float GetDistanceToCamera()
        {
            Vertex3f finalPos = GetFinalWorldPosition();
            Vertex3f cameraPos = _camera.Position;

            float dx = finalPos.x - cameraPos.x;
            float dy = finalPos.y - cameraPos.y;
            float dz = finalPos.z - cameraPos.z;

            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// 거리에 따른 스케일
        /// </summary>
        protected float CalculateScale(float distance)
        {
            if (!_scaleWithDistance)
                return _baseScale;

            float scale = _baseScale * (50.0f / (distance + 50.0f));
            return Math.Max(_minScale, Math.Min(_maxScale, scale));
        }

        /// <summary>
        /// 거리에 따른 알파
        /// </summary>
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
        /// 빌보드 행렬 계산 (카메라를 향하도록)
        /// </summary>
        protected Matrix4x4f CalculateBillboardMatrix()
        {
            Vertex3f finalPos = GetFinalWorldPosition();
            float distance = GetDistanceToCamera();

            // 스케일 계산
            float scale = CalculateScale(distance);
            float finalWidth = _width * scale;
            float finalHeight = _height * scale;

            Matrix4x4f modelMatrix = Matrix4x4f.Identity;

            if (_faceCamera)
            {
                // 카메라를 향하도록 회전
                Vertex3f cameraPos = _camera.Position;
                Vertex3f direction = new Vertex3f(
                    cameraPos.x - finalPos.x,
                    cameraPos.y - finalPos.y,
                    cameraPos.z - finalPos.z
                );
                direction.Normalize();

                // 뷰 행렬의 역행렬에서 회전 부분만 추출
                Matrix4x4f viewMatrix = _camera.ViewMatrix;

                // 빌보드 회전 (카메라의 역방향)
                modelMatrix[0, 0] = viewMatrix[0, 0];
                modelMatrix[0, 1] = viewMatrix[1, 0];
                modelMatrix[0, 2] = viewMatrix[2, 0];

                modelMatrix[1, 0] = viewMatrix[0, 1];
                modelMatrix[1, 1] = viewMatrix[1, 1];
                modelMatrix[1, 2] = viewMatrix[2, 1];

                modelMatrix[2, 0] = viewMatrix[0, 2];
                modelMatrix[2, 1] = viewMatrix[1, 2];
                modelMatrix[2, 2] = viewMatrix[2, 2];
            }

            // 스케일 적용
            modelMatrix[0, 0] *= finalWidth;
            modelMatrix[1, 0] *= finalWidth;
            modelMatrix[2, 0] *= finalWidth;

            modelMatrix[0, 1] *= finalHeight;
            modelMatrix[1, 1] *= finalHeight;
            modelMatrix[2, 1] *= finalHeight;

            // 위치 적용
            modelMatrix[3, 0] = finalPos.x;
            modelMatrix[3, 1] = finalPos.y;
            modelMatrix[3, 2] = finalPos.z;

            return modelMatrix;
        }

        /// <summary>
        /// 업데이트
        /// </summary>
        public virtual void Update(int deltaTime)
        {
            if (!_isActive)
                return;

            float distance = GetDistanceToCamera();

            // 거리 체크
            if (distance < _minDistance || distance > _maxDistance)
            {
                _isVisible = false;
                return;
            }

            // 화면 안에 있는지 체크 (간단한 버전)
            Vertex3f finalPos = GetFinalWorldPosition();
            if (IsInFrustum(finalPos))
            {
                _isVisible = true;
            }
            else
            {
                _isVisible = false;
            }
        }

        /// <summary>
        /// 프러스텀 컬링 (간단한 버전)
        /// </summary>
        protected bool IsInFrustum(Vertex3f position)
        {
            // 카메라 뒤에 있으면 제외
            Vertex3f toPoint = new Vertex3f(
                position.x - _camera.Position.x,
                position.y - _camera.Position.y,
                position.z - _camera.Position.z
            );

            Vertex3f forward = _camera.Forward;
            float dot = toPoint.x * forward.x + toPoint.y * forward.y + toPoint.z * forward.z;

            return dot > 0;
        }

        /// <summary>
        /// 렌더링
        /// </summary>
        public virtual void Render(BillboardShader shader)
        {
            if (!IsVisible)
                return;

            if (_isDirty)
                UpdateTexture();

            if (_textureId == 0)
                return;

            float distance = GetDistanceToCamera();
            float alpha = CalculateAlpha(distance);

            if (alpha <= 0)
                return;

            // 행렬 계산
            Matrix4x4f modelMatrix = CalculateBillboardMatrix();
            Matrix4x4f viewMatrix = _camera.ViewMatrix;
            Matrix4x4f projectionMatrix = _camera.ProjectiveMatrix;
            Matrix4x4f mvp = projectionMatrix * viewMatrix * modelMatrix;

            // 셰이더 설정
            shader.Use();
            shader.SetMatrix("mvp", mvp);
            shader.SetFloat("alpha", alpha);
            shader.SetTexture("billboardTexture", _textureId, 0);

            // 렌더링
            Gl.BindVertexArray(_vao);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
            Gl.BindVertexArray(0);
        }

        /// <summary>
        /// 텍스처 업데이트 (자식 클래스에서 구현)
        /// </summary>
        protected abstract void UpdateTexture();

        /// <summary>
        /// 텍스처 생성 (GDI+로 그린 비트맵을 GPU에 업로드)
        /// </summary>
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

            // 텍스처 파라미터
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
        /// 정리
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
    }
}