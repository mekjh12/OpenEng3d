using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ui2d
{
	public class TextMeshCreator
    {
		public static double LINE_HEIGHT = 0.03f;

		public static int SPACE_ASCII = 32;

		private MetaFile metaData;

		public TextMeshCreator(string metaFile, float aspectRatio)
		{
			metaData = new MetaFile(metaFile, aspectRatio);
		}

		public void ReLoad(string metaFile, float aspectRatio)
        {
			metaData = new MetaFile(metaFile, aspectRatio);
		}

		public TextMesh CreateTextMesh(Text text, float lineSpace, float maxLineSize)
		{
			List<Line> lines = CreateStructure(text, maxLineSize);
			TextMesh data = CreateQuadVertices(text, lines, lineSpace);
			return data;
		}

		private List<Line> CreateStructure(Text text, float maxLineSize)
		{
			char[] chars = text.TextContent.ToCharArray();
			List<Line> lines = new List<Line>();
			Line currentLine = new Line(metaData.SpaceWidth, text.FontSize, maxLineSize);
			Word currentWord = new Word(text.FontSize);
            foreach (char c in chars)
            {
				int ascii = (int)c;
				if (ascii == SPACE_ASCII)
				{
					if (currentWord.Text == "<br>")
					{
						lines.Add(currentLine);
						currentWord.Clear();
						currentLine = new Line(metaData.SpaceWidth, text.FontSize, maxLineSize);
					}
                    else
                    {
						bool added = currentLine.AttemptToAddWord(currentWord);
						if (!added)
						{
							lines.Add(currentLine);
							currentLine = new Line(metaData.SpaceWidth, text.FontSize, maxLineSize);
							currentLine.AttemptToAddWord(currentWord);
						}
						currentWord = new Word(text.FontSize);
						continue;
					}
				}

				if (ascii != SPACE_ASCII)
                {
					Character character = metaData.GetCharacter(ascii);
					currentWord.AddCharacter(character);
				}
			}

			CompleteStructure(lines, currentLine, currentWord, text, maxLineSize);
			return lines;
		}

		private void CompleteStructure(List<Line> lines, Line currentLine, Word currentWord, Text text, float maxLineSize)
		{
			bool added = currentLine.AttemptToAddWord(currentWord);
			if (!added)
			{
				lines.Add(currentLine);
				currentLine = new Line(metaData.SpaceWidth, text.FontSize, maxLineSize);
				currentLine.AttemptToAddWord(currentWord);
			}
			lines.Add(currentLine);
		}

		private TextMesh CreateQuadVertices(Text text, List<Line> lines, float lineSpace)
		{
			text.NumberOfLines = lines.Count;
			double curserX = 0f;
			double curserY = 0f;
			List<float> vertices = new List<float>();
			List<float> textureCoords = new List<float>();

			foreach (Line line in lines)
			{
				//if (text.IsCentered)
				{
					//curserX = (line.MaxLength - line.LineLength) * 0.5f;
				}

				foreach (Word word in line.Words)
				{
					foreach (Character letter in word.Characters)
					{
						this.AddVerticesForCharacter(curserX, curserY, letter, text.FontSize, vertices);
						TextMeshCreator.AddTexCoords(textureCoords, letter.TextureCoordX, letter.TextureCoordY,
								letter.MaxTextureCoordX, letter.MaxTextureCoordY);
						curserX += letter.Advance * text.FontSize;
					}
					curserX += metaData.SpaceWidth * text.FontSize;
				}
				curserX = 0;
				curserY += lineSpace * LINE_HEIGHT * text.FontSize;
			}
			return new TextMesh(ListToArray(vertices), ListToArray(textureCoords));
		}

		private void AddVerticesForCharacter(double curserX, double curserY, Character character, double fontSize,
				List<float> vertices)
		{
            float aspect = UIEngine.Aspect;
			double y = curserY + (character.OffsetY * fontSize);
            double x = curserX + (character.OffsetX * fontSize);
            double maxY = y + (character.SizeY * fontSize);
            double maxX = x + (character.SizeX * fontSize);
			double properX = (2 * x) - 1;
			double properY = (-2 * y) + 1;
			double properMaxX = (2 * maxX) - 1;
			double properMaxY = (-2 * maxY) + 1;
			TextMeshCreator.AddVertices(vertices, properX, properY, properMaxX, properMaxY);
		}

		private static void AddVertices(List<float> vertices, double x, double y, double maxX, double maxY)
		{
            vertices.Add((float)x);
            vertices.Add((float)y);
            vertices.Add((float)x);
            vertices.Add((float)maxY);
            vertices.Add((float)maxX);
            vertices.Add((float)maxY);
            vertices.Add((float)maxX);
            vertices.Add((float)maxY);
            vertices.Add((float)maxX);
            vertices.Add((float)y);
            vertices.Add((float)x);
            vertices.Add((float)y);
            /*
			 vertices.Add((float)x);
            vertices.Add((float)y);
            vertices.Add((float)maxX);
            vertices.Add((float)y);
            vertices.Add((float)maxX);
            vertices.Add((float)maxY);
            vertices.Add((float)maxX);
            vertices.Add((float)maxY);
            vertices.Add((float)x);
            vertices.Add((float)maxY);
            vertices.Add((float)x);
            vertices.Add((float)y);
			*/


        }

        private static void AddTexCoords(List<float> texCoords, double x, double y, double maxX, double maxY)
		{
            texCoords.Add((float)x);
            texCoords.Add((float)y);
            texCoords.Add((float)x);
            texCoords.Add((float)maxY);
            texCoords.Add((float)maxX);
            texCoords.Add((float)maxY);
            texCoords.Add((float)maxX);
            texCoords.Add((float)maxY);
            texCoords.Add((float)maxX);
            texCoords.Add((float)y);
            texCoords.Add((float)x);
            texCoords.Add((float)y);
            /*
			 texCoords.Add((float)x);
            texCoords.Add((float)y);
            texCoords.Add((float)maxX);
            texCoords.Add((float)y);
            texCoords.Add((float)maxX);
            texCoords.Add((float)maxY);
            texCoords.Add((float)maxX);
            texCoords.Add((float)maxY);
            texCoords.Add((float)x);
            texCoords.Add((float)maxY);
            texCoords.Add((float)x);
            texCoords.Add((float)y);
			*/


        }


        private static float[] ListToArray(List<float> listOfFloats)
		{
			float[] array = new float[listOfFloats.Count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = listOfFloats[i];
			}
			return array;
		}
	}
}
