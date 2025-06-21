using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Abstractions
{
    public interface ICameraMovement
    {
        void GoForward(float deltaDistance);
        void GoUp(float deltaDistance);
        void GoRight(float deltaDistance);
        void GoTo(Vertex3f position);
    }
}
