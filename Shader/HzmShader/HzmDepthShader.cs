using Common;
using OpenGL;
using System;

namespace Shader
{
    /// <summary>
    /// 계층적 Z버퍼를 화면 평면에 그려주는 세이더
    /// </summary>
    public class HzmDepthShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\HzmShader\dummy.vert";
        const string GEOMETRY_FILE = @"\Shader\HzmShader\post.gs.glsl";
        const string FRAGMENT_FILE = @"\Shader\HzmShader\depth.frag";

        // ✅ 유니폼 위치 캐싱
        private int loc_LOD;
        private int loc_IsPerspective;
        private int loc_DepthTexture;
        private int loc_CameraFar;
        private int loc_CameraNear;

        public HzmDepthShader(string projectPath) : base()
        {
            // 셰이더 초기화
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            GeomFileName = projectPath + GEOMETRY_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;

            // ✅ 컴파일 및 링크
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_LOD = GetUniformLocation("LOD");
            loc_IsPerspective = GetUniformLocation("IsPerspective");
            loc_DepthTexture = GetUniformLocation("DepthTexture");
            loc_CameraFar = GetUniformLocation("CameraFar");
            loc_CameraNear = GetUniformLocation("CameraNear");
        }

        protected override void BindAttributes()
        {
            //base.BindAttribute(0, "position");
        }

        public void LoadLOD(int lod)
        {
            Gl.Uniform1(loc_LOD, lod);
        }

        /// <summary>
        /// 깊이맵을 원근형 렌더링 여부
        /// </summary>
        public void LoadIsPerspective(bool isPerspective)
        {
            Gl.Uniform1(loc_IsPerspective, isPerspective ? 1 : 0);
        }

        public void LoadDepthTexture(TextureUnit textureUnit, uint texture)
        {
            int ind = textureUnit - TextureUnit.Texture0;
            Gl.Uniform1(loc_DepthTexture, ind);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        public void LoadCameraFar(float far)
        {
            Gl.Uniform1(loc_CameraFar, far);
        }

        public void LoadCameraNear(float near)
        {
            Gl.Uniform1(loc_CameraNear, near);
        }
    }
}