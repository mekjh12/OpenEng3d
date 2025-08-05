using Animate.AniModels;
using AutoGenEnums;
using Common.Abstractions;
using Model3d;
using OpenGL;
using Shader;
using System;
using System.Collections.Generic;

namespace Animate
{
    public abstract partial class AniActor
    {
        protected HUMAN_ACTION _prevMotion = HUMAN_ACTION.BREATHING_IDLE; // 이전 모션 상태
        protected HUMAN_ACTION _curMotion = HUMAN_ACTION.BREATHING_IDLE; // 현재 모션 상태

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

        // 컴포넌트들
        protected AnimationComponent _animationComponent; // 애니메이션 컴포넌트
        protected TransformComponent _transformComponent; // 트랜스폼 컴포넌트


        BlendMotion _blendMotion;

        // 속성
        public string Name => _name;
        public AniRig AniRig => _aniRig;
        public Animator Animator => _animator;
        public Transform Transform => _transform;
        public float MotionTime => _animator.MotionTime;
        public Motion CurrentMotion => _animator.CurrentMotion;
        public Matrix4x4f[] AnimatedTransforms => _animator.AnimatedTransforms;
        public Matrix4x4f ModelMatrix => _transform.Matrix4x4f;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="name">액터 이름</param>
        /// <param name="aniRig">애니메이션 리그</param>
        public AniActor(string name, AniRig aniRig)
        {
            _items = new Dictionary<string, ItemAttachment>();
            _items.Clear();

            _aniRig = aniRig;

            _animator = new Animator(aniRig.Armature.RootBone);
            _transform = new Transform();
        }

        /// <summary>
        /// 지정한 모션을 한 번만 실행하고 모션을 복귀한다.
        /// </summary>
        /// <param name="motionName">모션 이름</param>
        public void SetMotionOnce(string motionName)
        {
            Motion curMotion = _aniRig.Motions.GetMotion(Actions.ActionMap[_curMotion]);
            Motion nextMotion = _aniRig.Motions.GetMotion(motionName);
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
        protected void SetMotion(string motionName, MotionCache motionCache, float blendingInterval = 0.2f)
        {
            _animator.OnceFinished = null;

            Motion motion = _aniRig.Motions.GetMotion(motionName);

            if (motion == null)
            {
                motion = _aniRig.Motions.DefaultMotion;
            }
            else
            {
                _animator.SetMotion(motion, motionCache, blendingInterval);
            }

            _animator.Play();
        }

        public void SetBlendMotion(float blendFactor)
        {
            if (_blendMotion == null)
            {
            }

            Motion motion1 = _aniRig.Motions.GetMotion("Walking");
            Motion motion2 = _aniRig.Motions.GetMotion("Fast Run");

            _blendMotion = new BlendMotion(_animator.BoneTraversalOrder, motion1, motion2, 1.0f, 2.0f, blendFactor);

            _animator.OnceFinished = null;
            _animator.SetMotion(_blendMotion, null, 0.0f);

            _animator.Play();
        }


        /// <summary>
        /// 업데이트를 통하여 애니메이션 행렬을 업데이트한다.
        /// </summary>
        /// <param name="deltaTime">델타 시간</param>
        public void Update(int deltaTime)
        {
            // 애니메이션 업데이트 전에 호출할 수 있는 콜백 함수
            if (_updateBefore != null)
            {
                _updateBefore();
            }

            _animator.Update(0.001f * deltaTime);

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
                    //Renderer.Render(staticShader, entity, camera);
                }
                Gl.Enable(EnableCap.CullFace);
            }
        }
    }
}