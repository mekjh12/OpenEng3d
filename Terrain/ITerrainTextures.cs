using Model3d;
using System.Threading.Tasks;

namespace Terrain
{
    /// <summary>
    /// 지형 텍스처 관리를 위한 인터페이스
    /// </summary>
    public interface ITerrainTextures
    {
        Texture HeightMapTexture { get; }     // 높이맵 텍스처

        // 텍스처 로딩
        void LoadHeightMap(string heightMapPath);          // 높이맵 로드
    }
}