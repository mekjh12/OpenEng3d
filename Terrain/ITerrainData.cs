using OpenGL;
using System.Drawing;
using System.Threading.Tasks;

namespace Terrain
{    
    /// <summary>
    /// 지형의 높이 데이터를 관리하는 인터페이스
    /// </summary>
    public interface ITerrainData
    {
        // 높이맵 속성
        int Width { get; }                   // 높이맵의 너비
        int Height { get; }                  // 높이맵의 높이  
        float Size { get; }                  // 높이맵의 전체 크기

        // 높이값 계산
        Vertex3f GetTerrainHeightVertex3f(Vertex3f position);     // 3D 위치에서의 지형 높이 반환
        float GetTerrainHeight(Vertex3f position,                  // 이중 선형 보간으로 높이값 계산
            float verticalScale = TerrainConstants.DEFAULT_VERTICAL_SCALE);

        // 데이터 로딩
        Task<Bitmap> LoadFromFile(string fileName, int n, int chunkSize);        
    }
}