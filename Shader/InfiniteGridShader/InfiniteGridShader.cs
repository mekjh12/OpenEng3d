using OpenGL;
using System.Drawing.Drawing2D;
using System.IO;
using Common;

namespace Shader
{
    /// <summary>
    /// 쉐이더를 사용하기 위해서는 실행 위치의 \Shader\Terrain\에 소스코드를 넣어주세요.
    /// </summary>
    public class InfiniteGridShader : ShaderProgram<UnlitShader.UNIFORM_NAME>
    {
        const string VERTEX_FILE = @"\Shader\InfiniteGridShader\grid.vert";
        const string FRAGMENT_FILE = @"\Shader\InfiniteGridShader\grid.frag";

        public InfiniteGridShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            UniformLocations("gVP", "gCameraFocusWorldPos", "gCameraWorldPos");
            UniformLocations("viewport_size", "focal_length", "aspect_ratio", "view");
        }

        protected override void BindAttributes()
        {
            //base.BindAttribute(0, "position");
        }

        public void LoadAspectRatio(float aspectRatio)
        {
            base.LoadFloat(_location["aspect_ratio"], aspectRatio);
        }

        public void LoadFocalLength(float focalLength)
        {
            base.LoadFloat(_location["focal_length"], focalLength);
        }

        public void LoadViewportSize(Vertex2f viewportSize)
        {
            base.LoadVector(_location["viewport_size"], viewportSize);
        }

        public void LoadViewMatrix(Matrix4x4f matrix)
        {
            base.LoadMatrix(_location["view"], matrix);
        }

        public void LoadOrbitCameraPosition(Vertex3f cameraPosition) => base.LoadVector(_location["gCameraWorldPos"], cameraPosition);


        public void LoadCameraPosition(Vertex3f cameraPosition) => base.LoadVector(_location["gCameraFocusWorldPos"], cameraPosition);

        public void LoadVPMatrix(Matrix4x4f gVP) => base.LoadMatrix(_location["gVP"], gVP);

    }
}
