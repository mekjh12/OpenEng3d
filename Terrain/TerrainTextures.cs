using Model3d;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace Terrain
{
    /// <summary>
    /// 지형 시스템에서 사용되는 모든 텍스처 리소스를 관리하는 클래스입니다.
    /// 높이맵, 기본 텍스처, 디테일맵 등의 텍스처 로딩과 관리를 담당합니다.
    /// </summary>
    public class TerrainTextures : ITerrainTextures
    {
        private Texture _heightMapTexture;     // 지형의 높이값을 정의하는 높이맵 텍스처 (R채널 사용)
        public Texture HeightMapTexture => _heightMapTexture;

        /// <summary>
        /// TerrainTextures 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <remarks>
        /// 텍스처 배열은 총 5개의 슬롯으로 초기화됩니다:
        /// - 슬롯 0: 기본 베이스 텍스처
        /// - 슬롯 1-4: 블렌딩될 레이어 텍스처
        /// 디테일맵은 초기에 비활성화됩니다.
        /// </remarks>
        public TerrainTextures()
        {
            
        }

        /// <summary>
        /// 높이맵 텍스처를 로드합니다.
        /// </summary>
        /// <param name="heightMapPath">높이맵 이미지 파일 경로</param>
        /// <remarks>
        /// 높이맵은 지형의 높이값을 정의하며, 이미지의 R채널이 높이값으로 사용됩니다.
        /// 흰색(1.0)이 최대 높이, 검은색(0.0)이 최소 높이를 나타냅니다.
        /// </remarks>
        public void LoadHeightMap(string heightMapPath)
        {
            _heightMapTexture = new Texture(heightMapPath);
        }

        public void LoadHeightMap(Bitmap bitmap)
        {
            _heightMapTexture = new Texture(bitmap);
        }

        public void LoadHeightMap(BitmapData bitmapData)
        {
            _heightMapTexture = new Texture(bitmapData);
        }
    }
}