using Common;
using OpenGL;
using System;

namespace Shader
{
    /// <summary>
    /// 렌더 버퍼의 깊이맵을 화면 평면에 그려주는 셰이더
    /// </summary>
    public class RenderDepthBufferShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\RenderDepthBufferShader\dummy.vert";
        const string GEOMETRY_FILE = @"\Shader\RenderDepthBufferShader\post.gs.glsl";
        const string FRAGMENT_FILE = @"\Shader\RenderDepthBufferShader\renderdepth.frag";

        // ✅ 유니폼 위치 캐싱
        private int loc_IsPerspective;
        private int loc_DepthTexture;
        private int loc_CameraFar;
        private int loc_CameraNear;

        public RenderDepthBufferShader(string projectPath) : base()
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
            loc_IsPerspective = GetUniformLocation("IsPerspective");
            loc_DepthTexture = GetUniformLocation("DepthTexture");
            loc_CameraFar = GetUniformLocation("CameraFar");
            loc_CameraNear = GetUniformLocation("CameraNear");
        }

        protected override void BindAttributes()
        {
            // 풀스크린 쿼드는 geometry shader에서 생성하므로 attribute 없음
        }

        /// <summary>
        /// 깊이맵을 원근형 렌더링 여부
        /// </summary>
        public void LoadIsPerspective(bool isPerspective)
        {
            Gl.Uniform1(loc_IsPerspective, isPerspective ? 1 : 0);
        }

        /// <summary>
        /// 렌더 버퍼의 깊이 텍스처 바인딩
        /// </summary>
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