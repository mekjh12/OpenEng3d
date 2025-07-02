using System.Collections.Generic;
using System.Linq;

namespace Animate
{
    /// <summary>
    /// ----------------------------------------------------- <br/>
    /// * 뼈대 골격의 포즈클래스 <br/>
    /// ----------------------------------------------------- <br/>
    /// - 골격이 원점으로부터 S->R->T한 행렬을 가진다. <br/>
    /// - 뼈대의 로컬공간의 행렬이 아니다.<br/>
    /// - 골격의 뼈대의 정보는 뼈대이름의 딕셔너리로 접근한다. <br/>
    /// ----------------------------------------------------- <br/>
    /// </summary>
    public class ArmaturePose
    {
        Dictionary<string, BoneTransform> _pose; // 뼈대의 이름과 뼈대 포즈를 저장하는 딕셔너리

        public bool ContainsKey(string name) => _pose.ContainsKey(name);

        public ArmaturePose()
        {
            _pose = new Dictionary<string, BoneTransform>();
        }

        public BoneTransform this[string jointName]
        {
            get => _pose.ContainsKey(jointName)? _pose[jointName] : BoneTransform.Identity;
            set => _pose[jointName] = value;
        }

        public string[] BoneNames => _pose.Keys.ToArray();

        public BoneTransform[] BoneTransforms => _pose.Values.ToArray();

        public ArmaturePose Clone()
        {
            ArmaturePose clone = new ArmaturePose();
            foreach (var kvp in _pose)
            {
                clone._pose[kvp.Key] = kvp.Value;
            }
            return clone;
        }
    }
}
