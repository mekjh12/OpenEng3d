using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Animate
{
    /// <summary>
    /// 3D 애니메이션에서 사용되는 골격(Armature) 클래스
    /// 본(Bone)들의 계층 구조를 관리하고 애니메이션 처리를 위한 기능을 제공합니다.
    /// </summary>
    public class Armature
    {
        // 골격 구조
        private Bone _rootBone;             // 루트 본
        private float _hipHeight = 1.0f;    // 엉덩이 높이 비율
        private string _hipBoneName = "";  // 루트 본 이름
        private int _maxBoneIndex = 0;      // 

        // 본 관리 데이터
        private Dictionary<string, Bone> _dicBones;         // 본 이름 -> 본 객체 매핑
        private Dictionary<string, int> _dicBoneIndex;      // 본 이름 -> 인덱스 매핑
        private Dictionary<int, string> _dicBoneNames;      // 본 인덱스 -> 본 이름 매핑

        /// <summary>
        /// 엉덩이 높이 비율을 가져오거나 설정한다.
        /// </summary>
        public float HipHeight
        {
            get => _hipHeight;
            set => _hipHeight = value;
        }

        public string[] BoneNames => _dicBoneIndex.Keys.ToArray();
        public Bone RootBone => _rootBone;

        public Dictionary<string, Bone> DicBones => _dicBones;

        public int MaxBoneIndex { get => _maxBoneIndex; }

        /// <summary>
        /// Armature 생성자
        /// </summary>
        public Armature(string hipBoneName)
        {
            _dicBones = new Dictionary<string, Bone>();
            _dicBoneIndex = new Dictionary<string, int>();
            _dicBoneNames = new Dictionary<int, string>();
            _hipBoneName = hipBoneName;
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

        public Bone this[int boneIndex]
        {
            get
            {
                string boneName = _dicBoneNames.ContainsKey(boneIndex) ? _dicBoneNames[boneIndex] : null;
                if (boneName == null) return null;
                return _dicBones[boneName];
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
            _dicBoneNames.Clear();

            // 본 이름과 인덱스 매핑 생성
            for (int i = 0; i < boneNames.Length; i++)
            {
                _dicBoneIndex.Add(boneNames[i], i);
                _dicBoneNames.Add(i, boneNames[i]);
            }

            _maxBoneIndex = boneNames.Length;
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

        public int AttachBone(string parentBoneName, ATTACHMENT_SLOT boneName, Matrix4x4f localBindTransform)
        {
            return AttachBone(parentBoneName, boneName.ToString(), localBindTransform);
        }

        /// <summary>
        /// 지정된 부모 본에 새로운 본을 연결한다.
        /// </summary>
        /// <param name="parentBoneName">부모 본의 이름</param>
        /// <param name="boneName">새로 생성할 본의 이름</param>
        /// <param name="localBindTransform">새 본의 로컬 바인딩 변환 행렬</param>
        /// <exception cref="ArgumentException">부모 본이 존재하지 않거나 본 이름이 이미 존재할 때</exception>
        /// <exception cref="ArgumentNullException">본 이름이 null이거나 빈 문자열일 때</exception>
        public int AttachBone(string parentBoneName, string boneName, Matrix4x4f localBindTransform)
        {
            // 이미 추가된 본이면 추가할 필요가 없다.
            if (_dicBones.ContainsKey(boneName)) return _dicBoneIndex[boneName];

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
            newBone.BoneMatrixSet.LocalBindTransform = localBindTransform;
            newBone.BoneMatrixSet.LocalTransform = localBindTransform;
            newBone.BoneMatrixSet.InverseBindPoseTransform = Matrix4x4f.Identity;
            parentBone.AddChild(newBone);

            // 딕셔너리에 새 본 추가
            _dicBones[boneName] = newBone;
            _dicBoneIndex[boneName] = newBoneIndex;

            return newBoneIndex;
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


        /// <summary>
        /// RootBone으로부터 연결된 모든 본을 쇠사슬 구조로 출력한다.
        /// </summary>
        /// <returns>본들의 계층 구조를 나타내는 문자열</returns>
        public override string ToString()
        {
            if (_rootBone == null)
            {
                return "Armature: [루트 본 없음]";
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Armature: 총 {_dicBones.Count}개의 본");
            sb.AppendLine($"엉덩이 높이 비율: {_hipHeight:F3}");
            sb.AppendLine("본 계층 구조:");

            // 루트 본부터 시작하여 재귀적으로 트리 구조 출력
            BuildBoneChainString(_rootBone, sb, "", true);

            return sb.ToString();
        }

        /// <summary>
        /// 재귀적으로 본의 계층 구조를 문자열로 구성한다.
        /// </summary>
        /// <param name="bone">현재 출력할 본</param>
        /// <param name="sb">문자열 빌더</param>
        /// <param name="indent">들여쓰기 문자열</param>
        /// <param name="isLast">현재 본이 같은 레벨에서 마지막 본인지 여부</param>
        private void BuildBoneChainString(Bone bone, StringBuilder sb, string indent, bool isLast)
        {
            if (bone == null) return;

            // 트리 구조 표시용 문자
            string connector = isLast ? "└── " : "├── ";
            string nextIndent = indent + (isLast ? "    " : "│   ");

            // 본 정보 출력
            string boneInfo = $"[{bone.Index}] {bone.Name}";

            // 추가 정보 표시 (자식 개수, 위치 등)
            if (bone.Children.Count > 0)
            {
                boneInfo += $" (자식: {bone.Children.Count}개)";
            }
            else
            {
                boneInfo += " (말단 본)";
            }

            sb.AppendLine($"{indent}{connector}{boneInfo}");

            // 자식 본들을 재귀적으로 출력
            for (int i = 0; i < bone.Children.Count; i++)
            {
                bool isLastChild = (i == bone.Children.Count - 1);
                BuildBoneChainString(bone.Children[i], sb, nextIndent, isLastChild);
            }
        }

        /// <summary>
        /// 간단한 본 체인 정보를 한 줄로 출력한다.
        /// </summary>
        /// <returns>간단한 본 체인 문자열</returns>
        public string ToSimpleChainString()
        {
            if (_rootBone == null)
            {
                return "빈 골격";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append($"[{_rootBone.Name}]");

            BuildSimpleChain(_rootBone, sb);

            return sb.ToString();
        }

        /// <summary>
        /// 재귀적으로 간단한 체인 문자열을 구성한다.
        /// </summary>
        /// <param name="bone">현재 본</param>
        /// <param name="sb">문자열 빌더</param>
        private void BuildSimpleChain(Bone bone, StringBuilder sb)
        {
            if (bone == null || bone.Children.Count == 0) return;

            foreach (var child in bone.Children)
            {
                sb.Append($" → [{child.Name}]");
                BuildSimpleChain(child, sb);
            }
        }
    }
}