using AutoGenEnums;
using Common.Abstractions;
using OpenGL;
using Shader;
using System.Xml.Linq;
using Ui3d;
using ZetaExt;

namespace Animate
{
    public class Human : Primate<HUMAN_ACTION>
    {
        CharacterNamePlate _characterNameplate;

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

        public override void Render(Camera camera, Matrix4x4f vp, AnimateShader ashader, StaticShader sshader,
                    bool isSkinVisible = true, bool isBoneVisible = false, bool isBoneParentCurrentVisible = false)
        {
            base.Render(camera, vp, ashader, sshader, isSkinVisible, isBoneVisible, isBoneParentCurrentVisible);

            // 이름표 렌더링
            if (_characterNameplate == null)
            {
                _characterNameplate = new CharacterNamePlate(camera, characterName: _name);

                // 캐릭터 위치에 빌보드 위치 설정
                _characterNameplate.WorldPosition = Transform.Position;
                _characterNameplate.Offset = new Vertex3f(0, 0, 0.35f); // 머리 위
            }
            else
            {
                Gl.Enable(EnableCap.Blend);
                Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                Gl.Disable(EnableCap.DepthTest);

                _characterNameplate.Render();

                Gl.Enable(EnableCap.DepthTest);
                Gl.Disable(EnableCap.Blend);
            }
        }

        public override void Update(int deltaTime)
        {
            base.Update(deltaTime);

            // ★ 빌보드 업데이트 (캐릭터 위치 추적)
            if (_characterNameplate != null)
            {
                Bone bone = _aniRig.Armature["mixamorig_Head"];
                _characterNameplate.WorldPosition = (ModelMatrix * _animator.RootTransforms[bone.Index]).Position;
                _characterNameplate.Update(deltaTime);
            }
        }
    }
}