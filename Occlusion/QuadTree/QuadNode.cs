using Geometry;
using System.Collections.Generic;

namespace Occlusion
{
    /// <summary>
    /// 쿼드트리 노드
    /// </summary>
    public class QuadNode
    {
        // 노드의 공간 영역
        public AABB3f AABB;

        // 자식 노드 (4개)
        // [0] = 남서(SW), [1] = 남동(SE), [2] = 북서(NW), [3] = 북동(NE)
        public QuadNode[] Children;

        public bool[] IsLinked;

        // 이 노드에 속한 객체들
        public List<TreeObject> Objects;

        // 경계에 걸친 객체들 (자식 노드들과 공유)
        public List<TreeObject> BoundaryObjects;

        // 노드 정보
        public int Depth;
        public bool IsLeaf => Children == null;

        public QuadNode(AABB3f aabb, int depth)
        {
            AABB = aabb;
            Depth = depth;
            Objects = new List<TreeObject>();
            BoundaryObjects = new List<TreeObject>();
        }

        public void SetVisible(bool visible)
        {
            if (IsLinked == null)
            {
                IsLinked = new bool[4] { false, false, false, false };
            }

            for (int i = 0; i < 4; i++)
            {
                IsLinked[i] = visible;
            }
        }
    }
}
