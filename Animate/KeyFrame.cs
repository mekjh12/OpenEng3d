namespace Animate
{
    public class KeyFrame
    {
        private ArmaturePose _pose;
        private float _timeStamp;

        public bool ContainsKey(string boneName) => _pose.ContainsKey(boneName);

        public KeyFrame(float timeStamp)
        {
            _timeStamp = timeStamp;
            _pose = new ArmaturePose();
        }

        public ArmaturePose Pose
        {
            get => _pose;
            set => _pose = value;
        }

        public float TimeStamp
        {
            get => _timeStamp;
            set => _timeStamp = value;
        }

        public BoneTransform this[string boneName]
        {
            get => _pose[boneName];
            set => _pose[boneName] = value;
        }

        public void AddBoneTransform(string boneName, BoneTransform boneTransform)
        {
            // 뼈대가 있으면 기존 뼈대에 덮어쓰고 없으면 새로운 뼈대를 추가한다.
            _pose[boneName] = boneTransform;
        }

        public KeyFrame Clone()
        {
            KeyFrame res = new KeyFrame(_timeStamp);
            ArmaturePose armaturePose = new ArmaturePose();
            armaturePose = _pose.Clone();
            res.Pose = armaturePose;
            return res;
        }

    }
}
