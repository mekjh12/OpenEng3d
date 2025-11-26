using OpenGL;
using System.IO;
using Common;

namespace Shader
{
    /// <summary>
    /// 스카이박스 렌더링을 위한 셰이더 클래스입니다.
    /// 큐브맵 텍스처를 사용하여 배경 하늘을 렌더링하며, 안개 효과를 지원합니다.
    /// </summary>
    public class SkyBoxShader : ShaderProgramBase
    {
        const string VERTEX_SOURCES = @"
        #version 420 core
        // (-1,-1,-1)--(1,1,1)의 정보가 들어온다.
        in vec3 position;
        out vec3 TexCoords;
        out vec3 fragPos;

        uniform mat4 proj;
        uniform mat4 view;
        void main(void)
        {
            TexCoords = position;
            vec4 pos = proj * view * vec4(position, 1.0);
            fragPos = position * 1000.0f;

            // optimization : z=1로 만들어 이미 그린 픽셀은 생략가능하다.
            // (1) proj : pos.xyww; z=w로 만들어 무한 원점으로 만든다.
            // (2) revProj :  vec4(pos.x, pos.y, 0.0f, pos.w); z=0으로 만들어 무한 원점으로 만든다.

            gl_Position = vec4(pos.x, pos.y, pos.w, pos.w);
        }
        ";

        const string FRAGMENT_SOURCES = @"
        #version 420 core

        out vec4 FragColor;
        in vec3 TexCoords;
        in vec3 fragPos;

        uniform samplerCube skybox;

        uniform vec3 camPos;
        uniform vec3 fogColor;
        uniform float fogDensity;
        uniform vec4 fogPlane;

        // ================================================================
        // 픽셀에 셰이더 색상으로부터 안개를 적용하여 반환한다.
        // param : shadedColor 셰이더한 픽셀의 색상
        //         v  정규화되지 않은 뷰벡터 v 
        // ================================================================
        vec3 ApplyHalfspaceFog(vec3 shadedColor, vec3 fogcolor, vec3 v, float density, float fv, float u1, float u2)
        {
            const float kFogEpsilon = 0.0001f;
            float x = min(u2, 0.0f);
            float tau = 0.5f * density * length(v) * (u1 -  x * x / (abs(fv) + kFogEpsilon));
            return mix(fogcolor, shadedColor, exp(tau));
        }

        void main(void)
        {
            //vec3 tex = vec3(TexCoords.x, TexCoords.y, TexCoords.z);
            //vec4 textureColor4 = texture(skybox, tex);

            // 지울것
            vec4 textureColor4 = vec4(0.0f, 0.3f, 0.65f, 1.0f);

            float fc = dot(camPos, fogPlane.xyz) + fogPlane.w;
            float fp = dot(fragPos.xyz, fogPlane.xyz) + fogPlane.w;
            vec3 v = camPos - fragPos.xyz;
            float fv = dot(v, fogPlane.xyz);
            float m = (fc<0) ? 1.0f: 0.0f;
            float u1 = m * (fc + fp);
            float u2 = fp * sign(fc);

            vec3 final = ApplyHalfspaceFog(textureColor4.xyz, fogColor, v, fogDensity, fv, u1, u2);
            FragColor = vec4(final, 1.0f);
        }
        ";

        // 유니폼 위치 (캐싱)
        private int loc_view;
        private int loc_proj;
        private int loc_camPos;
        private int loc_skybox;
        private int loc_fogColor;
        private int loc_fogDensity;
        private int loc_fogPlane;

        public SkyBoxShader(string projectPath) : base()
        {
            _name = this.GetType().Name;

            // 임시 파일로 셰이더 소스 저장
            string vertFileName = Path.Combine(projectPath, "sky_vert.tmp");
            File.WriteAllText(vertFileName, VERTEX_SOURCES);
            VertFileName = vertFileName;

            string fragFileName = Path.Combine(projectPath, "sky_frag.tmp");
            File.WriteAllText(fragFileName, FRAGMENT_SOURCES);
            FragFileName = fragFileName;

            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_view = GetUniformLocation("view");
            loc_proj = GetUniformLocation("proj");
            loc_camPos = GetUniformLocation("camPos");
            loc_skybox = GetUniformLocation("skybox");
            loc_fogColor = GetUniformLocation("fogColor");
            loc_fogDensity = GetUniformLocation("fogDensity");
            loc_fogPlane = GetUniformLocation("fogPlane");
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
        }

        // === Load 메서드들 ===

        /// <summary>
        /// 프로젝션 행렬 설정
        /// </summary>
        public void LoadProjMatrix(Matrix4x4f matrix)
        {
            Gl.UniformMatrix4f(loc_proj, 1, false, matrix);
        }

        /// <summary>
        /// 뷰 행렬 설정 (평행이동 제거하여 스카이박스가 카메라를 따라다니게 함)
        /// </summary>
        public void LoadViewMatrix(Matrix4x4f matrix)
        {
            Gl.UniformMatrix4f(loc_view, 1, false, matrix);
        }

        /// <summary>
        /// 카메라 위치 설정
        /// </summary>
        public void LoadCameraPosition(Vertex3f position)
        {
            Gl.Uniform3f(loc_camPos, 1, position);
        }

        /// <summary>
        /// 스카이박스 큐브맵 텍스처 바인딩
        /// </summary>
        public void LoadSkyboxTexture(int textureUnit, uint texture)
        {
            Gl.Uniform1i(loc_skybox, 1, textureUnit);
            Gl.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + textureUnit));
            Gl.BindTexture(TextureTarget.TextureCubeMap, texture);
        }

        /// <summary>
        /// 안개 색상 설정
        /// </summary>
        public void LoadFogColor(Vertex3f fogColor)
        {
            Gl.Uniform3f(loc_fogColor, 1, fogColor);
        }

        /// <summary>
        /// 안개 밀도 설정
        /// </summary>
        public void LoadFogDensity(float density)
        {
            Gl.Uniform1f(loc_fogDensity, 1, density);
        }

        /// <summary>
        /// 안개 평면 설정 (높이 기반 안개)
        /// </summary>
        public void LoadFogPlane(Vertex4f fogPlane)
        {
            Gl.Uniform4f(loc_fogPlane, 1, fogPlane);
        }

        /// <summary>
        /// 범용 텍스처 바인딩 (확장용)
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