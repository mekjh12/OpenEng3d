using OpenGL;

namespace Model3d
{
    public class WorldCoordinate
    {
        private Entity _worldCoordinateAxis;

        public Entity WorldAxis => _worldCoordinateAxis;

        public WorldCoordinate(float size)
        {
            RawModel3d rawModel3D = Loader3d.LoadPoints(new float[]
                {
                    0, 0, 0, size, 0, 0,  0, 0, 0, -size, 0, 0, // x-axis
                    0, 0, 0, 0, size, 0, 0, 0, 0, 0, -size, 0,  // y-axis
                    0, 0, 0, 0, 0, size, 0, 0, 0, 0, 0, -size   // z-axis
                });
            _worldCoordinateAxis = new Entity("worldCoordinateAxis", "lines", rawModel3D);
        }
    }
}
