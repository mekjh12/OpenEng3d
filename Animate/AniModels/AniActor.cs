using AutoGenEnums;
using Common.Abstractions;
using Model3d;
using OpenGL;
using Shader;
using System;
using System.Collections.Generic;
using System.IO;
using ZetaExt;

namespace Animate
{
    public abstract class AniActor
    {
        protected string _name;
        protected AniRig _aniRig;

        protected Action _updateBefore;
        protected Action _updateAfter;
        protected Transform _transform;

        Dictionary<string, AnimateEntity> _models;
        protected Bone _rootBone;
        protected Animator _animator;

        public AniRig AniRig => _aniRig;

        public string Name => _name;

        public Transform Transform => _transform;

        #region 디버깅용
        public enum RenderingMode { Animation, BoneWeight, Static, None, Count };
        PolygonMode _polygonMode = PolygonMode.Fill;
        RenderingMode _renderingMode = RenderingMode.Animation;
        int _selectedBoneIndex = 0;
        float _axisLength = 10.3f;
        float _drawThick = 1.0f;

        public int BoneCount => _aniRig.DicBones.Count;

        public Motion CurrentMotion => _animator.CurrentMotion;

        public int SelectedBoneIndex
        {
            get => _selectedBoneIndex;
            set => _selectedBoneIndex = value;
        }

        public PolygonMode PolygonMode
        {
            get => _polygonMode;
            set => _polygonMode = value;
        }

        public RenderingMode RenderMode
        {
            get => _renderingMode;
            set => _renderingMode = value;
        }

        public void PopPolygonMode()
        {
            _polygonMode++;
            if (_polygonMode >= (PolygonMode)6915) _polygonMode = (PolygonMode)6912;
        }

        public void PopPolygonModeMode()
        {
            _renderingMode++;
            if (_renderingMode == RenderingMode.Count - 1) _renderingMode = 0;
        }
        #endregion

        /// <summary>
        /// 본이름으로부터 본을 가져온다.
        /// </summary>
        /// <param name="boneName"></param>
        /// <returns></returns>
        public Bone GetBoneByName(string boneName) => _aniRig.Armature[boneName];

        /// <summary>
        /// Animator를 가져온다.
        /// </summary>
        public Animator Animator => _animator;

        /// <summary>
        /// 모션의 총 시간 길이를 가져온다.
        /// </summary>
        public float MotionTime => _animator.MotionTime;

        /// <summary>
        /// Entity들을 모두 가져온다.
        /// </summary>
        public Dictionary<string, AnimateEntity> Entities => _models;

        /// <summary>
        /// 루트본을 가져온다.
        /// </summary>
        public Bone RootBone => _rootBone;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="model"></param>
        /// <param name="aniRig"></param>
        public AniActor(string name, AnimateEntity model, AniRig aniRig)
        {
            _models = new Dictionary<string, AnimateEntity>();
            _models.Add(name, model);

            _aniRig = aniRig;
            _rootBone = aniRig.Armature.RootBone;
            _animator = new Animator(this);
            _transform = new Transform();

        }

        public virtual void SetMotionOnce(ACTION motion)
        {


        }


        public void SetMotionOnce(string motionName, ACTION nextAction)
        {
            Motion motion = _aniRig.Motions.GetMotion(nextAction);

            _animator.OnceFinised = () =>
            {
                if (nextAction == ACTION.STOP)
                {
                    _animator.Stop();
                }
                else
                {
                    //SetMotion(nextAction);
                }
                _animator.OnceFinised = null;
            };

            if (motion == null)
                motion = _aniRig.Motions.DefaultMotion;

            if (motion != null)
                _animator.SetMotion(motion);
        }


        /// <summary>
        /// 모션을 설정한다.
        /// </summary>
        /// <param name="motionName"></param>
        protected void SetMotion(string motionName)
        {
            _animator.OnceFinised = null;

            Motion motion = _aniRig.Motions.GetMotion(motionName);

            if (motion == null)
                motion = _aniRig.Motions.DefaultMotion;

            if (motion != null)
                _animator.SetMotion(motion);
        }

        /// <summary>
        /// 업데이트를 통하여 애니메이션 행렬을 업데이트한다.
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Update(int deltaTime)
        {
            // 애니메이션 업데이트 전에 호출할 수 있는 콜백 함수
            if (_updateBefore != null)
            {
                _updateBefore();
            }

            // 애니메이션 업데이트
            _animator.Update(0.001f * deltaTime);

            // 애니메이션 업데이트 후에 호출할 수 있는 콜백 함수
            if (_updateAfter != null)
            {
                _updateAfter();
            }
        }

        public void Render(Camera camera, StaticShader staticShader, AnimateShader ashader, 
            bool isSkinVisible = true, bool isBoneVisible = false, bool isBoneParentCurrentVisible = false)
        {
            Matrix4x4f[] finalAnimatedBoneMatrices = _animator.AnimatedTransforms;

            int index = 0;
            foreach (KeyValuePair<string, AnimateEntity> item in _models)
            {
                AnimateEntity entity = item.Value;
                Matrix4x4f modelMatrix = entity.ModelMatrix;

                if (isSkinVisible) // 스킨
                {
                    Gl.PolygonMode(MaterialFace.FrontAndBack, _polygonMode);
                    Gl.Disable(EnableCap.CullFace);
                    if (_renderingMode == RenderingMode.Animation)
                    {
                        Renderer3d.Render(ashader, _transform.Matrix4x4f, finalAnimatedBoneMatrices, entity, camera);
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

                // 애니메이션 뼈대 렌더링
                if (isBoneVisible)
                {
                    int ind = 0;
                    foreach (Matrix4x4f jointTransform in AnimatedTransforms)
                    {
                        if (ind >= 52) //52이상은 추가한 뼈들이다.
                        {
                            Renderer3d.RenderLocalAxis(staticShader, camera, size: jointTransform.Position.Norm() * _axisLength,
                                thick: 5.0f*_drawThick, _transform.Matrix4x4f * modelMatrix * jointTransform);
                        }
                        else
                        {
                            Renderer3d.RenderLocalAxis(staticShader, camera, size: jointTransform.Position.Norm() * _axisLength,
                                thick: _drawThick, _transform.Matrix4x4f * modelMatrix * jointTransform);
                        }
                        ind++;
                    }
                }

                Renderer3d.RenderLocalAxis(staticShader, camera, size: 100.0f, thick: _drawThick, _rootBone.AnimatedTransform * _transform.Matrix4x4f);

                // 정지 뼈대
                //foreach (Matrix4x4f jointTransform in _aniModel.InverseBindPoseTransforms)
                {
                    //Renderer.RenderLocalAxis(_shader, camera, size: _axisLength, thick: _drawThick, entityModel * jointTransform.Inverse);
                }

                index++;
            }
        }

        /// <summary>
        /// * 애니매이션에서 뼈들의 뼈공간 ==> 캐릭터 공간으로의 변환 행렬<br/>
        /// * 뼈들의 포즈를 렌더링하기 위하여 사용할 수 있다.<br/>
        /// </summary>
        public Matrix4x4f[] AnimatedTransforms
        {
            get
            {
                Matrix4x4f[] jointMatrices = new Matrix4x4f[BoneCount];
                foreach (KeyValuePair<string, Bone> item in _aniRig.DicBones)
                {
                    Bone bone = item.Value;
                    if (bone.Index >= 0)
                        jointMatrices[bone.Index] = bone.AnimatedTransform;
                }
                return jointMatrices;
            }
        }

        protected void AddEntity(string name, AnimateEntity entity)
        {
            if (_models.ContainsKey(name))
            {
                _models.Remove(name);
                _models.Add(name, entity);
            }
            else
            {
                _models.Add(name, entity);
            }
        }

        protected void Remove(string name)
        {
            if (_models.ContainsKey(name))
            {
                _models.Remove(name);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="expandValue"></param>
        /// <returns></returns>
        public Entity Attach(string fileName, float expandValue = 0.01f)
        {
            string name = Path.GetFileNameWithoutExtension(fileName);
            List<TexturedModel> texturedModels = _aniRig.WearCloth(fileName, expandValue);
            AnimateEntity clothEntity = new AnimateEntity("aniModel_" + name, texturedModels[0]);
            AddEntity(name, clothEntity);
            return clothEntity;
        }

        public Entity Attach(string name, TexturedModel texturedModel)
        {
            AnimateEntity clothEntity = new AnimateEntity(name, texturedModel);
            AddEntity(name, clothEntity);
            return clothEntity;
        }

    }
}
