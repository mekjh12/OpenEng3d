using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics
{
    public interface IRigidBody
    {
        /// <summary>
        /// 강체의 모양에 따라 관성텐서를 지정한다.
        /// </summary>
        void InitInertiaTensor();

        /// <summary>
        /// 강체의 모양에 따라 물질의 상태에 따라 질량을 계산한다.
        /// </summary>
        void CalculateMass();

        /// <summary>
        /// 강체의 모양에 따라 AABB 바운딩볼륨을 계산한다.
        /// </summary>
        void CalculateBoundingVolume();
    }
}
