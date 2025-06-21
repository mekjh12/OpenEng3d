using OpenGL;
using System.IO;
using Common;

namespace Shader
{
    public class SkyBoxShader : ShaderProgram<UnlitShader.UNIFORM_NAME>
    {
        const string vertex_sources = @"
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

            // optimization : z=1로 만들어 이미 드린 픽셀은 생략가능하다.
            // (1) proj : pos.xyww; z=w로 만들어 무한 원점으로 만든다.
            // (2) revProj :  vec4(pos.x, pos.y, 0.0f, pos.w); z=0으로 만들어 무한 원점으로 만든다.

            gl_Position = vec4(pos.x, pos.y, pos.w, pos.w);
        }
        ";

        const string fragment_sources = @"
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
        // 픽셀에 세이더 색상으로부터 안개를 적용하여 반환한다.
        // param : shadedColor 세이더한 픽셀의 색상
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

        public SkyBoxShader(string projectPath) : base()
        {
            _name = this.GetType().Name;

            string vertFileName = projectPath + "\\sky_vert.tmp";
            File.WriteAllText(vertFileName, vertex_sources);
            VertFileName = vertFileName;

            string fragFileName = projectPath + "\\sky_frag.tmp";
            File.WriteAllText(fragFileName, fragment_sources);
            FragFilename = fragFileName;

            InitCompileShader();
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
        }

        protected override void GetAllUniformLocations()
        {
            UniformLocations("view", "proj");
            UniformLocations("camPos");
            UniformLocations("fogColor", "fogDensity", "fogPlane");
        }

        public void LoadTexture(string textureUniformName, TextureUnit textureUnit, uint texture)
        {
            base.LoadInt(_location[textureUniformName], textureUnit - TextureUnit.Texture0);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        public void LoadProjMatrix(Matrix4x4f matrix)
        {
            base.LoadMatrix(_location["proj"], matrix);
        }

        public void LoadViewMatrix(Matrix4x4f matrix)
        {
            base.LoadMatrix(_location["view"], matrix);
        }

        public void LoadObjectColor(Vertex4f color)
        {
            base.LoadVector(_location["color"], color);
        }

        public void LoadCameraPosition(Vertex3f pos)
        {
            base.LoadVector(_location["camPos"], pos);
        }

        public void LoadFogPlane(Vertex4f fogPlane)
        {
            base.LoadVector(_location["fogPlane"], fogPlane);
        }

        public void LoadFogDensity(float density)
        {
            base.LoadFloat(_location["fogDensity"], density);
        }

        public void LoadFogColor(Vertex3f fogcolor)
        {
            base.LoadVector(_location["fogColor"], fogcolor);
        }
    }
}
