using OpenGL;
using System.Collections.Generic;

namespace Animate
{
    public interface Motionable
    {
        bool InterpolatePoseAtTime(float motionTime, ref Dictionary<string, Matrix4x4f> outPose);

        KeyFrame CloneKeyFrame(float time);

        string Name { get; }

        float Length { get; }

    }
}
