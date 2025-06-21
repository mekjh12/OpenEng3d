using Common;
using OpenGL;
using Shader;
using System;
using System.IO;
using System.Windows.Forms;

namespace Ui2d
{
    /// <summary>
    /// 사각형의 2d상자를 렌더링해 준다.
    /// 텍스처에 직사각형 모양을 렌더링한다.
    /// </summary>
    public class UIShader : ShaderProgram<Enum>
    {
        const string VERTEX_FILE = @"\ui2d.vert";
        const string FRAGMENT_FILE = @"\ui2d.frag";

        //
        //   (0,0) --- (1,0)               (-1,1)  --- (1,1)
        //     |     /   |        --->       |      /    |   
        //     |   /     |                   |   /       |   
        //   (0,1) --- (1,1)               (-1,-1) --- (1,-1)
        //
        float[] _quadVertices  = { 0, 0, 0, 1, 1, 1, 1, 1, 1, 0, 0, 0 };
        float[] _quadTexCoords = { 0, 0, 0, 1, 1, 1, 1, 1, 1, 0, 0, 0 };

        float[] _quadLineVertices =
        {
            0, 0, 0, 0, 1, 0,  
            0, 1, 0, 1, 1, 0,
            1, 1, 0, 1, 0, 0,
            1, 0, 0, 0, 0, 0
        };

        private RawModel2d _rawModel;
        private RawModel2d _rawModelLine;

        private float _x;
        private float _y;
        private float _width;
        private float _height;

        private int loc_model;
        private int loc_color;
        private int loc_horzflip;
        private int loc_texcoordShift;
        private int loc_texcoordScale;
        private int loc_enableTexture;

        public uint VAO => _rawModel.VAO;

        public uint VAO_LINE => _rawModelLine.VAO;

        public int VertexCount => _rawModel.VertexCount;

        public void SetView(float x, float y, float width, float height)
        {            
            _x = x;
            _y = y;
            _width = width;
            _height = height;
        }

        public UIShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;
            InitCompileShader();
            _rawModel = LoadToFontVertexArray(_quadVertices, _quadTexCoords);
            _rawModelLine = LoadToFontVertexArray(_quadLineVertices);
        }

        public void LoadColor(Vertex4f color)
        {
            base.LoadVector(loc_color, color);
        }

        public void LoadTexcoordModelTransform(float x, float y, float scaleX, float scaleY)
        {
            base.LoadVector(loc_texcoordShift, new Vertex2f(x, y));
            base.LoadVector(loc_texcoordScale, new Vertex2f(scaleX, scaleY));
        }

        public void LoadEnableTexture(bool isEnable)
        {
            base.LoadBoolean(loc_enableTexture, isEnable);
        }

        public void LoadModelMatrix()
        {
            Matrix4x4f M = Matrix4x4f.Identity;
            M[0, 0] = 2 * _width;
            M[1, 1] = -2 * _height;
            M[3, 0] = -1.0f + 2 * _x;
            M[3, 1] = 1.0f - 2 * _y;
            base.LoadMatrix(loc_model, M);
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "aPos");
            base.BindAttribute(1, "aTexCoords");
        }

        protected override void GetAllUniformLocations()
        {
            loc_model = base.GetUniformLocation("model");
            loc_color = base.GetUniformLocation("color");
            loc_texcoordShift = base.GetUniformLocation("texcoordShift");
            loc_texcoordScale = base.GetUniformLocation("texcoordScale");
            loc_enableTexture = base.GetUniformLocation("enableTexture");

            loc_horzflip = base.GetUniformLocation("horzflip");
        }

        public void LoadHorizonFlip(bool xflip)
        {
            base.LoadBoolean(loc_horzflip, xflip);
        }

        #region gpu영역

        private void DeleteVertextArray(uint vao, uint vbo)
        {
            Gl.DeleteVertexArrays(new uint[] { (uint)vao });
            Gl.DeleteBuffers(new uint[] { (uint)vbo });
        }

        private RawModel2d LoadToFontVertexArray(float[] positions, float[] texcoords)
        {
            int stride = 4;
            int[] indices = new int[] { 0, 2, 4 };
            int numVertices = positions.Length / 2;
            float[] vertices = new float[numVertices * stride];

            for (int i = 0; i < numVertices; i++)
            {
                vertices[stride * i + 0] = positions[2 * i + 0];
                vertices[stride * i + 1] = positions[2 * i + 1];
                vertices[stride * i + 2] = texcoords[2 * i + 0];
                vertices[stride * i + 3] = texcoords[2 * i + 1];
            }

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            uint vbo = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)(vertices.Length * sizeof(float)), vertices, BufferUsage.StaticDraw);

            Gl.EnableVertexAttribArray(0);
            for (uint i = 0; i < indices.Length - 1; i++)
                Gl.VertexAttribPointer(i, indices[i + 1] - indices[i], VertexAttribType.Float, false, stride * sizeof(float), (System.IntPtr)((indices[i]) * sizeof(float)));
            Gl.BindVertexArray(0);

            Gl.EnableVertexAttribArray(0);

            return new RawModel2d(vao, vbo, numVertices);
        }

        private RawModel2d LoadToFontVertexArray(float[] positions)
        {
            int numVertices = positions.Length / 3;

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            // VBO 생성
            uint vboID = Gl.GenBuffer();

            Gl.BindBuffer(BufferTarget.ArrayBuffer, vboID);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)(positions.Length * sizeof(float)), positions, BufferUsage.StaticDraw);

            Gl.VertexAttribPointer(0, 3, VertexAttribType.Float, false, 0, IntPtr.Zero);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GPUBuffer.Add(vao, vboID);

            Gl.BindVertexArray(0);

            RawModel2d rawModel = new RawModel2d(vao, vboID, numVertices);
            return rawModel;
        }

        #endregion
    }
}
