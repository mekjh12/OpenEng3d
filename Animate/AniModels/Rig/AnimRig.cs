using Assimp;
using Model3d;
using OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Animate
{
    /// <summary>
    /// 모델, 모션, 골격을 통합 관리하는 애니메이션 리그 클래스
    /// </summary>
    public partial class AnimRig
    {
        // -----------------------------------------------------------------------
        // 멤버변수
        // -----------------------------------------------------------------------

        // 파일 정보
        private string _filename;           // 파일 경로
        private readonly string _name;      // 리그 이름

        // 핵심 데이터
        private MotionStorage _motions;                 // 모션 저장소
        private MotionCache _motionCache;               // 모션 캐시 (블렌딩 모션 캐시)
        private Motion _bindMotion;                     // 바인딩 모션

        private List<TexturedModel> _texturedModels;    // 텍스처 모델 목록
        private Armature _armature;                     // 골격 구조

        // 추가 본 부위 인덱스 딕셔너리
        protected Dictionary<ATTACHMENT_SLOT, int> _dicIndices;

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        // 공개 속성
        public Dictionary<string, Bone> DicBones => _armature.DicBones;
        public Armature Armature => _armature;
        public MotionStorage Motions => _motions;
        public MotionCache MotionCache => _motionCache;
        public int BoneCount => _armature.BoneNames.Length;
        public List<TexturedModel> TexturedModels => _texturedModels;
        public string Name => _name;
        public Motion BindMotion => _bindMotion;
        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        /// <summary>
        /// AniRig 생성자
        /// </summary>
        /// <param name="filename">로드할 파일 경로</param>
        /// <param name="hipBoneName"> 힙 본 이름</param>
        /// <param name="isLoadAnimation">애니메이션 로드 여부</param>
        public AnimRig(string filename, string hipBoneName, bool isLoadAnimation = true)
        {
            // 파일명과 이름 설정
            _filename = filename;
            _name = Path.GetFileNameWithoutExtension(filename);

            // 골격 구조 초기화
            _armature = new Armature(hipBoneName);

            // 파일에서 데이터 로드
            AniRigLoader.LoadFile(filename, hipBoneName, 
                out Armature armature,
                out MotionStorage motions,
                out List<TexturedModel> models,
                out Matrix4x4f bindShapeMatrix,
                out MeshData meshData);

            // 가져온 골격에서 힙 높이를 설정한다.
            foreach (Bone bone in armature.DicBones.Values)
            {
                if (bone.IsHipBone)
                {
                    armature.HipHeight = bone.BoneMatrixSet.Pivot.z;
                    break;
                }
            }

            // 로드된 데이터로 초기화
            _motions = motions;
            _armature = armature;
            _texturedModels = models;

            // 텍스처 모델 리스트 재초기화 (중복 처리 - 개선 필요)
            _texturedModels = new List<TexturedModel>();
            _texturedModels.AddRange(models);

            // 모션 저장소 재초기화 (중복 처리 - 개선 필요)
            _motions = new MotionStorage();
            _motions.AddMotion(new Motion("default", lengthInSeconds: 2.0f));

            // 모션 캐시 초기화
            _motionCache = new MotionCache(_name);

            // 추가 본 부위 인덱스 딕셔너리 초기화
            _dicIndices = new Dictionary<ATTACHMENT_SLOT, int>();

            // A or T-Pose 모션을 추가한다.
            _bindMotion = (Motion)AniRigLoader.LoadBindPoseMotion(this);
        }

        public int this[ATTACHMENT_SLOT bodyPart]
        {
            get
            {
                if (_dicIndices.TryGetValue(bodyPart, out int index))
                {
                    return index;
                }
                return -1; // 해당 부위가 없을 경우 -1 반환
            }
        }

        /// <summary>
        /// 리그에 모션을 추가한다.
        /// </summary>
        /// <param name="motion">추가할 모션</param>
        public void AddMotion(Motionable motion)
        {
            Motions.AddMotion(motion);
        }


        public Motionable GetMotion(string name)
        {
            return _motions.GetMotion(name);
        }

        public Motionable AddBlendMotion(string newMotionName, string motionName1, string motionName2, 
            float blendFactor1, float blendFactor2, float periodicTime = 0.0f)
        {
            Motionable motion1 = _motions.GetMotion(motionName1);
            Motionable motion2 = _motions.GetMotion(motionName2);

            if (motion1 == null || motion2 == null)
            {
                throw new Exception($"모션({motionName1} 또는 {motionName2})이 존재하지 않습니다.");
            }

            BlendMotion newBlendMotion = new BlendMotion(newMotionName, motion1, motion2, blendFactor1, blendFactor2, blendFactor1);
            _motions.AddMotion(newBlendMotion);

            // 기본값으로 두 행동의 평균으로 설정한다.
            float avg = 0.5f;
            newBlendMotion.SetBlendFactor(blendFactor1 * (1 - avg) + blendFactor2 * avg);

            // 주기 시간 설정
            if (periodicTime <= 0.0f)
            {
                // 주기 시간이 0 이하이면 두 모션의 평균 길이로 설정
            }
            else
            {
                // 주기 시간이 양수이면 해당 값으로 설정
                newBlendMotion.SetPeriodTime(periodicTime);
            }

            return newBlendMotion;
        }

        /// <summary>
        /// 골격에 새로운 뼈대를 추가한다.
        /// </summary>
        /// <param name="boneName">추가할 뼈대 이름</param>
        /// <param name="boneIndex">추가할 뼈대 인덱스</param>
        /// <param name="parentBoneName">부모 뼈대 이름</param>
        /// <param name="inverseBindPoseTransform">캐릭터 공간의 바인딩행렬의 역행렬</param>
        /// <param name="localBindTransform">부모 뼈공간에서의 바인딩 행렬</param>
        /// <returns>생성된 뼈대 객체</returns>
        public Bone AddBone(string boneName, int boneIndex,
            string parentBoneName,
            Matrix4x4f inverseBindPoseTransform,
            Matrix4x4f localBindTransform)
        {
            // 뼈대 이름 중복 확인
            if (_armature.IsExistBone(boneName))
            {
                throw new Exception($"뼈대 이름({boneName})이 이미 존재합니다.");
            }

            // 뼈대 인덱스 중복 확인
            if (_armature.IsExistBoneIndex(boneName))
            {
                throw new Exception($"뼈대 인덱스({boneIndex})가 이미 존재합니다.");
            }

            // 부모 뼈대 존재 확인
            if (!_armature.IsExistBoneIndex(parentBoneName))
            {
                throw new Exception($"부모 뼈대 이름({parentBoneName})이 존재하지 않습니다.");
            }

            // 새로운 뼈대 생성 및 설정
            Bone parentBone = _armature[parentBoneName];
            Bone newBone = new Bone(boneName, boneIndex);

            // 부모-자식 관계 설정
            parentBone.AddChild(newBone);
            newBone.Parent = parentBone;

            // 변환 행렬 설정
            newBone.BoneMatrixSet.LocalBindTransform = localBindTransform;
            newBone.BoneMatrixSet.LocalTransform = localBindTransform;
            newBone.BoneMatrixSet.InverseBindPoseTransform = inverseBindPoseTransform;

            // 골격에 뼈대 추가
            _armature.AddBone(newBone);

            return newBone;
        }

    }
}