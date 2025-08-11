using OpenGL;
using System.Collections.Generic;

namespace Animate
{
    public interface Motionable
    {
        // 재생 관련
        string Name { get; }
        bool InterpolatePoseAtTime(float motionTime, ref Dictionary<string, Matrix4x4f> outPose);
        KeyFrame CloneKeyFrame(float time);
        float PeriodTime { get; }

        // 이동 관련
        float Speed { get; }
        FootStepAnalyzer.MovementType MovementType { get; }
    }
}
