using OpenGL;
using System.Collections.Generic;

namespace Animate
{
    public interface Motionable
    {
        // 재생 관련
        string Name { get; }

        float PeriodTime { get; }

        // 이동 관련
        float Speed { get; }
        FootStepAnalyzer.MovementType MovementType { get; }

        Bone RootBone { get; }

        /// <summary>
        /// 매 프레임마다 모션을 업데이트합니다. 속도와 메모리에 유의하세요.
        /// </summary>
        /// <param name="motionTime"></param>
        /// <param name="outPose"></param>
        /// <returns></returns>
        bool InterpolatePoseAtTime(float motionTime, ref Dictionary<string, Matrix4x4f> outPose, Bone searchStartBone = null);

        /// <summary>
        /// 모션과 모션을 이어주기 위해서 프레임 복제가 필요합니다.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        KeyFrame CloneKeyFrame(float time);
    }
}
