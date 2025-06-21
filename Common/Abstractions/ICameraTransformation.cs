using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Abstractions
{
    public interface ICameraTransformation
    {
        Matrix4x4f ViewMatrix { get; }
        Matrix4x4f ProjectiveMatrix { get; }
        Matrix4x4f ModelMatrix { get; }
        Matrix4x4f MVPMatrix { get; }

        void Yaw(float deltaDegree);
        void Pitch(float deltaDegree);
        void Roll(float deltaDegree);
    }
}
