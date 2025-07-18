using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Animate
{
    /// <summary>
    /// 3D 애니메이션에서 사용되는 골격(Armature) 클래스
    /// 본(Bone)들의 계층 구조를 관리하고 애니메이션 처리를 위한 기능을 제공합니다.
    /// </summary>
    public class Armature
    {
        // 골격 구조
        private Bone _rootBone;                 // 루트 본
        private float _hipHeightScaled = 1.0f;  // 엉덩이 높이 비율

        // 본 관리 데이터
        private Dictionary<string, Bone> _dicBones;     // 본 이름 -> 본 객체 매핑
        private Dictionary<string, int> _dicBoneIndex;  // 본 이름 -> 인덱스 매핑

        /// <summary>
        /// 엉덩이 높이 비율을 가져오거나 설정한다.
        /// </summary>
        public float HipHeightScaled
        {
            get => _hipHeightScaled;
            set => _hipHeightScaled = value;
        }

        public string[] BoneNames => _dicBoneIndex.Keys.ToArray();
        public Bone RootBone => _rootBone;
        public Dictionary<string, Bone> DicBones => _dicBones;

        /// <summary>
        /// Armature 생성자
        /// </summary>
        public Armature()
        {
            _dicBones = new Dictionary<string, Bone>();
            _dicBoneIndex = new Dictionary<string, int>();
        }

        /// <summary>
        /// 본 이름으로 본 객체에 접근하는 인덱서
        /// </summary>
        /// <param name="boneName">찾을 본의 이름</param>
        /// <returns>해당 본 객체, 없으면 null</returns>
        public Bone this[string boneName]
        {
            get
            {
                if (_dicBones == null) return null;
                return _dicBones.ContainsKey(boneName) ? _dicBones[boneName] : null;
            }
        }

        /// <summary>
        /// 본 이름 배열을 설정하고 이름-인덱스 매핑을 생성한다.
        /// </summary>
        /// <param name="boneNames">설정할 본 이름들의 배열</param>
        public void SetupBoneMapping(string[] boneNames)
        {
            // 기존 인덱스 매핑 초기화
            _dicBoneIndex.Clear();

            // 본 이름과 인덱스 매핑 생성
            for (int i = 0; i < boneNames.Length; i++)
            {
                _dicBoneIndex.Add(boneNames[i], i);
            }
        }

        /// <summary>
        /// 골격에 새로운 본을 추가한다.
        /// </summary>
        /// <param name="bone">추가할 본 객체</param>
        /// <exception cref="ArgumentException">동일한 이름의 본이 이미 존재할 때</exception>
        public void AddBone(Bone bone)
        {
            string boneName = bone.Name;

            // 본 이름 유효성 검사
            if (boneName == "")
            {
                throw new Exception("이 메소드를 사용하려면 Bone Name이 존재해야 합니다.");
            }

            // 중복 본 이름 확인
            if (_dicBones.ContainsKey(boneName))
            {
                throw new ArgumentException($"Bone with name {boneName} already exists.");
            }

            // 본 추가
            _dicBones[boneName] = bone;
        }

        /// <summary>
        /// 지정된 부모 본에 새로운 본을 연결한다.
        /// </summary>
        /// <param name="parentBoneName">부모 본의 이름</param>
        /// <param name="boneName">새로 생성할 본의 이름</param>
        /// <param name="localTransform">새 본의 로컬 변환 행렬</param>
        /// <exception cref="ArgumentException">부모 본이 존재하지 않거나 본 이름이 이미 존재할 때</exception>
        /// <exception cref="ArgumentNullException">본 이름이 null이거나 빈 문자열일 때</exception>
        public void AttachBone(string parentBoneName, string boneName, Matrix4x4f localTransform)
        {            
            // 부모 본 존재 확인
            if (!_dicBones.ContainsKey(parentBoneName))
                throw new ArgumentException($"부모 본 '{parentBoneName}'이 존재하지 않습니다.", nameof(parentBoneName));

            // 중복 본 이름 확인
            if (_dicBones.ContainsKey(boneName))
                throw new ArgumentException($"본 이름 '{boneName}'이 이미 존재합니다.", nameof(boneName));

            // 부모-자식 관계 설정
            Bone parentBone = _dicBones[parentBoneName];

            // 새로운 본 인덱스 계산
            int newBoneIndex = _dicBoneIndex.Count > 0 ? _dicBoneIndex.Values.Max() + 1 : 0;

            // 새로운 본 생성
            Bone newBone = new Bone(boneName, newBoneIndex);

            // 변환 행렬 설정
            newBone.BoneTransforms.LocalBindTransform = localTransform;
            newBone.BoneTransforms.InverseBindPoseTransform = Matrix4x4f.Identity;
            parentBone.AddChild(newBone);

            // 딕셔너리에 새 본 추가
            _dicBones[boneName] = newBone;
            _dicBoneIndex[boneName] = newBoneIndex;

            // 애니메이션 변환 행렬 업데이트
            newBone.UpdatePropagateTransform(isSelfIncluded: true);
        }

        /// <summary>
        /// 본 이름으로 인덱스를 가져온다.
        /// </summary>
        /// <param name="boneName">찾을 본의 이름</param>
        /// <returns>본의 인덱스, 존재하지 않으면 -1</returns>
        public int GetBoneIndex(string boneName)
        {
            // 딕셔너리 유효성 및 본 존재 확인
            if (_dicBoneIndex == null || !_dicBoneIndex.ContainsKey(boneName))
            {
                return -1; // 존재하지 않는 경우
            }
            return _dicBoneIndex[boneName];
        }

        /// <summary>
        /// 지정된 이름의 본 인덱스가 존재하는지 확인한다.
        /// </summary>
        /// <param name="boneName">확인할 본의 이름</param>
        /// <returns>존재하면 true, 아니면 false</returns>
        public bool IsExistBoneIndex(string boneName)
        {
            if (_dicBoneIndex == null)
            {
                return false;
            }
            return _dicBoneIndex.ContainsKey(boneName);
        }

        /// <summary>
        /// 지정된 이름의 본이 존재하는지 확인한다.
        /// </summary>
        /// <param name="boneName">확인할 본의 이름</param>
        /// <returns>존재하면 true, 아니면 false</returns>
        public bool IsExistBone(string boneName)
        {
            if (_dicBones == null)
            {
                return false;
            }
            return _dicBones.ContainsKey(boneName);
        }

        /// <summary>
        /// 루트 본을 설정한다.
        /// </summary>
        /// <param name="rootBone">설정할 루트 본</param>
        /// <exception cref="ArgumentNullException">rootBone이 null일 때</exception>
        public void SetRootBone(Bone rootBone)
        {
            if (rootBone == null)
            {
                throw new ArgumentNullException(nameof(rootBone));
            }
            _rootBone = rootBone;
        }
    }
}