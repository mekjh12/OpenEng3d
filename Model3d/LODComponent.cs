using Camera3d;
using Common.Abstractions;
using OpenGL;
using ZetaExt;

namespace Model3d
{
    public class LODComponent : ILodable
    {
        private readonly float[] DISTANCE_LOD;
        Vertex3f _position;
        Vertex3f _cameraPosition;
        int _lod;

        public LODComponent(float nearDistance, float farDistance)
        {
            DISTANCE_LOD = new float[] { nearDistance, farDistance };
        }

        public int CurrentLod => _lod;

        public bool ShouldUseImpostor => CurrentLod == 2;

        public float DistanceLodLow => DISTANCE_LOD[DISTANCE_LOD.Length - 1];

        public float DistanceLodHigh => (DISTANCE_LOD.Length > 0) ? float.MaxValue : DISTANCE_LOD[0];

        public void Update(Vertex3f position, Vertex3f cameraPosition)
        {
            _position = position;
            _cameraPosition = cameraPosition;

            float distance = (cameraPosition - position).Length();
            for (int i = 0; i < DISTANCE_LOD.Length; i++)
            {
                if (DISTANCE_LOD[i] > distance)
                {
                    _lod = i;
                    return;
                }
            }
            _lod = DISTANCE_LOD.Length;
        }
    }
}
