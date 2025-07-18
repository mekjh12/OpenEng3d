using Model3d;
using OpenGL;
using System;
using System.Collections.Generic;
using System.IO;

namespace Animate
{
    /// <summary>
    /// 모델, 모션, 골격을 통합 관리하는 애니메이션 리그 클래스
    /// </summary>
    public class AniRig
    {
        // 파일 정보
        private string _filename;           // 파일 경로
        private readonly string _name;      // 리그 이름

        // 핵심 데이터
        private Matrix4x4f _bindShapeMatrix;            // 바인드 형태 행렬
        private MotionStorage _motions;                 // 모션 저장소
        private List<TexturedModel> _texturedModels;    // 텍스처 모델 목록
        private Armature _armature;                     // 골격 구조

        // 공개 속성
        public Dictionary<string, Bone> DicBones => _armature.DicBones;
        public Armature Armature => _armature;
        public MotionStorage Motions => _motions;
        public int BoneCount => _armature.BoneNames.Length;
        public List<TexturedModel> TexturedModels => _texturedModels;
        public string Name => _name;
        public Matrix4x4f BindShapeMatrix => _bindShapeMatrix;

        /// <summary>
        /// AniRig 생성자
        /// </summary>
        /// <param name="filename">로드할 파일 경로</param>
        /// <param name="isLoadAnimation">애니메이션 로드 여부</param>
        public AniRig(string filename, bool isLoadAnimation = true)
        {
            // 파일명과 이름 설정
            _filename = filename;
            _name = Path.GetFileNameWithoutExtension(filename);

            // 골격 구조 초기화
            _armature = new Armature();

            // 파일에서 데이터 로드
            (Armature armature, MotionStorage motions, List<TexturedModel> models, Matrix4x4f bindShapeMatrix)
                = AniRigLoader.LoadFile(filename);

            // 로드된 데이터로 초기화
            _bindShapeMatrix = bindShapeMatrix;
            _motions = motions;
            _armature = armature;
            _texturedModels = models;

            // 텍스처 모델 리스트 재초기화 (중복 처리 - 개선 필요)
            _texturedModels = new List<TexturedModel>();
            _texturedModels.AddRange(models);

            // 모션 저장소 재초기화 (중복 처리 - 개선 필요)
            _motions = new MotionStorage();
            _motions.AddMotion(new Motion("default", lengthInSeconds: 2.0f));
        }

        /// <summary>
        /// 리그에 모션을 추가한다.
        /// </summary>
        /// <param name="motion">추가할 모션</param>
        public void AddMotion(Motion motion)
        {
            Motions.AddMotion(motion);
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
            newBone.BoneTransforms.LocalBindTransform = localBindTransform;
            newBone.BoneTransforms.InverseBindPoseTransform = inverseBindPoseTransform;

            // 골격에 뼈대 추가
            _armature.AddBone(newBone);

            return newBone;
        }
    }
}