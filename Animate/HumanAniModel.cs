using Geometry;
using OpenGL;
using ZetaExt;

namespace Animate
{
    public class HumanAniModel : Mammal
    {
        public enum ACTION
        { 
            IDLE, WALK, RUN, SLOW_RUN, JUMP, WALK_BACK, LEFT_STRAFE_WALK, RIGHT_STRAFE_WALK, GUN_PLAY,
            AXE_ATTACK,
            STOP,
            T_POSE,
            COUNT
        };

        public enum HAND_ITEM
        {
            NONE,
            GUN,
            AXE,
            COUNT
        }

        ACTION _prevMotion = ACTION.IDLE;
        ACTION _curMotion = ACTION.IDLE;
        HAND_ITEM _curHandItem = HAND_ITEM.NONE;

        public HAND_ITEM CurrentHandItem
        {
            get => _curHandItem;
            set => _curHandItem = value;
        }

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
                    _collider.LowerBound = _transform.Position + _xmlDae.LowerCollider;
                    _collider.UpperBound = _transform.Position + _xmlDae.UpperCollider;
                }
                return _collider;
            }
        } 

        public HumanAniModel(string name, AnimateEntity model, AniDae xmlDae) : base(name, model, xmlDae)
        {
           
        }

        public void SetMotionOnce(string motionName, ACTION nextAction)
        {
            Motion motion = _xmlDae.Motions.GetMotion(motionName);

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
                motion = _xmlDae.Motions.DefaultMotion;

            if (motion != null)
                _animator.SetMotion(motion);
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

        public void HandAction()
        {
            switch (_curHandItem)
            {
                case HAND_ITEM.NONE:
                    SetMotion(ACTION.IDLE);
                    break;
                case HAND_ITEM.GUN:
                    SetMotion(ACTION.GUN_PLAY);
                    break;
                case HAND_ITEM.AXE:
                    SetMotion(ACTION.AXE_ATTACK);
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

            if (_curMotion == ACTION.IDLE)
            {
                SetMotion("Breathing Idle");
                UnfoldHand(BODY_PART.LeftHand);
                UnfoldHand(BODY_PART.RightHand);
            }
            else if (_curMotion == ACTION.WALK)
            {
                SetMotion("Walking");
            }
            else if (_curMotion == ACTION.T_POSE)
            {
                SetMotion("a-T-Pose");
            }
            else if (_curMotion == ACTION.SLOW_RUN)
            {
                SetMotion("Slow Run");
            }
            else if (_curMotion == ACTION.JUMP)
            {
                SetMotion("Jump");
            }
            else if (_curMotion == ACTION.RUN)
            {
                SetMotion("Running");
            }
            else if (_curMotion == ACTION.WALK_BACK)
            {
                SetMotion("Walking Backwards");
            }
            else if (_curMotion == ACTION.LEFT_STRAFE_WALK)
            {
                SetMotion("Left Strafe Walk");
            }
            else if (_curMotion == ACTION.RIGHT_STRAFE_WALK)
            {
                SetMotion("Right Strafe Walk");
            }
            else if (_curMotion == ACTION.AXE_ATTACK)
            {
                FoldHand(BODY_PART.LeftHand);
                FoldHand(BODY_PART.RightHand);
                _curHandItem = HAND_ITEM.AXE;
                SetMotionOnce("Axe Attack Downward", "Axe Standing Idle");
                _rightHandEntity?.LocalBindTransform(sx: 100, sy: 100, sz: 100, rotx: 180, roty: -90, rotz: 0);
            }
            else if (_curMotion == ACTION.GUN_PLAY)
            {
                FoldHand(BODY_PART.LeftHand);
                FoldHand(BODY_PART.RightHand);
                _curHandItem = HAND_ITEM.GUN;
                SetMotionOnce("Gunplay", ACTION.STOP);
                _rightHandEntity?.LocalBindTransform(sx: 100, sy: 100, sz: 100, rotx: 130, roty: 180, rotz: -90);
            }
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
