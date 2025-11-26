using OpenGL;
using System;
using System.IO;
using Common;

namespace Shader
{
    /// <summary>
    /// Worley Noise (Cellular Noise) 생성을 위한 셰이더 클래스입니다.
    /// 3D 텍스처에 절차적 노이즈를 생성하며, 구름이나 돌 표면 등에 활용됩니다.
    /// </summary>
    public class WorleyNoiseGenShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\WorleyNoiseGenShader\worleyGen.vert";
        const string FRAGMENT_FILE = @"\Shader\WorleyNoiseGenShader\worleyGen.frag";

        // 유니폼 위치 (캐싱)
        private int loc_model;
        private int loc_view;
        private int loc_proj;
        private int loc_numCellsPerAxis;

        public WorleyNoiseGenShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_model = GetUniformLocation("model");
            loc_view = GetUniformLocation("view");
            loc_proj = GetUniformLocation("proj");
            loc_numCellsPerAxis = GetUniformLocation("numCellsPerAxis");
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
        }

        // === Load 메서드들 ===

        /// <summary>
        /// 모델 행렬 설정
        /// </summary>
        public void LoadModelMatrix(Matrix4x4f matrix)
        {
            Gl.UniformMatrix4f(loc_model, 1, false, matrix);
        }

        /// <summary>
        /// 뷰 행렬 설정
        /// </summary>
        public void LoadViewMatrix(Matrix4x4f matrix)
        {
            Gl.UniformMatrix4f(loc_view, 1, false, matrix);
        }

        /// <summary>
        /// 프로젝션 행렬 설정
        /// </summary>
        public void LoadProjMatrix(Matrix4x4f matrix)
        {
            Gl.UniformMatrix4f(loc_proj, 1, false, matrix);
        }

        /// <summary>
        /// 축당 셀 개수 설정 (Worley Noise 해상도)
        /// </summary>
        public void LoadNumOfCellPerAxis(int numCellsPerAxis)
        {
            Gl.Uniform1i(loc_numCellsPerAxis, 1, numCellsPerAxis);
        }

        /// <summary>
        /// SSBO(Shader Storage Buffer Object) 바인딩
        /// Worley 포인트 데이터를 GPU로 전달합니다.
        /// </summary>
        /// <param name="bufferIndex">SSBO 버퍼 인덱스</param>
        /// <param name="numCellsPerAxis">축당 셀 개수</param>
        public void BindSSBO(uint bufferIndex, int numCellsPerAxis)
        {
            uint size = (uint)(numCellsPerAxis * numCellsPerAxis * numCellsPerAxis * Vertex3f.Size);
            uint loc = Gl.GetProgramResourceIndex(_programID, ProgramInterface.ShaderStorageBlock, "shader_data");

            Gl.ShaderStorageBlockBinding(_programID, loc, bufferIndex);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, bufferIndex, bufferIndex);
            Gl.BindBufferRange(BufferTarget.ShaderStorageBuffer, bufferIndex, bufferIndex, IntPtr.Zero, size);
        }

        /// <summary>
        /// 3D 텍스처 바인딩 (Worley Noise 출력용)
        /// </summary>
        public void LoadTexture3D(uint texture)
        {
            Gl.BindTexture(TextureTarget.Texture3d, texture);
        }

        /// <summary>
        /// 범용 2D 텍스처 바인딩 (확장용)
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