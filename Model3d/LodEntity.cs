using Common.Abstractions;
using OpenGL;
using System.Collections.Generic;

namespace Model3d
{
    public class LodEntity : Entity, ILodable
    {
        LODComponent _lodComponent;
        readonly List<BaseModel3d> _lowPolyModels;

        public LodEntity(string entityName, string modelName, BaseModel3d[] rawModels, BaseModel3d[] lowPolyModels, float[] lodDistances) : base(entityName, modelName, rawModels)
        {
            _lowPolyModels = new List<BaseModel3d>();
            _lowPolyModels.AddRange(lowPolyModels);
            _lodComponent = new LODComponent(lodDistances[0], lodDistances[1]);
        }

        public int CurrentLod => _lodComponent.CurrentLod;

        public bool ShouldUseImpostor => _lodComponent.ShouldUseImpostor;

        public override void Update(Camera camera)
        {
            base.Update(camera);
            Update(Position, camera.Position);
        }

        public void Update(Vertex3f position, Vertex3f cameraPosition)
        {
            ((ILodable)_lodComponent).Update(position, cameraPosition);
        }

        public override List<BaseModel3d> Models
        {
            get => CurrentLod == 1 ? _lowPolyModels : base.Models;
        }

        public float DistanceLodLow => ((ILodable)_lodComponent).DistanceLodLow;

        public float DistanceLodHigh => ((ILodable)_lodComponent).DistanceLodHigh;
    }
}
