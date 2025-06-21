namespace Ui2d
{
    public class Character
    {
		private char character;
		private int id;
		private double xTextureCoord;
		private double yTextureCoord;
		private double xMaxTextureCoord;
		private double yMaxTextureCoord;
		private double xOffset;
		private double yOffset;
		private double sizeX;
		private double sizeY;
		private double xAdvance;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id">the ASCII value of the character.</param>
		/// <param name="xTextureCoord">the x texture coordinate for the top left corner of the character in the texture atlas.</param>
		/// <param name="yTextureCoord">the y texture coordinate for the top left corner of the character in the texture atlas.</param>
		/// <param name="xTexSize">the width of the character in the texture atlas.</param>
		/// <param name="yTexSize">the height of the character in the texture atlas.</param>
		/// <param name="xOffset">the x distance from the curser to the left edge of the character's quad.</param>
		/// <param name="yOffset">the y distance from the curser to the top edge of the character's quad.</param>
		/// <param name="sizeX">the width of the character's quad in screen space.</param>
		/// <param name="sizeY">the height of the character's quad in screen space.</param>
		/// <param name="xAdvance">how far in pixels the cursor should advance after adding  this character.</param>
		public Character(int id, double xTextureCoord, double yTextureCoord, double xTexSize, double yTexSize,
				double xOffset, double yOffset, double sizeX, double sizeY, double xAdvance)
		{
			this.character = (char)id;
			this.id = id;
			this.xTextureCoord = xTextureCoord;
			this.yTextureCoord = yTextureCoord;
			this.xOffset = xOffset;
			this.yOffset = yOffset;
			this.sizeX = sizeX;
			this.sizeY = sizeY;
			this.xMaxTextureCoord = xTexSize + xTextureCoord;
			this.yMaxTextureCoord = yTexSize + yTextureCoord;
			this.xAdvance = xAdvance;
		}

		public string Char
		{
            get
            {
				return character.ToString();
			}
		}

		public int ID { get { return id; } }

		public double TextureCoordX { get { return xTextureCoord; } }

		public double TextureCoordY { get { return yTextureCoord; } }

		public double MaxTextureCoordX { get { return xMaxTextureCoord; } }

		public double MaxTextureCoordY { get { return yMaxTextureCoord; } }

		public double OffsetX { get { return xOffset; } }

		public double OffsetY { get { return yOffset; } }

		public double SizeX { get { return sizeX; } }

		public double SizeY { get { return sizeY; } }

		public double Advance { get { return xAdvance; } }

	}
}
