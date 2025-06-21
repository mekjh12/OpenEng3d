using System.Collections.Generic;

namespace Physics.Collision
{
    /// <summary>
    /// 그리드의 셀
    /// [참고] Broad Phase Collision Detection Using Spatial Partitioning - Build New Games
    /// </summary>
    public class GridCell
    {
        protected List<RigidBody> _bodies;

        /// <summary>
        /// 셀의 강체 갯수
        /// </summary>
        public int Count => _bodies.Count;

        /// <summary>
        /// 생성자
        /// </summary>
        public GridCell()
        {
        }

        /// <summary>
        /// 인덱싱
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public RigidBody this[int index]
        {
            get => _bodies [index];
        }

        /// <summary>
        /// 셀에 강체를 추가한다.
        /// </summary>
        /// <param name="body"></param>
        public void Add(RigidBody body)
        {
            if (_bodies == null)
            {
                _bodies = new List<RigidBody>();
            }

            _bodies.Add(body);
        }

        /// <summary>
        /// 셀에 강체를 제거한다.
        /// </summary>
        /// <param name="body"></param>
        public void Remove(RigidBody body)
        {
            if (_bodies != null)
            {
                _bodies.Remove(body);
            }
        }

        /// <summary>
        /// 셀의 강체를 모두 제거한다.
        /// </summary>
        public void Clear()
        {
            if (_bodies != null)
            {
                _bodies.Clear();
            }
        }
    }
}
