using OpenGL;

namespace Animate
{

    public class ThreeBoneLookAtIK
    {
        float _firstWeight = 0.2f;
        float _secondWeight = 0.3f;
        float _thirdWeight = 0.5f;
        Bone _firstBone;
        Bone _secondBone;
        Bone _thirdBone;
        SingleBoneLookAt _firstLookAt;
        SingleBoneLookAt _secondLookAt;
        SingleBoneLookAt _thirdLookAt;

        public SingleBoneLookAt FirstLookAt { get => _firstLookAt; }
        public SingleBoneLookAt SecondLookAt { get => _secondLookAt; }
        public SingleBoneLookAt ThirdLookAt { get => _thirdLookAt; }


        public ThreeBoneLookAtIK(Bone firstBone, Bone secondBone, Bone thirdBone, 
            float firstWeight = 0.5f, float secondWeight = 0.3f, float thirdWeight = 0.2f,
            LocalSpaceAxis forward = LocalSpaceAxis.Z, LocalSpaceAxis up = LocalSpaceAxis.Y)
        {
            _firstWeight = firstWeight;
            _secondWeight = secondWeight;
            _thirdWeight = thirdWeight;
            _firstBone = firstBone;
            _secondBone = secondBone;
            _thirdBone = thirdBone;

            _firstLookAt = new SingleBoneLookAt(_firstBone, forward, up);
            _secondLookAt = new SingleBoneLookAt(_secondBone, forward, up);
            _thirdLookAt = new SingleBoneLookAt(_thirdBone, forward, up); ;
        }

        public void LookAt(Vertex3f targetPosition, Matrix4x4f modelMatrix, Animator animator)
        {
            ZetaExt.Quaternion rotation;
            _thirdLookAt.Calculate(targetPosition, modelMatrix, animator);
            rotation = ZetaExt.Quaternion.Identity.Interpolate(_thirdLookAt.Rotation, _thirdWeight);
            _thirdLookAt.Rotate((Matrix4x4f)rotation, animator, true);

            _secondLookAt.Calculate(targetPosition, modelMatrix, animator);
            rotation = ZetaExt.Quaternion.Identity.Interpolate(_secondLookAt.Rotation, _secondWeight);
            _secondLookAt.Rotate((Matrix4x4f)rotation, animator, true);

            _firstLookAt.Solve(targetPosition, modelMatrix, animator);
        }
    }
}
