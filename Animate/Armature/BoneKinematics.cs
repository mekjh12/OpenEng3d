using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animate
{
    public class BoneKinematics
    {

        // 회전 제한
        private BoneAngle _restrictAngle;


        /// <summary>
        /// 회전 각도 제한 설정
        /// </summary>
        public BoneAngle RestrictAngle
        {
            get => _restrictAngle;
            set => _restrictAngle = value;
        }


        public BoneKinematics()
        {
            _restrictAngle = new BoneAngle();
        }
    }
}
