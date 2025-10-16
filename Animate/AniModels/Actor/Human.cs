using AutoGenEnums;
using Common.Abstractions;
using OpenGL;
using Shader;
using System.Collections.Generic;
using System.Drawing;
using Ui3d;
using ZetaExt;

namespace Animate
{
    public static class MIXAMORIG_BONENAME
    {
        // Hips (Root)
        public const string mixamorig_Hips = "mixamorig_Hips";

        // Spine
        public const string mixamorig_Spine = "mixamorig_Spine";
        public const string mixamorig_Spine1 = "mixamorig_Spine1";
        public const string mixamorig_Spine2 = "mixamorig_Spine2";

        // Neck and Head
        public const string mixamorig_Neck = "mixamorig_Neck";
        public const string mixamorig_Head = "mixamorig_Head";
        public const string mixamorig_HeadTop_End = "mixamorig_HeadTop_End";

        // Left Leg
        public const string mixamorig_LeftUpLeg = "mixamorig_LeftUpLeg";
        public const string mixamorig_LeftLeg = "mixamorig_LeftLeg";
        public const string mixamorig_LeftFoot = "mixamorig_LeftFoot";
        public const string mixamorig_LeftToeBase = "mixamorig_LeftToeBase";
        public const string mixamorig_LeftToe_End = "mixamorig_LeftToe_End";

        // Right Leg
        public const string mixamorig_RightUpLeg = "mixamorig_RightUpLeg";
        public const string mixamorig_RightLeg = "mixamorig_RightLeg";
        public const string mixamorig_RightFoot = "mixamorig_RightFoot";
        public const string mixamorig_RightToeBase = "mixamorig_RightToeBase";
        public const string mixamorig_RightToe_End = "mixamorig_RightToe_End";

        // Left Arm
        public const string mixamorig_LeftShoulder = "mixamorig_LeftShoulder";
        public const string mixamorig_LeftArm = "mixamorig_LeftArm";
        public const string mixamorig_LeftForeArm = "mixamorig_LeftForeArm";
        public const string mixamorig_LeftHand = "mixamorig_LeftHand";

        // Left Hand Fingers
        public const string mixamorig_LeftHandThumb1 = "mixamorig_LeftHandThumb1";
        public const string mixamorig_LeftHandThumb2 = "mixamorig_LeftHandThumb2";
        public const string mixamorig_LeftHandThumb3 = "mixamorig_LeftHandThumb3";
        public const string mixamorig_LeftHandThumb4 = "mixamorig_LeftHandThumb4";
        public const string mixamorig_LeftHandIndex1 = "mixamorig_LeftHandIndex1";
        public const string mixamorig_LeftHandIndex2 = "mixamorig_LeftHandIndex2";
        public const string mixamorig_LeftHandIndex3 = "mixamorig_LeftHandIndex3";
        public const string mixamorig_LeftHandIndex4 = "mixamorig_LeftHandIndex4";
        public const string mixamorig_LeftHandMiddle1 = "mixamorig_LeftHandMiddle1";
        public const string mixamorig_LeftHandMiddle2 = "mixamorig_LeftHandMiddle2";
        public const string mixamorig_LeftHandMiddle3 = "mixamorig_LeftHandMiddle3";
        public const string mixamorig_LeftHandMiddle4 = "mixamorig_LeftHandMiddle4";
        public const string mixamorig_LeftHandRing1 = "mixamorig_LeftHandRing1";
        public const string mixamorig_LeftHandRing2 = "mixamorig_LeftHandRing2";
        public const string mixamorig_LeftHandRing3 = "mixamorig_LeftHandRing3";
        public const string mixamorig_LeftHandRing4 = "mixamorig_LeftHandRing4";
        public const string mixamorig_LeftHandPinky1 = "mixamorig_LeftHandPinky1";
        public const string mixamorig_LeftHandPinky2 = "mixamorig_LeftHandPinky2";
        public const string mixamorig_LeftHandPinky3 = "mixamorig_LeftHandPinky3";
        public const string mixamorig_LeftHandPinky4 = "mixamorig_LeftHandPinky4";

        // Right Arm
        public const string mixamorig_RightShoulder = "mixamorig_RightShoulder";
        public const string mixamorig_RightArm = "mixamorig_RightArm";
        public const string mixamorig_RightForeArm = "mixamorig_RightForeArm";
        public const string mixamorig_RightHand = "mixamorig_RightHand";

        // Right Hand Fingers
        public const string mixamorig_RightHandThumb1 = "mixamorig_RightHandThumb1";
        public const string mixamorig_RightHandThumb2 = "mixamorig_RightHandThumb2";
        public const string mixamorig_RightHandThumb3 = "mixamorig_RightHandThumb3";
        public const string mixamorig_RightHandThumb4 = "mixamorig_RightHandThumb4";
        public const string mixamorig_RightHandIndex1 = "mixamorig_RightHandIndex1";
        public const string mixamorig_RightHandIndex2 = "mixamorig_RightHandIndex2";
        public const string mixamorig_RightHandIndex3 = "mixamorig_RightHandIndex3";
        public const string mixamorig_RightHandIndex4 = "mixamorig_RightHandIndex4";
        public const string mixamorig_RightHandMiddle1 = "mixamorig_RightHandMiddle1";
        public const string mixamorig_RightHandMiddle2 = "mixamorig_RightHandMiddle2";
        public const string mixamorig_RightHandMiddle3 = "mixamorig_RightHandMiddle3";
        public const string mixamorig_RightHandMiddle4 = "mixamorig_RightHandMiddle4";
        public const string mixamorig_RightHandRing1 = "mixamorig_RightHandRing1";
        public const string mixamorig_RightHandRing2 = "mixamorig_RightHandRing2";
        public const string mixamorig_RightHandRing3 = "mixamorig_RightHandRing3";
        public const string mixamorig_RightHandRing4 = "mixamorig_RightHandRing4";
        public const string mixamorig_RightHandPinky1 = "mixamorig_RightHandPinky1";
        public const string mixamorig_RightHandPinky2 = "mixamorig_RightHandPinky2";
        public const string mixamorig_RightHandPinky3 = "mixamorig_RightHandPinky3";
        public const string mixamorig_RightHandPinky4 = "mixamorig_RightHandPinky4";
    }

    public class Human : Primate<HUMAN_ACTION>
    {
        Dictionary<string, TextNamePlate> _dicTextNamePlates = new Dictionary<string, TextNamePlate>();
        bool _isInit = false;

        public Human(string name, AnimRig aniRig) : base(name, aniRig, HUMAN_ACTION.A_T_POSE)
        {
        }

        public override HUMAN_ACTION RandomAction => (HUMAN_ACTION)Rand.NextInt(0, (int)(HUMAN_ACTION.RANDOM - 1));

        public override void SetMotionImmediately(HUMAN_ACTION action)
        {
            if (action == HUMAN_ACTION.RANDOM) action = RandomAction;
            SetMotion(HumanActions.ActionMap[action], transitionDuration: 0.0f);
        }

        public override void SetMotion(HUMAN_ACTION action)
        {
            if (action == HUMAN_ACTION.RANDOM) action = RandomAction;
            SetMotion(HumanActions.ActionMap[action]);
        }

        public override void SetMotionOnce(HUMAN_ACTION action)
        {
            if (action == HUMAN_ACTION.RANDOM) action = RandomAction;
            SetMotionOnce(HumanActions.ActionMap[action]);
        }

        protected override string GetActionName(HUMAN_ACTION action)
        {
            return action.IsCommonAction() ? action.GetName() : HumanActions.GetActionName(action);
        }

        public void AddBoneTagNamePlate(Camera camera, string text, string boneName)
        {
            TextNamePlate textNamePlate = new TextNamePlate(camera, text);
            textNamePlate.Width *= 0.35f;
            textNamePlate.Height *= 0.35f;
            textNamePlate.Offset = Vertex3f.Zero;

            if (!_dicTextNamePlates.ContainsKey(boneName))
            {
                _dicTextNamePlates.Add(boneName, textNamePlate);
            }
            else
            {
                _dicTextNamePlates[boneName] = textNamePlate;
            }
        }

        public override void Render(Camera camera, Matrix4x4f vp, AnimateShader ashader, StaticShader sshader,
                    bool isSkinVisible = true, bool isBoneVisible = false, bool isBoneParentCurrentVisible = false)
        {
            base.Render(camera, vp, ashader, sshader, isSkinVisible, isBoneVisible, isBoneParentCurrentVisible);

            // OpenGL 상태 설정 (블렌딩 활성화, 깊이 테스트 비활성화)
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            Gl.Disable(EnableCap.DepthTest);

            // 이름표 렌더링
            foreach (var textNamePlate in _dicTextNamePlates)
            {
                TextNamePlate characterName = textNamePlate.Value;
                if (characterName.IsVisible)
                {
                    characterName.Render();
                }
            }

            // OpenGL 상태 복원
            Gl.Enable(EnableCap.DepthTest);
            Gl.Disable(EnableCap.Blend);
        }

        public override void Update(int deltaTime)
        {
            base.Update(deltaTime);

            foreach (var item in _dicTextNamePlates)
            {
                string boneName = item.Key;
                TextNamePlate textNamePlate = item.Value;
                Bone bone = _aniRig.Armature[boneName];
                textNamePlate.WorldPosition = (ModelMatrix * _animator.RootTransforms[bone.Index]).Position;

                if (!_isInit)
                {
                    textNamePlate.Refresh();
                    _isInit = false;
                }
                textNamePlate.Update(deltaTime);
            }
        }
    }
}