using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ui2d
{
	public class Word
    {
		private string word;
		private List<Character> characters = new List<Character>();
		private double width = 0;
		private double fontSize;

        public double FontSize
        {
            get => fontSize;
            set => fontSize = value;
        }

		public List<Character> Characters { get { return characters; } }

		public double WordWidth { get { return width; } }

		public string Text
        {
            get
            {
				return this.word;
            }
        }

		/// <summary>
		/// 단어를 만듦
		/// </summary>
		/// <param name="fontSize">단어의 폰트크기</param>
		public Word(double fontSize)
		{
			this.fontSize = fontSize;
		}

		/// <summary>
		/// 
		/// Adds a character to the end of the current word and increases the screen-space width of the word.
		/// </summary>
		/// <param name="character">the character to be added.</param>
		public void AddCharacter(Character character)
		{
			if (character == null) return;

			this.word += character.Char;

			characters.Add(character);
			width += character.Advance * fontSize;
		}

		public void Clear()
        {
			this.word = "";
			characters.Clear();
			width = 0;
		}

	}
}
