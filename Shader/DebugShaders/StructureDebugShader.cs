using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// Structure Buffer 디버그 시각화 셰이더
    /// </summary>
    public class StructureDebugShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\DebugShaders\structure_debug.vert";
        const string FRAGMENT_FILE = @"\Shader\DebugShaders\structure_debug.frag";

        private int loc_structureBuffer;
        private int loc_debugMode;
        private int loc_depthRange;

        public StructureDebugShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;

            InitCompileShader();
        }

        protected override void BindAttributes()
        {
            // Fullscreen quad는 attribute 불필요
            // gl_VertexID만 사용
        }

        protected override void GetAllUniformLocations()
        {
            loc_structureBuffer = GetUniformLocation("structureBuffer");
            loc_debugMode = GetUniformLocation("debugMode");
            loc_depthRange = GetUniformLocation("depthRange");
        }

        /// <summary>
        /// Structure Buffer 텍스처 바인딩
        /// </summary>
        public void LoadStructureBuffer(uint textureId)
        {
            Gl.Uniform1(loc_structureBuffer, 0);
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
        }

        /// <summary>
        /// 디버그 모드 설정
        /// 0: Depth (히트맵)
        /// 1: dz/dx (X 미분)
        /// 2: dz/dy (Y 미분)
        /// 3: Gradient magnitude (경사도)
        /// 4: Raw RGBA
        /// 5: Bit split verification
        /// </summary>
        public void LoadDebugMode(GENG.STRUCTUREBUFFER_DEBUG_MODE mode)
        {
            Gl.Uniform1(loc_debugMode, (int)mode);
        }

        /// <summary>
        /// 깊이 시각화 범위 설정
        /// </summary>
        public void LoadDepthRange(float range)
        {
            Gl.Uniform1(loc_depthRange, range);
        }
    }
}