using System.Collections.Generic;

namespace Ui2d
{
    public class Line
	{
		private double maxLength;
		private double spaceSize;

		private List<Word> words = new List<Word>();
		private double currentLineLength = 0;

		/// <summary>
		/// Creates an empty line.
		/// </summary>
		/// <param name="spaceWidth">the screen-space width of a space character.</param>
		/// <param name="fontSize">the size of font being used.</param>
		/// <param name="maxLength">the screen-space maximum length of a line.</param>
		public Line(double spaceWidth, double fontSize, double maxLength)
		{
			this.spaceSize = spaceWidth * fontSize;
			this.maxLength = maxLength;
		}

		/// <summary>
		/// Attempt to add a word to the line. If the line can fit the word in
		/// without reaching the maximum line length then the word is added and the
		/// line length increased.
		/// </summary>
		/// <param name="word">the word to try to add.</param>
		/// <returns>{@code true} if the word has successfully been added to the line.</returns>
		public bool AttemptToAddWord(Word word)
		{
			double additionalLength = word.WordWidth;
			additionalLength += (words.Count > 0) ? spaceSize : 0;
			if (currentLineLength + additionalLength <= maxLength)
			{
				words.Add(word);
				currentLineLength += additionalLength;
				return true;
			}
			else
			{
				return false;
			}
		}

		public double MaxLength { get { return maxLength; } }

		public double LineLength { get { return currentLineLength; } }

		public List<Word> Words { get { return words; } }

	}
}
