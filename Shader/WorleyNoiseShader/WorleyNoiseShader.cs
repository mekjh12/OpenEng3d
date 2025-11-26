using OpenGL;
using System;
using System.IO;
using Common;

namespace Shader
{
    /// <summary>
    /// Worley Noise를 사용한 볼륨 렌더링 셰이더 클래스입니다.
    /// 레이 마칭을 통해 3D 구름이나 연기 등의 볼륨 효과를 렌더링합니다.
    /// </summary>
    public class WorleyNoiseShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\WorleyNoiseShader\worley.vert";
        const string FRAGMENT_FILE = @"\Shader\WorleyNoiseShader\worley.frag";

        // 유니폼 위치 (캐싱)
        private int loc_model;
        private int loc_view;
        private int loc_proj;
        private int loc_numCellsPerAxis;
        private int loc_viewportSize;
        private int loc_focalLength;
        private int loc_aspectRatio;
        private int loc_gamma;
        private int loc_stepLength;
        private int loc_rayOrigin;
        private int loc_volume;
        private int loc_densityPower;
        private int loc_absorption;
        private int loc_centerPosition;
        private int loc_boundSize;
        private int loc_inSide;

        public WorleyNoiseShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            // 변환 행렬
            loc_model = GetUniformLocation("model");
            loc_view = GetUniformLocation("view");
            loc_proj = GetUniformLocation("proj");

            // Worley 설정
            loc_numCellsPerAxis = GetUniformLocation("numCellsPerAxis");

            // 카메라 및 뷰포트
            loc_viewportSize = GetUniformLocation("viewport_size");
            loc_focalLength = GetUniformLocation("focal_length");
            loc_aspectRatio = GetUniformLocation("aspect_ratio");
            loc_rayOrigin = GetUniformLocation("ray_origin");

            // 볼륨 렌더링 파라미터
            loc_gamma = GetUniformLocation("gamma");
            loc_stepLength = GetUniformLocation("step_length");
            loc_volume = GetUniformLocation("volume");
            loc_densityPower = GetUniformLocation("densityPower");
            loc_absorption = GetUniformLocation("absorption");

            // 바운딩 볼륨
            loc_centerPosition = GetUniformLocation("centerPosition");
            loc_boundSize = GetUniformLocation("boundSize");
            loc_inSide = GetUniformLocation("inSide");
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
        }

        // === 변환 행렬 ===

        public void LoadModelMatrix(Matrix4x4f matrix)
        {
            Gl.UniformMatrix4f(loc_model, 1, false, matrix);
        }

        public void LoadViewMatrix(Matrix4x4f matrix)
        {
            Gl.UniformMatrix4f(loc_view, 1, false, matrix);
        }

        public void LoadProjMatrix(Matrix4x4f matrix)
        {
            Gl.UniformMatrix4f(loc_proj, 1, false, matrix);
        }

        // === Worley Noise 설정 ===

        /// <summary>
        /// 축당 셀 개수 설정
        /// </summary>
        public void LoadNumOfCellPerAxis(int numCellsPerAxis)
        {
            Gl.Uniform1i(loc_numCellsPerAxis, 1, numCellsPerAxis);
        }

        /// <summary>
        /// SSBO(Shader Storage Buffer Object) 바인딩
        /// </summary>
        public void BindSSBO(uint bufferIndex, int numCellsPerAxis)
        {
            uint size = (uint)(numCellsPerAxis * numCellsPerAxis * numCellsPerAxis * Vertex3f.Size);
            uint loc = Gl.GetProgramResourceIndex(_programID, ProgramInterface.ShaderStorageBlock, "shader_data");

            Gl.ShaderStorageBlockBinding(_programID, loc, bufferIndex);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, bufferIndex, bufferIndex);
            Gl.BindBufferRange(BufferTarget.ShaderStorageBuffer, bufferIndex, bufferIndex, IntPtr.Zero, size);
        }

        // === 카메라 및 뷰포트 ===

        /// <summary>
        /// 뷰포트 크기 설정 (픽셀 단위)
        /// </summary>
        public void LoadViewportSize(Vertex2f size)
        {
            Gl.Uniform2f(loc_viewportSize, 1, size);
        }

        /// <summary>
        /// 카메라 초점 거리 설정
        /// </summary>
        public void LoadFocalLength(float focalLength)
        {
            Gl.Uniform1f(loc_focalLength, 1, focalLength);
        }

        /// <summary>
        /// 화면 종횡비 설정
        /// </summary>
        public void LoadAspectRatio(float aspectRatio)
        {
            Gl.Uniform1f(loc_aspectRatio, 1, aspectRatio);
        }

        /// <summary>
        /// 레이 원점 (카메라 위치) 설정
        /// </summary>
        public void LoadRayOrigin(Vertex3f origin)
        {
            Gl.Uniform3f(loc_rayOrigin, 1, origin);
        }

        // === 볼륨 렌더링 파라미터 ===

        /// <summary>
        /// 감마 보정 값 설정
        /// </summary>
        public void LoadGamma(float gamma)
        {
            Gl.Uniform1f(loc_gamma, 1, gamma);
        }

        /// <summary>
        /// 레이 마칭 스텝 길이 설정
        /// </summary>
        public void LoadStepLength(float stepLength)
        {
            Gl.Uniform1f(loc_stepLength, 1, stepLength);
        }

        /// <summary>
        /// 밀도 제곱 지수 설정 (밀도 조절)
        /// </summary>
        public void LoadDensityPower(float densityPower)
        {
            Gl.Uniform1f(loc_densityPower, 1, densityPower);
        }

        /// <summary>
        /// 흡수율 설정 (빛의 감쇠)
        /// </summary>
        public void LoadAbsorption(float absorption)
        {
            Gl.Uniform1f(loc_absorption, 1, absorption);
        }

        // === 바운딩 볼륨 ===

        /// <summary>
        /// 볼륨 중심 위치 설정
        /// </summary>
        public void LoadCenterPosition(Vertex3f centerPosition)
        {
            Gl.Uniform3f(loc_centerPosition, 1, centerPosition);
        }

        /// <summary>
        /// 바운딩 박스 크기 설정
        /// </summary>
        public void LoadBoundSize(Vertex3f boundSize)
        {
            Gl.Uniform3f(loc_boundSize, 1, boundSize);
        }

        /// <summary>
        /// 카메라가 볼륨 내부에 있는지 여부
        /// </summary>
        public void LoadCameraInsideCube(bool inSide)
        {
            Gl.Uniform1i(loc_inSide, 1, inSide ? 1 : 0);
        }

        // === 텍스처 ===

        /// <summary>
        /// 3D 볼륨 텍스처 바인딩
        /// </summary>
        public void LoadTexture3D(uint texture)
        {
            Gl.BindTexture(TextureTarget.Texture3d, texture);
        }

        /// <summary>
        /// 범용 2D 텍스처 바인딩
        /// </summary>
        public void LoadTexture(string uniformName, TextureUnit textureUnit, uint texture)
        {
            int location = GetUniformLocation(uniformName);
            Gl.Uniform1i(location, 1, (int)(textureUnit - TextureUnit.Texture0));
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }
    }
}