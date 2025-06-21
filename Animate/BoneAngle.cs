using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animate
{
    public class BoneAngle
    {
        Vertex4f _angle;
        Vertex2f _twist;

        public Vertex4f ConstraintAngle
        {
            get => _angle; set => _angle = value;
        }

        public Vertex2f TwistAngle
        {
            get => _twist; set => _twist = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="theta1">x axis rotation angle</param>
        /// <param name="theta2">x axis rotation angle</param>
        /// <param name="theta3">z axis rotation angle</param>
        /// <param name="theta4">z axis rotation angle</param>
        /// <param name="twistThetaMin">y axis rotation angle</param>
        /// <param name="twistThetaMax">y axis rotation angle</param>
        public BoneAngle(float theta1 = -180.0f, float theta2 = 180.0f, float theta3 = -180.0f, float theta4 = 180.0f, float twistThetaMin = -90.0f, float twistThetaMax = 90.0f)
        {
            _angle = new Vertex4f(theta1, theta2, theta3, theta4);
            _twist = new Vertex2f(twistThetaMin, twistThetaMax);
        }

        public bool IsInboundTwist(float theta) => (_twist.x <= theta && theta <= _twist.y);

    }
}
