using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZetaEngine
{
    public class FontFamily
    {
		int textureAtlas;
		TextMeshCreator textMeshCreator;

		#region 속성

		public int TextureAtlas
		{
			get
			{
				return textureAtlas;
			}
		}

		#endregion

		/// <summary>
		/// Creates a new font and loads up the data about each character from the font file.
		/// </summary>
		/// <param name="textureAtlas">the ID of the font atlas texture.</param>
		/// <param name="fontFile">the font file containing information about each character in the texture atlas.</param>
		public FontFamily(int textureAtlas, string fontMetaFile, float aspectRatio)
		{
			this.textureAtlas = textureAtlas;
			this.textMeshCreator = new TextMeshCreator(metaFile: fontMetaFile, aspectRatio);
		}


		/// <summary>
		/// Takes in an unloaded text and calculate all of the vertices for the quads
		/// on which this text will be rendered. The vertex positions and texture
		/// coords and calculated based on the information from the font file.
		/// </summary>
		/// <param name="text">the unloaded text.</param>
		/// <returns>Information about the vertices of all the quads.</returns>
		public TextMesh LoadMesh(Text text, float lineSpace, float maxLineSize)
		{
			return textMeshCreator.CreateTextMesh(text, lineSpace, maxLineSize);
		}
	}
}
