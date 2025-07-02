using Geometry;
using Model3d;
using OpenGL;
using System;
using System.Collections.Generic;
using System.IO;

namespace Animate
{
    /// <summary>
    /// 영장류 클래스 - 인간, 원숭이, 유인원 등을 포함하는 포유류 목
    /// 
    /// 영장류의 주요 특징:
    /// - 발달된 뇌와 높은 지능, 복잡한 사회 구조
    /// - 정교한 손가락 움직임과 엄지손가락 대립 가능
    /// - 양안시(입체시)가 가능한 전방향 눈
    /// - 직립보행 또는 팔을 이용한 나무 위 이동
    /// - 도구 사용 능력과 학습 능력
    /// - 긴 수명과 느린 성장 과정
    /// 
    /// 3D 그래픽스 구현 완료:
    /// - 손가락 관절 애니메이션 (접기/펴기)
    /// - 눈동자 추적 시스템
    /// - 신체 부위별 아이템 장착 시스템
    /// 
    /// TODO: 표정 변화 시스템, 입 모양 변형, 헤어/털 시뮬레이션,
    /// 근육 변형 애니메이션, 걸음걸이 패턴 다양화
    /// </summary>
    public class Primate : AniModel
    {
        public enum BODY_PART
        {
            LeftHand, RightHand, Head, Back, Count
        }

        protected Entity _leftHandEntity;
        protected Entity _rightHandEntity;
        protected Entity _headEntity;
        protected Entity _backEntity;

        public Entity LeftHandEntity => _leftHandEntity;

        public Entity RightHandEntity => _rightHandEntity;

        public OBB RightHandItemEntityOBB
        {
            get
            {

                return null;
            }
        }

        public Primate(string name, AnimateEntity model, AniDae xmlDae) : base(name, model, xmlDae)
        {
            //TransplantEye(EngineLoop.PROJECT_PATH + "\\Res\\Human\\simple_eye.dae", "mixamorig_Head");

            HandGrabItem(_aniDae, "mixamorig_LeftHand_Item", "mixamorig_LeftHand",
                  Matrix4x4f.RotatedY(0), Matrix4x4f.Translated(0, 10, 3) * Matrix4x4f.Scaled(1, 1, 1));
            HandGrabItem(_aniDae, "mixamorig_RightHand_Item", "mixamorig_RightHand",
                Matrix4x4f.RotatedY(180), Matrix4x4f.Translated(0, 10, 3) * Matrix4x4f.Scaled(1, 1, 1));
            HandGrabItem(_aniDae, "mixamorig_Head_Item", "mixamorig_Head",
                Matrix4x4f.RotatedY(0), Matrix4x4f.Translated(0, 18.5f, 7.2f) * Matrix4x4f.Scaled(1, 1, 1));
            HandGrabItem(_aniDae, "mixamorig_Back", "mixamorig_Spine2",
                Matrix4x4f.RotatedY(0), Matrix4x4f.Translated(0, 0, -10.0f) * Matrix4x4f.Scaled(1, 1, 1));
        }

        public void RemoveItem(BODY_PART hand)
        {
            switch (hand)
            {
                case BODY_PART.LeftHand:
                    if (_leftHandEntity != null) Remove(_leftHandEntity.Name);
                    break;
                case BODY_PART.RightHand:
                    if (_rightHandEntity != null) Remove(_rightHandEntity.Name);
                    break;
                case BODY_PART.Head:
                    if (_headEntity != null) Remove(_headEntity.Name);
                    break;
                case BODY_PART.Back:
                    if (_backEntity != null) Remove(_backEntity.Name);
                    break;
                case BODY_PART.Count:
                    break;
                default:
                    break;
            }
        }

        public void Attach(BODY_PART hand, AnimateEntity entity)
        {
            entity.IsOnlyOneJointWeight = true;
            switch (hand)
            {
                case BODY_PART.LeftHand:
                    entity.BoneIndexOnlyOneJoint = GetBoneByName("mixamorig_LeftHand_Item").Index;
                    AddEntity(entity.Name, entity);
                    _leftHandEntity = entity;
                    break;
                case BODY_PART.RightHand:
                    entity.BoneIndexOnlyOneJoint = GetBoneByName("mixamorig_RightHand_Item").Index;
                    //entity.GenBoxOccluder();
                    AddEntity(entity.Name, entity);
                    _rightHandEntity = entity;
                    break;
                case BODY_PART.Head:
                    entity.BoneIndexOnlyOneJoint = GetBoneByName("mixamorig_Head_Item").Index;
                    AddEntity(entity.Name, entity);
                    _headEntity = entity;
                    break;
                case BODY_PART.Back:
                    break;
                case BODY_PART.Count:
                    break;
                default:
                    break;
            }
        }

        public void LootAtEye(Vertex3f worldPosition)
        {
            _updateAfter += () =>
            {
                GetBoneByName("mixamorig_eyeLeft")?.ApplyCoordinateFrame(_transform.Matrix4x4f.Position, worldPosition, Vertex3f.UnitZ);
                GetBoneByName("mixamorig_eyeRight")?.ApplyCoordinateFrame(_transform.Matrix4x4f.Position, worldPosition, Vertex3f.UnitZ);
            };
        }


        /// <summary>
        /// 손을 감싸쥔다.
        /// </summary>
        /// <param name="whereHand"></param>
        public void FoldHand(BODY_PART whereHand)
        {
            _updateAfter += () =>
            {
                Bone hand = GetBoneByName("mixamorig_" + (whereHand == BODY_PART.LeftHand ? "LeftHand" : "RightHand"));
                Stack<Bone> stack = new Stack<Bone>();
                stack.Push(hand);
                while (stack.Count > 0)
                {
                    Bone bone = stack.Pop();
                    if (bone.Name.IndexOf("Thumb") < 0)
                    {
                        bone.LocalTransform = bone.LocalBindTransform * Matrix4x4f.RotatedX(60);
                    }
                    else
                    {
                        if (bone.Name.IndexOf("Thumb1") >= 0)
                            bone.LocalTransform = bone.LocalBindTransform * Matrix4x4f.RotatedY(60);
                        if (bone.Name.IndexOf("Thumb2") >= 0)
                            bone.LocalTransform = bone.LocalBindTransform * Matrix4x4f.RotatedX(-0);
                        if (bone.Name.IndexOf("Thumb3") >= 0)
                            bone.LocalTransform = bone.LocalBindTransform * Matrix4x4f.RotatedX(-0);
                    }
                    foreach (Bone item in bone.Childrens) stack.Push(item);
                }
                hand.UpdateChildBone(isSelfIncluded: false);
            };
        }

        public void UnfoldHand(BODY_PART dir)
        {
            _updateAfter += () =>
            {
                Bone hand = GetBoneByName("mixamorig_" + (dir == BODY_PART.LeftHand ? "LeftHand" : "RightHand"));
                Stack<Bone> stack = new Stack<Bone>();
                stack.Push(hand);
                while (stack.Count > 0)
                {
                    Bone bone = stack.Pop();
                    if (bone.Name.IndexOf("Thumb") < 0)
                    {
                        bone.LocalTransform = bone.LocalBindTransform * Matrix4x4f.RotatedX(0);
                    }
                    else
                    {
                        if (bone.Name.IndexOf("Thumb1") >= 0)
                            bone.LocalTransform = bone.LocalBindTransform * Matrix4x4f.RotatedY(0);
                        if (bone.Name.IndexOf("Thumb2") >= 0)
                            bone.LocalTransform = bone.LocalBindTransform * Matrix4x4f.RotatedX(0);
                        if (bone.Name.IndexOf("Thumb3") >= 0)
                            bone.LocalTransform = bone.LocalBindTransform * Matrix4x4f.RotatedX(0);
                    }
                    foreach (Bone item in bone.Childrens) stack.Push(item);
                }
                hand.UpdateChildBone(isSelfIncluded: false);
            };
        }

        /// <summary>
        /// 눈을 이식한다.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="parentBoneName"></param>
        private void TransplantEye(string fileName, string parentBoneName)
        {
            if (!File.Exists(fileName))
            {
                Console.WriteLine($"{fileName}이 없어 눈 이식에 실패하였습니다.");
                return;
            }

            TexturedModel texturedModel = AniXmlLoader.LoadOnlyGeometryMesh(fileName);
            string boneName = $"mixamorig_eyeLeft";
            Bone LEyeBone = _aniDae.AddBone(boneName, _aniDae.BoneCount, parentBoneName,
                inverseBindTransform: Matrix4x4f.RotatedY(90).Inverse,
                localBindTransform: Matrix4x4f.Translated(4.4f, 11.8f, 12.5f) * Matrix4x4f.Scaled(0.75f, 0.65f, 0.65f));
            LEyeBone.RestrictAngle = new BoneAngle(-30, 30, -0, 0, -60, 60);
            AnimateEntity EntityL = new AnimateEntity(boneName, texturedModel);
            //EntityL.IsOnlyOneJointWeight = true;
            //EntityL.BoneIndexOnlyOneJoint = LEyeBone.Index;
            AddEntity(boneName, EntityL);

            boneName = $"mixamorig_eyeRight";
            Bone REyeBone = _aniDae.AddBone(boneName, _aniDae.BoneCount, parentBoneName,
                inverseBindTransform: Matrix4x4f.RotatedY(90).Inverse,
                localBindTransform: Matrix4x4f.Translated(-4.4f, 11.8f, 12.5f) * Matrix4x4f.Scaled(0.75f, 0.65f, 0.65f));
            REyeBone.RestrictAngle = new BoneAngle(-30, 30, -0, 0, -60, 60);
            AnimateEntity EntityR = new AnimateEntity(boneName, texturedModel);
            //EntityR.IsOnlyOneJointWeight = true;
            //EntityR.BoneIndexOnlyOneJoint = REyeBone.Index;
            AddEntity(boneName, EntityR);
        }

        /// <summary>
        /// 부모뼈로부터 자식뼈를 생성하고 생성한 뼈의 변환을 지정한다.
        /// </summary>
        /// <param name="xmlDae"></param>
        /// <param name="boneName"></param>
        /// <param name="parentBoneName"></param>
        /// <param name="bindTransform"> 캐릭터 공간의 invBind를 위하여 역행렬이 아닌 바인딩행렬을 지정한다.</param>
        /// <param name="localBindTransform">부모뼈공간에서의 바인딩 행렬을 지정한다.</param>
        /// <returns></returns>
        private Bone HandGrabItem(AniDae xmlDae, string boneName, string parentBoneName, Matrix4x4f bindTransform, Matrix4x4f localBindTransform)
        {
            Bone bone = xmlDae.AddBone(boneName, xmlDae.BoneCount, parentBoneName,
                inverseBindTransform: bindTransform.Inverse,
                localBindTransform: localBindTransform);
            //bone.RestrictAngle = new BoneAngle(-0, 0, -0, 0, -0, 0);
            return bone;
        }

    }
}