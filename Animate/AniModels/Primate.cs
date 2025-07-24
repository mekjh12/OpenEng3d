using Microsoft.SqlServer.Server;
using Model3d;
using OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using ZetaExt;

namespace Animate
{
    /// <summary>
    /// 영장류 클래스 - 인간, 원숭이, 유인원 등을 포함하는 포유류 목
    /// <code>
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
    /// </code>
    /// </summary>
    public abstract class Primate : AniActor
    {
        Dictionary<string, Action> _actions;

        public Primate(string name, AniRig aniRig) : base(name, aniRig)
        {
            _actions = new Dictionary<string, Action>();

            //TransplantEye(EngineLoop.PROJECT_PATH + "\\Res\\Human\\simple_eye.dae", "mixamorig_Head");
            /*
            HandGrabItem(_aniRig, "mixamorig_LeftHand_Item", "mixamorig_LeftHand",
                  Matrix4x4f.RotatedY(0), Matrix4x4f.Translated(0, 10, 3) * Matrix4x4f.Scaled(1, 1, 1));
            HandGrabItem(_aniRig, "mixamorig_RightHand_Item", "mixamorig_RightHand",
                Matrix4x4f.RotatedY(180), Matrix4x4f.Translated(0, 10, 3) * Matrix4x4f.Scaled(1, 1, 1));
            HandGrabItem(_aniRig, "mixamorig_Head_Item", "mixamorig_Head",
                Matrix4x4f.RotatedY(0), Matrix4x4f.Translated(0, 18.5f, 7.2f) * Matrix4x4f.Scaled(1, 1, 1));
            HandGrabItem(_aniRig, "mixamorig_Back", "mixamorig_Spine2",
                Matrix4x4f.RotatedY(0), Matrix4x4f.Translated(0, 0, -10.0f) * Matrix4x4f.Scaled(1, 1, 1));
            */
        }

        public void Attach(ATTACHMENT_SLOT hand)
        {
            
        }

        public void LootAtEye(Vertex3f worldPosition)
        {
            _updateAfter += () =>
            {
                AniRig.Armature["mixamorig_eyeLeft"]?.ApplyCoordinateFrame(_transform.Matrix4x4f.Position, worldPosition, Vertex3f.UnitZ);
                AniRig.Armature["mixamorig_eyeRight"]?.ApplyCoordinateFrame(_transform.Matrix4x4f.Position, worldPosition, Vertex3f.UnitZ);
            };
        }

        /// <summary>
        /// 손을 감싸쥔다.
        /// </summary>
        /// <param name="whereHand"></param>
        public void FoldHand(bool isLeft, float intensity = 60.0f)
        {
            string handName = (isLeft ? "LeftHand" : "RightHand");

            if (!_actions.ContainsKey("fold"+ handName))
            {
                Action action = () =>
                {
                    // 손을 가져온다.
                    Bone hand = AniRig.Armature["mixamorig_" + handName];

                    foreach (Bone bone in hand.ToBFSList(exceptBone: hand))
                    {
                        // 엄지 손가락이 아닌 경우
                        if (bone.Name.IndexOf("Thumb") < 0)
                        {
                            bone.BoneTransforms.LocalTransform = bone.BoneTransforms.LocalBindTransform * Matrix4x4f.RotatedX(40);
                        }
                    }

                    // 손의 모든 자식본을 업데이트한다.
                    hand.UpdateAnimatorTransforms(_animator, isSelfIncluded: false);
                };

                if (isLeft) _actions["fold" + handName] = action;
                if (!isLeft) _actions["fold" + handName] = action;

                _updateAfter += action;
            }
        }

        public void UnfoldHand(bool isLeft)
        {
            string keyName = "fold" + (isLeft ? "Left" : "Right") + "Hand";
            Action action = _actions[keyName];
            _updateAfter -= action;
            _actions.Remove(keyName);
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
            Bone LEyeBone = _aniRig.AddBone(boneName, _aniRig.BoneCount, parentBoneName,
                inverseBindPoseTransform: Matrix4x4f.RotatedY(90).Inverse,
                localBindTransform: Matrix4x4f.Translated(4.4f, 11.8f, 12.5f) * Matrix4x4f.Scaled(0.75f, 0.65f, 0.65f));
            LEyeBone.BoneKinematics.RestrictAngle = new BoneAngle(-30, 30, -0, 0, -60, 60);
            //AddEntity(boneName, texturedModel);

            boneName = $"mixamorig_eyeRight";
            Bone REyeBone = _aniRig.AddBone(boneName, _aniRig.BoneCount, parentBoneName,
                inverseBindPoseTransform: Matrix4x4f.RotatedY(90).Inverse,
                localBindTransform: Matrix4x4f.Translated(-4.4f, 11.8f, 12.5f) * Matrix4x4f.Scaled(0.75f, 0.65f, 0.65f));
            REyeBone.BoneKinematics.RestrictAngle = new BoneAngle(-30, 30, -0, 0, -60, 60);
            //AddEntity(boneName, texturedModel);
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
        private Bone HandGrabItem(AniRig aniRig, string boneName, string parentBoneName, Matrix4x4f bindTransform, Matrix4x4f localBindTransform)
        {
            Bone bone = aniRig.AddBone(boneName, aniRig.BoneCount, parentBoneName,
                inverseBindPoseTransform: bindTransform.Inverse,
                localBindTransform: localBindTransform);
            //bone.RestrictAngle = new BoneAngle(-0, 0, -0, 0, -0, 0);
            return bone;
        }

    }
}