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
    public class Human : Primate<HUMAN_ACTION>
    {
        TextNamePlate _textNamePlate;
        HealthBar _healthBar;
        List<DamageNumber> _damageNumbers = new List<DamageNumber>();
        List<FloatingItem> _floatingItems = new List<FloatingItem>();
        ChatBubble _chatBubble;
        List<HitMarker> _hitMarkers = new List<HitMarker>();
        InteractionPrompt _interactionPrompt;

        public Human(string name, AnimRig aniRig) : base(name, aniRig, HUMAN_ACTION.A_T_POSE)
        {
        }

        public override HUMAN_ACTION RandomAction => (HUMAN_ACTION)Rand.NextInt(0, (int)(HUMAN_ACTION.RANDOM - 1));

        public TextNamePlate TextNamePlate { get => _textNamePlate; set => _textNamePlate = value; }
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

        public void ShowInteraction(Camera camera, string keyText = "E", string actionText = "대화하기")
        {
            if (_interactionPrompt == null)
            {
                _interactionPrompt = new InteractionPrompt(camera, keyText, actionText);
                _interactionPrompt.WorldPosition = Transform.Position;
                _interactionPrompt.Offset = new Vertex3f(0, 0, 0.25f);
            }
            else
            {
                _interactionPrompt.KeyText = keyText;
                _interactionPrompt.ActionText = actionText;
                _interactionPrompt.IsActive = true;
            }
        }

        public void HideInteraction()
        {
            if (_interactionPrompt != null)
            {
                _interactionPrompt.IsActive = false;
            }
        }

        public void ShowHitMarker(Camera camera, Vertex3f position, bool isCritical = false)
        {
            var hitMarker = new HitMarker(camera, isCritical);
            hitMarker.WorldPosition = position;
            _hitMarkers.Add(hitMarker);
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
            Bone bone = _aniRig.Armature["mixamorig_Head"];
            Vertex3f pos = (ModelMatrix * _animator.RootTransforms[bone.Index]).Position;
            pos.z += 0.4f;

            var damageNumber = new DamageNumber(camera, damage, pos, isCritical, isHeal);
            _damageNumbers.Add(damageNumber);
        }

        /// <summary>
        /// 떠오르는 아이템을 표시합니다.
        /// </summary>
        /// <param name="camera">카메라</param>
        /// <param name="itemIcon">아이템 아이콘 비트맵</param>
        /// <param name="itemName">아이템 이름</param>
        /// <param name="count">획득 개수</param>
        public void ShowFloatingItem(Camera camera, Bitmap itemIcon, string itemName = "", int count = 1)
        {
            // 머리 위치에서 시작
            Bone bone = _aniRig.Armature["mixamorig_Head"];
            Vertex3f pos = (ModelMatrix * _animator.RootTransforms[bone.Index]).Position;
            pos.z += 0.3f; // 머리 위에서 시작

            var floatingItem = new FloatingItem(camera, itemIcon, itemName, count);
            floatingItem.WorldPosition = pos;
            _floatingItems.Add(floatingItem);
        }

        /// <summary>
        /// 채팅 말풍선을 표시합니다.
        /// </summary>
        public void ShowChat(Camera camera, string message, int displayDuration = 3000)
        {
            if (_chatBubble != null)
            {
                _chatBubble.Dispose();
            }

            Bone bone = _aniRig.Armature["mixamorig_Head"];
            Vertex3f pos = (ModelMatrix * _animator.RootTransforms[bone.Index]).Position;

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
            if (_textNamePlate == null)
            {
                _textNamePlate = new TextNamePlate(camera, characterName: _name);
                _textNamePlate.WorldPosition = Transform.Position;
                _textNamePlate.Offset = new Vertex3f(0, 0, 0.35f);
            }
            else
            {
                _textNamePlate.Render();
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

            // 떠오르는 아이템들 렌더링
            foreach (var floatingItem in _floatingItems)
            {
                if (floatingItem.IsVisible)
                {
                    floatingItem.Render();
                }
            }

            // 채팅 말풍선 렌더링
            if (_chatBubble != null && _chatBubble.IsVisible)
            {
                _chatBubble.Render();
            }

            // 히트 마커 렌더링
            foreach (var hitMarker in _hitMarkers)
            {
                if (hitMarker.IsVisible)
                {
                    hitMarker.Render();
                }
            }

            // 상호작용 프롬프트 렌더링
            if (_interactionPrompt != null && _interactionPrompt.IsVisible)
            {
                _interactionPrompt.Render();
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
            if (_textNamePlate != null)
            {
                _textNamePlate.WorldPosition = pos;
                _textNamePlate.Update(deltaTime);
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

                if (_damageNumbers[i].IsFinished)
                {
                    _damageNumbers[i].Dispose();
                    _damageNumbers.RemoveAt(i);
                }
            }

            // 떠오르는 아이템들 업데이트 및 정리
            for (int i = _floatingItems.Count - 1; i >= 0; i--)
            {
                _floatingItems[i].Update(deltaTime);

                if (_floatingItems[i].IsComplete)
                {
                    _floatingItems[i].Dispose();
                    _floatingItems.RemoveAt(i);
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

            // 히트 마커 업데이트 및 정리
            for (int i = _hitMarkers.Count - 1; i >= 0; i--)
            {
                _hitMarkers[i].Update(deltaTime);
                if (_hitMarkers[i].IsComplete)
                {
                    _hitMarkers[i].Dispose();
                    _hitMarkers.RemoveAt(i);
                }
            }

            // 상호작용 프롬프트 업데이트
            if (_interactionPrompt != null && _interactionPrompt.IsActive)
            {
                _interactionPrompt.WorldPosition = pos;
                _interactionPrompt.Update(deltaTime);
            }
        }
    }
}