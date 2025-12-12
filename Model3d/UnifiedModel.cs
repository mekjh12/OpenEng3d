using Common.Abstractions;
using System.Collections.Generic;
namespace Model3d
{
    /// <summary>
    /// 하나의 통합된 메시로 로드된 모델
    /// </summary>
    public class UnifiedModel
    {
        public uint VaoID { get; set; }
        public int VertexCount { get; set; }
        public int IndexCount { get; set; }
        public List<Texture> Textures { get; set; }  // Texture2DArray로 만들 텍스처 리스트
        public uint TextureArrayID { get; set; }     // 생성된 Texture2DArray ID

        public UnifiedModel()
        {
            Textures = new List<Texture>();
        }
    }
}
