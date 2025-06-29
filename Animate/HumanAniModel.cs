using AutoGenEnums;
using Geometry;
using OpenGL;
using ZetaExt;

namespace Animate
{
    public class HumanAniModel : Primate
    {
        public enum HAND_ITEM
        {
            NONE,
            GUN,
            AXE,
            COUNT
        }

        public HAND_ITEM CurrentHandItem
        {
            get => _curHandItem;
            set => _curHandItem = value;
        }

        ACTION _prevMotion = ACTION.BREATHING_IDLE;
        ACTION _curMotion = ACTION.BREATHING_IDLE;
        HAND_ITEM _curHandItem = HAND_ITEM.NONE;

        AABB _collider;

        public AABB Collider
        {
            get
            {
                if (_collider == null)
                {
                    _collider = new AABB();
                }
                else
                {
                    _collider.LowerBound = _transform.Position + _aniDae.LowerCollider;
                    _collider.UpperBound = _transform.Position + _aniDae.UpperCollider;
                }
                return _collider;
            }
        }



        public Vertex3f HipPosition
        {
            get
            {
                int index = GetBoneByName("mixamorig_Hip").Index;
                Matrix4x4f m = _transform.Matrix4x4f * BoneAnimationTransforms[index];
                return m.Column3.xyz();
            }
        }


        public Vertex3f RightHandPosition
        {
            get
            {
                int index = GetBoneByName("mixamorig_RightHand").Index;
                Matrix4x4f m = _transform.Matrix4x4f * BoneAnimationTransforms[index];
                return m.Column3.xyz();
            }
        }

        public Vertex3f HeadPosition
        {
            get
            {
                int index = GetBoneByName("mixamorig_Head").Index;
                Matrix4x4f m = _transform.Matrix4x4f * BoneAnimationTransforms[index];
                return m.Column3.xyz();
            }
        }

        public Vertex3f LeftFootPosition
        {
            get
            {
                int index = GetBoneByName("mixamorig_LeftFoot").Index;
                Matrix4x4f m = _transform.Matrix4x4f * BoneAnimationTransforms[index];
                return m.Column3.xyz();
            }
        }

        public Vertex3f RightFootPosition
        {
            get
            {
                int index = GetBoneByName("mixamorig_RightFoot").Index;
                Matrix4x4f m = _transform.Matrix4x4f * BoneAnimationTransforms[index];
                return m.Column3.xyz();
            }
        }

        public HumanAniModel(string name, AnimateEntity model, AniDae xmlDae) : base(name, model, xmlDae)
        {
           
        }

        public void SetMotionOnce(string motionName, ACTION nextAction)
        {
            Motion motion = _aniDae.Motions.GetMotion(motionName);

            _animator.OnceFinised = () =>
            {
                if (nextAction == ACTION.STOP)
                {
                    _animator.Stop();
                }
                else
                {
                    SetMotion(nextAction);
                }
                _animator.OnceFinised = null;
            };

            if (motion == null)
                motion = _aniDae.Motions.DefaultMotion;

            if (motion != null)
                _animator.SetMotion(motion);
        }

        public void HandAction()
        {
            switch (_curHandItem)
            {
                case HAND_ITEM.NONE:
                    SetMotion(ACTION.BREATHING_IDLE);
                    break;
                case HAND_ITEM.COUNT:
                    break;
                default:
                    break;
            }
        }

        public void SetMotionImmediately(ACTION action)
        {
            _animator.Play();
            _prevMotion = _curMotion;
            _curMotion = action;
        }

        /// <summary>
        /// 캐릭터의 모션을 지정해 준다.
        /// </summary>
        /// <param name="action"></param>
        public void SetMotion(ACTION action)
        {
            _animator.Play();
            _prevMotion = _curMotion;
            _curMotion = action;

            if (_curMotion == ACTION.BREATHING_IDLE)
            {
                SetMotion("Breathing Idle");
                UnfoldHand(BODY_PART.LeftHand);
                UnfoldHand(BODY_PART.RightHand);
            }
            else if (_curMotion == ACTION.WALKING)
            {
                SetMotion(Actions.WALKING);
            }
            else if (_curMotion == ACTION.A_T_POSE)
            {
                SetMotion( Actions.A_T_POSE);
            }
            else if (_curMotion == ACTION.SLOW_RUN)
            {
                SetMotion(Actions.SLOW_RUN);
            }

            /*
            else if (_curMotion == ACTION.GUN_PLAY)
            {
                FoldHand(BODY_PART.LeftHand);
                FoldHand(BODY_PART.RightHand);
                _curHandItem = HAND_ITEM.GUN;
                SetMotionOnce("Gunplay", ACTION.STOP);
                _rightHandEntity?.LocalBindTransform(sx: 100, sy: 100, sz: 100, rotx: 130, roty: 180, rotz: -90);
            }
            */

        }

        /// <summary>
        /// 캐릭터가 이전에 행동한 모션으로 지정해 준다.
        /// </summary>
        public void SetPrevMotion()
        {
            SetMotion(_prevMotion);
        }

    }
}
