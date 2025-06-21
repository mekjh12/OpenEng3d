using Common.Abstractions;
using OpenGL;

namespace Model3d
{
    // LOD 관련 인터페이스
    public interface ILodable
    {
        int CurrentLod{ get; }

        bool ShouldUseImpostor { get; }

        void Update(Vertex3f position, Vertex3f cameraPosition);

        float DistanceLodLow { get; }
        float DistanceLodHigh { get; }

    }

}
