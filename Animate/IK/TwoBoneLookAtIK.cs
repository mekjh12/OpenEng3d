using OpenGL;

namespace Animate
{
    public class TwoBoneLookAtIK
    {
        float _firstWeight = 0.3f;
        float _secondWeight = 0.7f;
        Bone _firstBone;
        Bone _secondBone;
        SingleBoneLookAt _firstLookAt;
        SingleBoneLookAt _secondLookAt;

        public SingleBoneLookAt FirstLookAt { get => _firstLookAt; }
        public SingleBoneLookAt SecondLookAt { get => _secondLookAt; }

        public TwoBoneLookAtIK(Bone firstBone, Bone secondBone,
            float firstWeight = 0.6f, float secondWeight = 0.4f,
            LocalSpaceAxis forward = LocalSpaceAxis.Z, LocalSpaceAxis up = LocalSpaceAxis.Y)
        {
            _firstWeight = firstWeight;
            _secondWeight = secondWeight;
            _firstBone = firstBone;
            _secondBone = secondBone;

            _firstLookAt = new SingleBoneLookAt(_firstBone, forward, up);
            _secondLookAt = new SingleBoneLookAt(_secondBone, forward, up);
        }

        public void LookAt(Vertex3f targetPosition, Matrix4x4f modelMatrix, Animator animator)
        {
            ZetaExt.Quaternion rotation;
            _secondLookAt.Calculate(targetPosition, modelMatrix, animator);
            rotation = ZetaExt.Quaternion.Identity.Interpolate(_secondLookAt.Rotation, _secondWeight);
            _secondLookAt.Rotate((Matrix4x4f)rotation, animator, true);

            _firstLookAt.Solve(targetPosition, modelMatrix, animator);
        }
    }
}
