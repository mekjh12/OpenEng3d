using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ui2d
{
	public class TextMesh
    {
		float[] vertexPositions;
		float[] textureCoords;
		float lowerBoundX;
		float lowerBoundY;
		float upperBoundX;
		float upperBoundY;

		#region 속성

		public float Width
        {
            get
            {
				return 0.5f * (upperBoundX - lowerBoundX);
			}
        }

		public float Height
		{
			get
			{
				return 0.5f * (upperBoundY - lowerBoundY);
			}
		}

		public float[] VertexPositions
		{
			get
			{
				return vertexPositions;
			}
		}

		public float[] TextureCoords
		{
			get
			{
				return textureCoords;
			}
		}

		public int VertexCount
		{
			get
			{
				return vertexPositions.Length / 2;
			}
		}

		#endregion

		public TextMesh(float[] vertexPositions, float[] textureCoords)
		{
			this.vertexPositions = vertexPositions;
			this.textureCoords = textureCoords;

			this.lowerBoundX = float.MaxValue;
			this.lowerBoundY = float.MaxValue;
			this.upperBoundX = float.MinValue;
			this.upperBoundY = float.MinValue;

			for (int i = 0; i < vertexPositions.Length - 1; i += 2)
			{
				this.lowerBoundX = Math.Min(vertexPositions[i], this.lowerBoundX);
				this.upperBoundX = Math.Max(vertexPositions[i], this.upperBoundX);
				this.lowerBoundY = Math.Min(vertexPositions[i + 1], this.lowerBoundY);
				this.upperBoundY = Math.Max(vertexPositions[i + 1], this.upperBoundY);
			}
		}

		
	}
}
