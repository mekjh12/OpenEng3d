using OpenGL;
using System;
using System.Windows.Forms;
using System.IO;
using Shader;
using Common;

namespace Ui2d
{
    /// <summary>
    /// 사각형의 2d상자를 렌더링해 준다.
    /// 텍스처에 직사각형 모양을 렌더링한다.
    /// </summary>
    public class UIColorShader : ShaderProgram<Enum>
    {
        const string VERTEX_FILE = @"\uiColor2d.vert";
        const string FRAGMENT_FILE = @"\uiColor2d.frag";

        //
        //   (0,0) --- (1,0)               (-1,1)  --- (1,1)
        //     |   \     |        --->       |      /    |   
        //     |     \   |                   |   /       |   
        //   (0,1) --- (1,1)               (-1,-1) --- (1,-1)
        //
        float[] _quadVertices = { 0, 0, 0,  1, 1, 0,  1, 0, 0,  0, 0, 0,  0, 1, 0,  1, 1, 0 };

        private float _x;
        private float _y;
        private float _z;
        private float _width;
        private float _height;

        private RawModel2d _rawModel;
        
        private int loc_model;
        private int loc_color;

        public uint VAO => _rawModel.VAO;


        public void SetView(float x, float y, float z, float width, float height)
        {
            _x = x;
            _y = y;
            _z = z;
            _width = width;
            _height = height;
        }

        public UIColorShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;
            InitCompileShader();
            _rawModel = LoadToFontVertexArray(_quadVertices);
        }

        public void LoadColor(Vertex4f color)
        {
            base.LoadVector(loc_color, color);
        }

        public void LoadModelMatrix()
        {
            // [0,1]--[0, 2]--[-1,1]
            Matrix4x4f M = Matrix4x4f.Identity;
            M[0, 0] = 2 * _width;
            M[1, 1] = -2 * _height;
            M[3, 0] = -1.0f + 2 * _x;
            M[3, 1] = 1.0f - 2 * _y;
            M[3, 2] = _z;
            base.LoadMatrix(loc_model, M);
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "aPos");
        }

        protected override void GetAllUniformLocations()
        {
            loc_model = base.GetUniformLocation("model");
            loc_color = base.GetUniformLocation("color");
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
    }
}
