using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZetaEngine
{
	public class MetaFile
    {
		private static int PAD_TOP = 0;
		private static int PAD_LEFT = 1;
		private static int PAD_BOTTOM = 2;
		private static int PAD_RIGHT = 3;

		private static int DESIRED_PADDING = 3;

		private static char[] SPLITTER = new char[] { ' ' };
		private static char[] NUMBER_SEPARATOR = new char[] { ',' };

		private double aspectRatio;

		private double verticalPerPixelSize;
		private double horizontalPerPixelSize;
		private double spaceWidth;
		private int[] padding;
		private int paddingWidth;
		private int paddingHeight;

		private Dictionary<int, Character> metaData = new Dictionary<int, Character>();

		private StreamReader reader;
		private Dictionary<string, string> values = new Dictionary<string, string>();

		public double SpaceWidth
		{
			get
			{
				return spaceWidth;
			}
		}

		/// <summary>
		/// Opens a font file in preparation for reading.
		/// </summary>
		/// <param name="file">the font file.</param>
		/// <param name="aspectRatio">aspectRatio</param>
		public MetaFile(string file, float aspectRatio)
		{
			this.aspectRatio = aspectRatio;
			this.OpenFile(file);
			this.LoadPaddingData();
			this.LoadLineSizes();
			int imageWidth = this.GetValueOfVariable("scaleW");
			this.LoadCharacterData(imageWidth);
			this.Close();
		}


		public Character GetCharacter(int ascii) 
		{
			if (!metaData.ContainsKey(ascii)) return null;
			return metaData[ascii];
		}

		/// <summary>
		/// Read in the next line and store the variable values.
		/// </summary>
		/// <returns>{@code true} if the end of the file hasn't been reached.</returns>
		private bool processNextLine()
		{
			values.Clear();
			String line = null;
			try
			{
				line = reader.ReadLine();
			}
			catch
			{

			}

			if (line == null) return false;

			foreach (string part in line.Split(SPLITTER))
            {
				string[] valuePairs = part.Split(new char[] { '=' });
				if (valuePairs.Length == 2)
				{
					values[valuePairs[0]] = valuePairs[1];
				}
			}

			return true;
		}

		/// <summary>
		/// Gets the {@code int} value of the variable with a certain name on thecurrent line.
		/// </summary>
		/// <param name="variable"> the name of the variable.</param>
		/// <returns>The value of the variable.</returns>
		private int GetValueOfVariable(string variable)
		{
			return int.Parse(values[variable]);
		}

		/// <summary>
		/// Gets the array of ints associated with a variable on the current line.
		/// </summary>
		/// <param name="variable">the name of the variable.</param>
		/// <returns>The int array of values associated with the variable.</returns>
		private int[] GetValuesOfVariable(String variable)
		{
			string[] numbers = values[variable].Split(NUMBER_SEPARATOR);
			int[] actualValues = new int[numbers.Length];
			for (int i = 0; i < actualValues.Length; i++)
			{
				actualValues[i] = int.Parse(numbers[i]);
			}
			return actualValues;
		}

		/// <summary>
		/// Closes the font file after finishing reading.
		/// </summary>
		private void Close()
		{
			try
			{
				reader.Close();
			}
			catch (IOException e)
			{
                Console.WriteLine(e.Message);
			}
		}

		/// <summary>
		/// Opens the font file, ready for reading.
		/// </summary>
		/// <param name="file"></param>
		private void OpenFile(string file)
		{
			if (!File.Exists(file))
            {
				new Exception("");
            }

			try
			{
				reader = new StreamReader(file);
			}
			catch
			{
                Console.WriteLine("Couldn't read font meta file!");
			}
		}

		/// <summary>
		/// Loads the data about how much padding is used around each character in the texture atlas.
		/// </summary>
		private void LoadPaddingData()
		{
			processNextLine();
			this.padding = GetValuesOfVariable("padding");
			this.paddingWidth = padding[PAD_LEFT] + padding[PAD_RIGHT];
			this.paddingHeight = padding[PAD_TOP] + padding[PAD_BOTTOM];
		}

		/**
		 * Loads information about the line height for this font in pixels, and uses
		 * this as a way to find the conversion rate between pixels in the texture
		 * atlas and screen-space.
		 */
		private void LoadLineSizes()
		{
			processNextLine();
			int lineHeightPixels = this.GetValueOfVariable("lineHeight") - paddingHeight;
			verticalPerPixelSize = TextMeshCreator.LINE_HEIGHT / (double)lineHeightPixels;
			horizontalPerPixelSize = verticalPerPixelSize / aspectRatio;
		}

		/// <summary>
		/// Loads in data about each character and stores the data in the {@link Character} class.
		/// </summary>
		/// <param name="imageWidth">the width of the texture atlas in pixels.</param>
		private void LoadCharacterData(int imageWidth)
		{
			processNextLine();
			processNextLine();
			while (processNextLine())
			{
				Character c = this.LoadCharacter(imageWidth);
				if (c != null)
				{
					metaData[c.ID] = c;
				}
			}
		}

		/// <summary>
		/// Loads all the data about one character in the texture atlas and converts
		/// it all from 'pixels' to 'screen-space' before storing. The effects of
		/// padding are also removed from the data.
		/// </summary>
		/// <param name="imageSize">the size of the texture atlas in pixels.</param>
		/// <returns>The data about the character.</returns>
		private Character LoadCharacter(int imageSize)
		{
			int id = this.GetValueOfVariable("id");
			if (id == TextMeshCreator.SPACE_ASCII)
			{
				this.spaceWidth = (this.GetValueOfVariable("xadvance") - paddingWidth) * horizontalPerPixelSize;
				return null;
			}
			double xTex = ((double)GetValueOfVariable("x") + (padding[PAD_LEFT] - DESIRED_PADDING)) / imageSize;
			double yTex = ((double)GetValueOfVariable("y") + (padding[PAD_TOP] - DESIRED_PADDING)) / imageSize;
			int width = GetValueOfVariable("width") - (paddingWidth - (2 * DESIRED_PADDING));
			int height = GetValueOfVariable("height") - ((paddingHeight) - (2 * DESIRED_PADDING));
			double quadWidth = width * horizontalPerPixelSize;
			double quadHeight = height * verticalPerPixelSize;
			double xTexSize = (double)width / imageSize;
			double yTexSize = (double)height / imageSize;
			double xOff = (GetValueOfVariable("xoffset") + padding[PAD_LEFT] - DESIRED_PADDING) * horizontalPerPixelSize;
			double yOff = (GetValueOfVariable("yoffset") + (padding[PAD_TOP] - DESIRED_PADDING)) * verticalPerPixelSize;
			double xAdvance = (GetValueOfVariable("xadvance") - paddingWidth) * horizontalPerPixelSize;
			return new Character(id, xTex, yTex, xTexSize, yTexSize, xOff, yOff, quadWidth, quadHeight, xAdvance);
		}
	}
}
