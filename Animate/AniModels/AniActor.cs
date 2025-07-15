using AutoGenEnums;
using Common.Abstractions;
using Common.Mathematics;
using Geometry;
using Model3d;
using OpenGL;
using Shader;
using System;
using System.Collections.Generic;

namespace Animate
{
    public abstract class AniActor: ITransformable
    {
        protected ACTION _prevMotion = ACTION.BREATHING_IDLE;
        protected ACTION _curMotion = ACTION.BREATHING_IDLE;

        protected string _name;
        protected AniRig _aniRig;
        protected Animator _animator;

        protected Action _updateBefore;
        protected Action _updateAfter;
        protected Transform _transform;

        List<TexturedModel> _items; // 애니리그에 부착할 텍스쳐 모델 리스트(아이템으로 모자, 옷 등을 부착할 수 있다.)

        // 컴포넌트들
        protected AnimationComponent _animationComponent;
        protected TransformComponent _transformComponent;

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
        /// 생성자
        /// </summary>
        /// <param name="model"></param>
        /// <param name="aniRig"></param>
        public AniActor(string name, AniRig aniRig)
        {
            _items = new List<TexturedModel>();
            _items.Clear();

            _aniRig = aniRig;

            _animator = new Animator(aniRig.Armature.RootBone);
            _transform = new Transform();

        }

        public void SetMotionOnce(string motionName)
        {
            Motion curMotion = _aniRig.Motions.GetMotion(Actions.ActionMap[_curMotion]);
            Motion nextMotion = _aniRig.Motions.GetMotion(motionName);
            if (nextMotion == null) nextMotion = _aniRig.Motions.DefaultMotion;

            _animator.OnceFinished = () =>
            {
                _animator.SetMotion(curMotion);
                _animator.OnceFinished = null;
            };

            _animator.SetMotion(nextMotion);
        }


        /// <summary>
        /// 모션을 설정한다.
        /// </summary>
        /// <param name="motionName"></param>
        /// <param name="blendingInterval"></param>
        protected void SetMotion(string motionName, float blendingInterval = 0.2f)
        {
            _animator.OnceFinished = null;

            Motion motion = _aniRig.Motions.GetMotion(motionName);

            if (motion == null)
            {
                motion = _aniRig.Motions.DefaultMotion;
            }
            else
            {
                _animator.SetMotion(motion, blendingInterval);
            }
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
            Matrix4x4f modelMatrix = _transform.Matrix4x4f;

            if (isSkinVisible) // 스킨
            {
                Gl.PolygonMode(MaterialFace.FrontAndBack, _polygonMode);
                Gl.Disable(EnableCap.CullFace);
                if (_renderingMode == RenderingMode.Animation)
                {
                    Renderer3d.Render(ashader, modelMatrix, _items, finalAnimatedBoneMatrices, camera);
                    Renderer3d.Render(ashader, modelMatrix, _aniRig.TexturedModels, finalAnimatedBoneMatrices, camera);
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

            /*
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
            */

            // 정지 뼈대
            //foreach (Matrix4x4f jointTransform in _aniModel.InverseBindPoseTransforms)
            {
                //Renderer.RenderLocalAxis(_shader, camera, size: _axisLength, thick: _drawThick, entityModel * jointTransform.Inverse);
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
                        jointMatrices[bone.Index] = bone.BoneTransforms.AnimatedTransform;
                }
                return jointMatrices;
            }
        }

        public Matrix4x4f LocalBindMatrix => ((ITransformable)_transformComponent).LocalBindMatrix;

        public Vertex3f Size { get => ((ITransformable)_transformComponent).Size; set => ((ITransformable)_transformComponent).Size = value; }
        public Vertex3f Position { get => ((ITransformable)_transformComponent).Position; set => ((ITransformable)_transformComponent).Position = value; }

        public Matrix4x4f ModelMatrix => ((ITransformable)_transformComponent).ModelMatrix;

        public bool IsMoved { get => ((ITransformable)_transformComponent).IsMoved; set => ((ITransformable)_transformComponent).IsMoved = value; }

        public Pose Pose => ((ITransformable)_transformComponent).Pose;

        protected void AddEntity(List<TexturedModel> models)
        {
            _items.AddRange(models);
        }

        protected void AddEntity(string name, TexturedModel model)
        {
            AddEntity(new List<TexturedModel>() { model });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="expandValue"></param>
        /// <returns></returns>
        public Entity Attach(string fileName, float expandValue = 0.01f)
        {
            //string name = Path.GetFileNameWithoutExtension(fileName);
            //List<TexturedModel> texturedModels = _aniRig.WearCloth(fileName, expandValue);
            //AnimateEntity clothEntity = new AnimateEntity("aniModel_" + name, texturedModels[0]);
            //AddEntity(name, texturedModels);
            //return clothEntity;
            return null;
        }

        public void Attach(string name, TexturedModel texturedModel)
        {
            AddEntity(name, texturedModel);
        }

        public void LocalBindTransform(float sx = 1, float sy = 1, float sz = 1, float rotx = 0, float roty = 0, float rotz = 0, float x = 0, float y = 0, float z = 0)
        {
            ((ITransformable)_transformComponent).LocalBindTransform(sx, sy, sz, rotx, roty, rotz, x, y, z);
        }

        public void Scale(float scaleX, float scaleY, float scaleZ)
        {
            ((ITransformable)_transformComponent).Scale(scaleX, scaleY, scaleZ);
        }

        public void Translate(float dx, float dy, float dz)
        {
            ((ITransformable)_transformComponent).Translate(dx, dy, dz);
        }

        public void SetRollPitchAngle(float pitch, float yaw, float roll)
        {
            ((ITransformable)_transformComponent).SetRollPitchAngle(pitch, yaw, roll);
        }

        public void Yaw(float deltaDegree)
        {
            ((ITransformable)_transformComponent).Yaw(deltaDegree);
        }

        public void Roll(float deltaDegree)
        {
            ((ITransformable)_transformComponent).Roll(deltaDegree);
        }

        public void Pitch(float deltaDegree)
        {
            ((ITransformable)_transformComponent).Pitch(deltaDegree);
        }
    }
}
