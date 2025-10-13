using AutoGenEnums;
using Common.Abstractions;
using OpenGL;
using Shader;
using System.Collections.Generic;
using Ui3d;
using ZetaExt;

namespace Animate
{
    public class Human : Primate<HUMAN_ACTION>
    {
        CharacterNamePlate _characterNameplate;
        HealthBar _healthBar;
        List<DamageNumber> _damageNumbers = new List<DamageNumber>();
        private ChatBubble _chatBubble;

        public Human(string name, AnimRig aniRig) : base(name, aniRig, HUMAN_ACTION.A_T_POSE)
        {
        }

        public override HUMAN_ACTION RandomAction => (HUMAN_ACTION)Rand.NextInt(0, (int)(HUMAN_ACTION.RANDOM - 1));

        public CharacterNamePlate CharacterNameplate { get => _characterNameplate; set => _characterNameplate = value; }
        public HealthBar HealthBar { get => _healthBar; set => _healthBar = value; }

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

        /// <summary>
        /// 데미지 숫자를 표시합니다.
        /// </summary>
        /// <param name="camera">카메라</param>
        /// <param name="damage">데미지 값</param>
        /// <param name="isCritical">크리티컬 여부</param>
        /// <param name="isHeal">힐 여부</param>
        public void ShowDamage(Camera camera, float damage, bool isCritical = false, bool isHeal = false)
        {
            // 머리 본 위치 가져오기
            Bone bone = _aniRig.Armature["mixamorig_Head"];
            Vertex3f pos = (ModelMatrix * _animator.RootTransforms[bone.Index]).Position;

            // 약간 위쪽으로 오프셋
            pos.z += 0.4f;

            // 데미지 숫자 생성
            var damageNumber = new DamageNumber(camera, damage, pos, isCritical, isHeal);
            _damageNumbers.Add(damageNumber);
        }

        /// <summary>
        /// 채팅 말풍선을 표시합니다.
        /// </summary>
        public void ShowChat(Camera camera, string message, int displayDuration = 3000)
        {
            // 기존 말풍선 제거
            if (_chatBubble != null)
            {
                _chatBubble.Dispose();
            }

            // 머리 본 위치 가져오기
            Bone bone = _aniRig.Armature["mixamorig_Head"];
            Vertex3f pos = (ModelMatrix * _animator.RootTransforms[bone.Index]).Position;

            // 채팅 말풍선 생성
            _chatBubble = new ChatBubble(camera, message, pos, displayDuration);
            _chatBubble.Offset = new Vertex3f(0, 0, displayDuration > 0 ? 0.5f : 0.3f);
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
            if (_characterNameplate == null)
            {
                _characterNameplate = new CharacterNamePlate(camera, characterName: _name);
                _characterNameplate.WorldPosition = Transform.Position;
                _characterNameplate.Offset = new Vertex3f(0, 0, 0.35f);
            }
            else
            {
                _characterNameplate.Render();
            }

            // 체력바 렌더링
            if (_healthBar == null)
            {
                _healthBar = new HealthBar(camera, maxHP: 100, currentHP: 75);
                _healthBar.WorldPosition = Transform.Position;
                _healthBar.Offset = new Vertex3f(0, 0, 0.30f);
            }
            else
            {
                _healthBar.Render();
            }

            // 데미지 숫자들 렌더링
            foreach (var damageNumber in _damageNumbers)
            {
                if (damageNumber.IsVisible)
                {
                    damageNumber.Render();
                }
            }

            // 채팅 말풍선 렌더링
            if (_chatBubble != null && _chatBubble.IsVisible)
            {
                _chatBubble.Render();
            }

            // OpenGL 상태 복원
            Gl.Enable(EnableCap.DepthTest);
            Gl.Disable(EnableCap.Blend);
        }

        public override void Update(int deltaTime)
        {
            base.Update(deltaTime);

            // 머리 본 위치 계산
            Bone bone = _aniRig.Armature["mixamorig_Head"];
            Vertex3f pos = (ModelMatrix * _animator.RootTransforms[bone.Index]).Position;

            // 이름표 업데이트
            if (_characterNameplate != null)
            {
                _characterNameplate.WorldPosition = pos;
                _characterNameplate.Update(deltaTime);
            }

            // 체력바 업데이트
            if (_healthBar != null)
            {
                _healthBar.WorldPosition = pos;
                _healthBar.Update(deltaTime);
            }

            // 데미지 숫자들 업데이트 및 정리
            for (int i = _damageNumbers.Count - 1; i >= 0; i--)
            {
                _damageNumbers[i].Update(deltaTime);

                // 애니메이션이 끝난 것은 제거
                if (_damageNumbers[i].IsFinished)
                {
                    _damageNumbers[i].Dispose();
                    _damageNumbers.RemoveAt(i);
                }
            }

            // 채팅 말풍선 업데이트
            if (_chatBubble != null)
            {
                _chatBubble.WorldPosition = pos;
                _chatBubble.Update(deltaTime);

                if (_chatBubble.IsFinished)
                {
                    _chatBubble.Dispose();
                    _chatBubble = null;
                }
            }
        }
    }
}
