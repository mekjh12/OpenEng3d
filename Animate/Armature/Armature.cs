using System;
using System.Collections.Generic;
using System.Linq;

namespace Animate
{
    public class Armature
    {
        Bone _rootBone;
        float _hipHeightScaled = 1.0f; // 비율을 얻는다.

        Dictionary<string, Bone> _dicBones;
        Dictionary<string, int> _dicBoneIndex;
        string[] _boneNames;

        public float HipHeightScaled
        {
            get => _hipHeightScaled;
            set => _hipHeightScaled = value;
        }

        public string[] BoneNames => _dicBoneIndex.Keys.ToArray();
        public Bone RootBone => _rootBone;
        public Dictionary<string, Bone> DicBones => _dicBones;

        public Armature()
        {
            _dicBones = new Dictionary<string, Bone>();
            _dicBoneIndex = new Dictionary<string, int>();
        }

        public Bone this[string boneName]
        {
            get
            {
                if (_dicBones == null) return null;
                return _dicBones.ContainsKey(boneName) ? _dicBones[boneName] : null;
            }
        }

        /// <summary>
        /// 본(Bone) 이름 배열을 설정하고 이름-인덱스 매핑을 생성합니다.
        /// </summary>
        /// <param name="boneNames">설정할 본 이름들의 배열</param>
        public void SetupBoneMapping(string[] boneNames)
        {
            // 본 이름 배열 초기화
            _boneNames = new string[boneNames.Length];

            // 본 이름-인덱스 딕셔너리 초기화 (기존 데이터 제거)
            _dicBoneIndex.Clear();

            // 각 본 이름을 배열에 저장하고 딕셔너리에 인덱스 매핑 추가
            for (int i = 0; i < boneNames.Length; i++)
            {
                _boneNames[i] = boneNames[i];
                _dicBoneIndex.Add(boneNames[i], i);
            }
        }

        public void AddBone(Bone bone)
        {
            string boneName = bone.Name;

            if (_dicBones.ContainsKey(boneName))
            {
                throw new ArgumentException($"Bone with name {boneName} already exists.");
            }

            _dicBones[boneName] = bone;
        }


        public void AddBone(string boneName, Bone bone)
        {
            if (bone == null)
            {
                throw new ArgumentNullException(nameof(bone));
            }

            if (_dicBones.ContainsKey(boneName))
            {
                throw new ArgumentException($"Bone with name {boneName} already exists.");
            }

            _boneNames = _boneNames.Append(boneName).ToArray();

            _dicBones[boneName] = bone;
        }

        public int GetBoneIndex(string boneName)
        {
            if (_dicBoneIndex == null || !_dicBoneIndex.ContainsKey(boneName))
            {
                return -1; // 존재하지 않는 경우
            }
            return _dicBoneIndex[boneName];
        }

        public bool IsExistBoneIndex(string boneName)
        {
            if (_dicBoneIndex == null)
            {
                return false;
            }
            return _dicBoneIndex.ContainsKey(boneName);
        }

        public bool IsExistBone(string boneName)
        {
            if (_dicBones == null)
            {
                return false;
            }

            return _dicBones.ContainsKey(boneName);
        }

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
