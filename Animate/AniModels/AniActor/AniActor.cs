using Common.Abstractions;
using OpenGL;
using Shader;
using System;
using System.Collections.Generic;

namespace Animate
{
    public abstract partial class AniActor<TAction>: IAniActor where TAction : struct, Enum
    {
        protected TAction _prevMotion;
        protected TAction _curMotion;

        protected string _name; // 액터 이름
        protected AniRig _aniRig; // 애니메이션 리그
        protected Animator _animator; // 애니메이터

        protected Action _updateBefore; // 업데이트 전 콜백
        protected Action _updateAfter; // 업데이트 후 콜백
        protected Transform _transform; // 트랜스폼

        // 아이템은 하나의 뼈대에 부착할 수 있는 텍스쳐 모델들이다.
        // 애니리그에 부착할 텍스쳐 모델 리스트(아이템으로 모자, 옷 등을 부착할 수 있다.)
        // 개선된 아이템 시스템: 아이템 이름을 키로 하고 ItemAttachment를 값으로 하는 딕셔너리
        Dictionary<string, ItemAttachment> _items;

        // 속성
        public string Name => _name;
        public AniRig AniRig => _aniRig;
        public Animator Animator => _animator;
        public Transform Transform => _transform;
        public float MotionTime => _animator.MotionTime;
        public Motionable CurrentMotion => _animator.CurrentMotion;
        public Matrix4x4f[] AnimatedTransforms => _animator.AnimatedTransforms;
        public Matrix4x4f ModelMatrix => _transform.Matrix4x4f;

        // 추상 함수 - 제네릭 타입으로 수정
        public abstract TAction RandomAction { get; }
        public abstract void SetMotionImmediately(TAction action);
        public abstract void SetMotion(TAction action);
        public abstract void SetMotionOnce(TAction action);
        protected abstract string GetActionName(TAction action);

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="name">액터 이름</param>
        /// <param name="aniRig">애니메이션 리그</param>
        /// <param name="defaultAction">기본 액션</param>
        public AniActor(string name, AniRig aniRig, TAction defaultAction)
        {
            _name = name;
            _items = new Dictionary<string, ItemAttachment>();
            _items.Clear();

            _aniRig = aniRig;
            _animator = new Animator(aniRig.Armature.RootBone);
            _transform = new Transform();

            // 기본 액션으로 초기화
            _prevMotion = defaultAction;
            _curMotion = defaultAction;
        }

        /// <summary>
        /// 지정한 모션을 한 번만 실행하고 모션을 복귀한다.
        /// </summary>
        /// <param name="motionName">모션 이름</param>
        public void SetMotionOnce(string motionName)
        {
            Motionable curMotion = _aniRig.Motions.GetMotion(GetActionName(_curMotion));
            Motionable nextMotion = _aniRig.Motions.GetMotion(motionName);
            if (nextMotion == null) nextMotion = _aniRig.Motions.DefaultMotion;
            
            _animator.OnceFinished = () =>
            {
                _animator.SetMotion(curMotion, _aniRig.MotionCache);
                _animator.OnceFinished = null;
            };

            _animator.SetMotion(nextMotion, _aniRig.MotionCache);
            _animator.Play();
        }

        /// <summary>
        /// 모션을 전환한다.
        /// </summary>
        /// <param name="motionName">모션 이름</param>
        /// <param name="blendingInterval">블렌딩 간격</param>
        public void SetMotion(string motionName, float blendingInterval = 0.2f)
        {
            _animator.OnceFinished = null;

            Motionable motion = _aniRig.Motions.GetMotion(motionName);

            if (motion == null)
            {
                motion = _aniRig.Motions.DefaultMotion;
            }
            else
            {
                _animator.SetMotion(motion, _aniRig.MotionCache, blendingInterval);
            }
            
            _animator.Play();
        }

        public void SetBlendMotionFactor(string name, float blendFactor)
        {
            BlendMotion blendMotion = (BlendMotion)_aniRig.GetMotion(name);
            blendMotion.SetBlendFactor(blendFactor);
        }

        /// <summary>
        /// 업데이트를 통하여 애니메이션 행렬을 업데이트한다.
        /// </summary>
        /// <param name="deltaTime">델타 시간</param>
        public void Update(int deltaTime)
        {
            // 애니메이션 업데이트를 위한 시간 간격을 계산한다.
            float duration = 0.001f * deltaTime;

            // 애니메이션 업데이트 전에 호출할 수 있는 콜백 함수
            if (_updateBefore != null)
            {
                _updateBefore();
            }

            _animator.Update(duration);

            if (_animator.CurrentMotion != null)
            {
                // 현재 모션이 속도를 적용해야 하는 경우, 트랜스폼을 업데이트한다.
                if (_animator.CurrentMotion.MovementType != FootStepAnalyzer.MovementType.Stationary)
                {
                    // 모션의 속도를 적용하여 애니메이션을 업데이트한다.
                    float deltaDistance = duration * _animator.CurrentMotion.Speed;

                    FootStepAnalyzer.MovementType movementType = _animator.CurrentMotion.MovementType;
                    if (movementType == FootStepAnalyzer.MovementType.Forward ||
                        movementType == FootStepAnalyzer.MovementType.Backward)
                    {
                        _transform.GoFoward(deltaDistance);
                    }
                    else if (movementType == FootStepAnalyzer.MovementType.Left)
                    {
                        _transform.GoLeft(deltaDistance);
                    }
                    else if (movementType == FootStepAnalyzer.MovementType.Right)
                    {
                        _transform.GoRight(deltaDistance);
                    }
                }

            }

            // 애니메이션 업데이트 후에 호출할 수 있는 콜백 함수
            if (_updateAfter != null)
            {
                _updateAfter();
            }
        }

        /// <summary>
        /// 렌더링을 수행한다.
        /// </summary>
        public void Render(Camera camera, Matrix4x4f vp, AnimateShader ashader, StaticShader sshader,
            bool isSkinVisible = true, bool isBoneVisible = false, bool isBoneParentCurrentVisible = false)
        {
            if (isSkinVisible)
            {
                Gl.PolygonMode(MaterialFace.FrontAndBack, _polygonMode);
                Gl.Disable(EnableCap.CullFace);
                if (_renderingMode == RenderingMode.Animation)
                {
                    Renderer3d.RenderSkinning(ashader, ModelMatrix, vp, _aniRig.TexturedModels, _animator.AnimatedTransforms);
                    //Renderer3d.RenderRigidBody(ashader, ModelMatrix, vp, _items.Values.ToList(),  _animator.RootTransforms);
                }
                else if (_renderingMode == RenderingMode.BoneWeight)
                {
                    //Renderer.Render(boneWeightShader, _boneIndex, _transform.Matrix4x4f, entity, camera);
                }
                else if (_renderingMode == RenderingMode.Static)
                {
                }
                Gl.Enable(EnableCap.CullFace);
            }
        }
    }
}