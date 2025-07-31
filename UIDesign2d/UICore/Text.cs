using OpenGL;
using System;

namespace Ui2d
{
	public class Text
    {
		string _name;

		Vertex4f _color = new Vertex4f(1f, 1f, 0f, 1.0f);
		string _textContent;
		float _fontSize;
		FontFamily _fontFamily;
		int _numberOfLines;

        float _width = 0.0f;
		float _height = 0.0f;

		TextVAO _textMeshVAO;
        TextMesh _textMesh;

        #region 속성

        public int NumberOfLines
		{
			get =>_numberOfLines;
			set => _numberOfLines = value;
		}

		/// <summary>
		/// 글자의 절대 너비
		/// </summary>
        public float Width => _width;

        /// <summary>
        /// 글자의 절대 높이
        /// </summary>
        public float Height => _height;       

		public string Name => _name; //TEXT의 ID를 관리하기 위한 Name
		
		public FontFamily FontFamily
		{
			get => _fontFamily;
			set => _fontFamily = value;
        }

		public Vertex4f Color
		{
			get=> _color;
			set=> _color = value;
		}

		public TextVAO Mesh => _textMeshVAO;

		public int VertexCount=> _textMeshVAO.VerticesCount;

		public float FontSize
		{
			get => _fontSize;
            set=> _fontSize = value;
		}

		public string TextContent
		{
			get => _textContent;
            set=> _textContent = value;
		}

		#endregion

		public Text(string name, string text, float fontSize)
		{
			_name = name;
			_textContent = text;
			_fontSize = fontSize;
		}

        /// <summary>
        /// 텍스트를 읽어와 VAO, Width, Height를 설정한다.
        /// </summary>
        /// <param name="txt"></param>
        /// <param name="lineSpacing"></param>
        /// <param name="maxLineWidth"></param>
        /// <returns></returns>
        public bool SetText(string txt, float lineSpacing, float maxLineWidth)
		{
			bool isOverWrited = false;
			if (_textMeshVAO != null)
			{
				this.DeleteVertextArray(_textMeshVAO.VAO, _textMeshVAO.VBO);
                isOverWrited = true;
			}

            _textContent = txt.Replace("<br>", " <br>");
			
			// 텍스트의 VAO, Width, Height를 설정한다.
            _textMesh = _fontFamily.LoadMesh(this, lineSpacing, maxLineWidth);
            _textMeshVAO = LoadToFontVertexArray(_textMesh.VertexPositions, _textMesh.TextureCoords);
			_height = _textMesh.Height;
            _width = _textMesh.Width; //_height * UIEngine.Aspect; //

            return isOverWrited;
		}

		#region gpu영역

		private void DeleteVertextArray(uint vao, uint vbo)
		{
			Gl.DeleteVertexArrays(new uint[] { (uint)vao });
			Gl.DeleteBuffers(new uint[] { (uint)vbo });
		}

		private TextVAO LoadToFontVertexArray(float[] positions, float[] texcoords)
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

			return new TextVAO(vao, vbo, numVertices);
		}
		#endregion
	}
}
