using Common;
using Common.Abstractions;
using OpenGL;

namespace Shader
{
    public class AABBBoxShader : ShaderProgramBase
    {
        const string VERTEx_FILE = @"\Shader\AABBShader\aabb_simple.vert";
        const string GEOMETRY_FILE = @"\Shader\AABBShader\aabb_simple.geom";
        const string FRAGMENT_FILE = @"\Shader\AABBShader\aabb_simple.frag";

        private int loc_vp;
        private int loc_aabbMin;
        private int loc_aabbMax;
        private int loc_color;

        private uint _dummyVAO;

        public AABBBoxShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEx_FILE;
            GeomFileName = projectPath + GEOMETRY_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            InitCompileShader();

            _dummyVAO = Gl.GenVertexArray();
        }

        protected override void GetAllUniformLocations()
        {
            loc_vp = GetUniformLocation("u_vp");
            loc_aabbMin = GetUniformLocation("u_min");
            loc_aabbMax = GetUniformLocation("u_max");
            loc_color = GetUniformLocation("u_color");
        }

        protected override void BindAttributes()
        {
            // Geometry Shader가 처리
        }

        public void LoadAABB(in AABB3f aabb)
        {
            Gl.Uniform3(loc_aabbMin, aabb.Min);
            Gl.Uniform3(loc_aabbMax, aabb.Max);
        }

        public void LoadColor(float r, float g, float b, float a)
        {
            Gl.Uniform4(loc_color, r, g, b, a);
        }

        public void RenderAABB(in AABB3f aabb, Camera camera, Vertex4f color)
        {
            Bind();
            LoadUniformMatrix4(loc_vp, camera.VPMatrix);
            LoadAABB(aabb);
            LoadColor(color.x, color.y, color.z, color.w);

            Gl.BindVertexArray(_dummyVAO);
            Gl.DrawArrays(PrimitiveType.Points, 0, 1);
            Gl.BindVertexArray(0);

            Unbind();
        }
    }
}