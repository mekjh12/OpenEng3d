using System;
using System.Collections.Generic;
using System.Linq;

namespace Animate
{
    public class Armature
    {
        bool _isInitialized = false;
        Bone _rootBone;
        float _hipHeightScaled = 1.0f; // 비율을 얻는다.

        Dictionary<string, Bone> _dicBones;
        Dictionary<string, int> _dicBoneIndex;
        string[] _boneNames;

        public Bone GetBoneByName(string boneName)
        {
            return _dicBones.ContainsKey(boneName) ? _dicBones[boneName] : null;
        }

        public void SetBoneNames(string[] boneNames)
        {
            if (_dicBoneIndex == null)
            {
                _dicBoneIndex = new Dictionary<string, int>();
            }

            _boneNames = new string[boneNames.Length];
            for (int i = 0; i < _boneNames.Length; i++)
            {
                _boneNames[i] = boneNames[i];
                _dicBoneIndex.Add(_boneNames[i], i);
            }

            _isInitialized = true;
        }

        public void AddBone(string boneName, Bone bone)
        {
            if (!_isInitialized)
            {
                throw new ArgumentException("_boneNames을 먼저 초기화해야 합니다.");
            }

            if (bone == null)
            {
                throw new ArgumentNullException(nameof(bone));
            }

            if (_dicBones == null)
            {
                _dicBones = new Dictionary<string, Bone>();
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

        public string[] BoneNames
        {
            get => _dicBoneIndex.Keys.ToArray<string>();
        }

        public float HipHeightScaled
        {
            get => _hipHeightScaled;
            set => _hipHeightScaled = value;
        }


        public Bone RootBone
        {
            get => _rootBone;
        }

        public Dictionary<string, Bone> DicBones
        {
            get => _dicBones;
        }

        public Armature()
        {

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
