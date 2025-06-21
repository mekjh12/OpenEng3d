using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Abstractions
{
    public interface ICameraProperties
    {
        string Name { get; }
        Vertex3f Position { get; set; }
        Vertex3f Forward { get; }
        Vertex3f Up { get; }
        Vertex3f Right { get; }

        float FOV { get; set; }
        float AspectRatio { get; }
        float NEAR { get; set; }
        float FAR { get; set; }
    }
}
