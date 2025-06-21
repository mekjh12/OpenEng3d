using OpenGL;
using System.Collections.Generic;

namespace Model3d
{
    /// <summary>
    /// 임포스터 렌더링에 필요한 데이터를 담는 클래스
    /// </summary>
    public class ImpostorData
    {
        private Texture _atlasTexture;            // 생성된 아틀라스 텍스처
        private Vertex2f _frameSize;               // 각 뷰의 UV 크기
        private Vertex2f _gridSize;                // 그리드 크기 (가로/세로 뷰 개수)
        private List<Vertex2f> _angleData;         // 각 뷰의 각도 데이터

        /// <summary>
        /// 생성된 아틀라스 텍스처
        /// </summary>
        public Texture AtlasTexture
        {
            get => _atlasTexture;
            set => _atlasTexture = value;
        }

        /// <summary>
        /// 각 뷰의 UV 크기
        /// </summary>
        public Vertex2f FrameSize
        {
            get => _frameSize;
            set => _frameSize = value;
        }

        /// <summary>
        /// 그리드 크기 (가로/세로 뷰 개수)
        /// </summary>
        public Vertex2f GridSize
        {
            get => _gridSize;
            set => _gridSize = value;
        }

        /// <summary>
        /// 각 뷰의 각도 데이터
        /// </summary>
        public List<Vertex2f> AngleData
        {
            get => _angleData;
            set => _angleData = value;
        }
    }
}
