using OpenGL;
using System;
using ZetaExt;

namespace Common.Abstractions
{
    /// <summary>
    /// AABB, OBB의 추상클래스
    /// BaseEntity를 가진다.
    /// </summary>
    public abstract class BoundVolume: IBoundVolumable, IBoundVolumeCostable
    {
        protected bool _useEnhanceBox;
        protected Vertex3f _color;
        protected BaseEntity _baseEntity;
        bool _isVisible;
        string _name;

        /// <summary>
        /// 느슨한 바운딩 박스의 사용 유무
        /// </summary>
        public bool UseEnhanceBox
        {
            get => _useEnhanceBox;
            set => _useEnhanceBox = value;
        }

        /// <summary>
        /// 렌더링 색상
        /// </summary>
        public Vertex3f Color
        {
            get => _color;
            set => _color = value;
        }

        /// <summary>
        /// 바운딩볼륨에 포함된 BaseEntity
        /// </summary>
        public BaseEntity BaseEntity
        {
            get => _baseEntity;
            set => _baseEntity = value;
        }

        /// <summary>
        /// 볼륨영역을 보이는지 유무를 반환한다.
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible; 
            set => _isVisible = value;
        }

        public abstract float Area { get; }
        public abstract Vertex3f Center { get; set; }
        public abstract Matrix4x4f ModelMatrix { get; }
        public abstract Vertex3f Size { get; set; }
        public abstract Vertex3f[] Vertices { get; }
        public string Name { get => _name; set => _name = value; }

        /// <summary>
        /// 깊은 복사를 한다.
        /// </summary>
        /// <returns></returns>
        public abstract BoundVolume Clone();

        /// <summary>
        /// BoundVolume가 NDC 공간에 투영되는 정규화된 면적을 계산합니다.
        /// 예: 1은 전체 화면의 1, 0.5는 전체 화면의 1/2을 덮는 경우
        /// </summary>
        /// <param name="vpMatrix">VP행렬</param>
        /// <returns>NDC 공간에 투영된 정규화된 면적 (0~1 범위)</returns>
        public virtual float CalculateScreenSpaceArea(Matrix4x4f vpMatrix)
        {
            Vertex3f[] corners = this.Vertices;
            Vertex2f[] ndcPoints = new Vertex2f[8];
            int validPoints = 0;

            // NDC 변환
            for (int i = 0; i < 8; i++)
            {
                Vertex4f clipSpace = vpMatrix * corners[i].Vertex4f();
                if (Math.Abs(clipSpace.w) > float.Epsilon)  // 0으로 나누기 방지
                {
                    float ndcX = clipSpace.x / clipSpace.w;
                    float ndcY = clipSpace.y / clipSpace.w;
                    // NDC 좌표를 -1~1 범위로 클램핑
                    ndcPoints[validPoints++] = new Vertex2f(
                        ndcX.Clamp(-1.0f, 1.0f),
                        ndcY.Clamp(-1.0f, 1.0f)
                    );
                }
            }

            // 유효한 점이 없는 경우
            if (validPoints == 0)
                return 0;

            // 경계 상자 계산
            float minX = ndcPoints[0].x;
            float minY = ndcPoints[0].y;
            float maxX = minX;
            float maxY = minY;

            for (int i = 1; i < validPoints; i++)
            {
                minX = Math.Min(minX, ndcPoints[i].x);
                minY = Math.Min(minY, ndcPoints[i].y);
                maxX = Math.Max(maxX, ndcPoints[i].x);
                maxY = Math.Max(maxY, ndcPoints[i].y);
            }

            float width = maxX - minX;
            float height = maxY - minY;

            return Math.Max(0, width * height * 0.25f);
        }

        public abstract BoundVolume Union(BoundVolume b);
        public abstract BoundVolume Intersect(BoundVolume boundVoulume);
    }
}
