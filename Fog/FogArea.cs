using OpenGL;

namespace Fog
{
    /// <summary>
    ///                  ^  
    ///                  | normal
    ///  --------------------------- Fog Plane
    ///          Fog Range
    /// 
    /// </summary>
    public class FogArea
    {
        Vertex3f _clearColor = new Vertex3f(1, 1, 1);
        float _fogDensity = 0.003f; //0.0001f;

        Vertex4f _fogPlane = new Vertex4f(0.1f, 0.1f, 0.1f, -0); // z방향이고 w가 음수이면 높이는 올라간다.

        public Vertex3f Color
        {
            get => _clearColor;
            set => _clearColor = value;
        }

        public float FogDensity
        {
            get => _fogDensity;
            set => _fogDensity = value;
        }

        public FogArea()
        {

        }
        
        public Vertex4f FogPlane
        {
            get
            {
                Vertex3f n = new Vertex3f(_fogPlane.x, _fogPlane.y, _fogPlane.z);
                n.Normalize();
                return new Vertex4f(n.x, n.y, n.z, _fogPlane.w);
            }
            set => _fogPlane = value;
        }



    }
}
