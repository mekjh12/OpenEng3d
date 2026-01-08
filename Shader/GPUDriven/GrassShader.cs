using OpenGL;
using Common;
using Shader;
using System;
using ZetaExt;

namespace Shader
{
    /// <summary>
    /// GPU 드리븐 풀 렌더링 셰이더 (Vertex Shader Expansion)
    /// - 더미 VAO 사용 (버텍스 데이터 없음)
    /// - gl_VertexID로 쿼드 확장
    /// - SSBO에서 풀 인스턴스 데이터 읽기
    /// </summary>
    public class GrassShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\GPUDriven\glsl\grass.vert";
        const string FRAGMENT_FILE = @"\Shader\GPUDriven\glsl\grass.frag";

        // Uniform 위치
        private int loc_cameraRight;
        private int loc_cameraUp;
        private int loc_grassWidth;
        private int loc_grassHeight;
        private int loc_grassTexture;
        private int loc_sunDirection;
        private int loc_grassColorTop;
        private int loc_grassColorBottom;

        public GrassShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;

            InitCompileShader();
        }

        protected override void BindAttributes()
        {
            // 더미 VAO 사용 - 버텍스 어트리뷰트 없음!
            // gl_VertexID와 gl_InstanceID만 사용
        }

        protected override void GetAllUniformLocations()
        {
            loc_cameraRight = GetUniformLocation("u_CameraRight");
            loc_cameraUp = GetUniformLocation("u_CameraUp");
            loc_grassWidth = GetUniformLocation("u_GrassWidth");
            loc_grassHeight = GetUniformLocation("u_GrassHeight");
            loc_grassTexture = GetUniformLocation("u_GrassTexture");
            loc_sunDirection = GetUniformLocation("u_SunDirection");
            loc_grassColorTop = GetUniformLocation("u_GrassColorTop");
            loc_grassColorBottom = GetUniformLocation("u_GrassColorBottom");
        }

        public void LoadCameraVectors(in Vertex3f right, in Vertex3f up)
        {
            Gl.Uniform3(loc_cameraRight, right.x, right.y, right.z);
            Gl.Uniform3(loc_cameraUp, up.x, up.y, up.z);
        }

        public void LoadGrassSize(float width, float height)
        {
            Gl.Uniform1(loc_grassWidth, width);
            Gl.Uniform1(loc_grassHeight, height);
        }

        public void LoadTexture(uint textureID)
        {
            Gl.Uniform1(loc_grassTexture, 0);
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, textureID);
        }

        public void LoadSunDirection(in Vertex3f dir)
        {
            float len = dir.Length();
            float nx = dir.x / len;
            float ny = dir.y / len;
            float nz = dir.z / len;
            Gl.Uniform3(loc_sunDirection, nx, ny, nz);
        }

        public void LoadGrassColors(in Vertex3f top, in Vertex3f bottom)
        {
            Gl.Uniform3(loc_grassColorTop, top.x, top.y, top.z);
            Gl.Uniform3(loc_grassColorBottom, bottom.x, bottom.y, bottom.z);
        }
    }
}