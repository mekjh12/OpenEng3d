using AutoGenEnums;
using Geometry;
using OpenGL;
using ZetaExt;

namespace Animate
{
    public class Human : Primate
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

        HAND_ITEM _curHandItem = HAND_ITEM.NONE;


        public Vertex3f HipPosition
        {
            get
            {
                int index = GetBoneByName("mixamorig_Hip").Index;
                Matrix4x4f m = _transform.Matrix4x4f * AnimatedTransforms[index];
                return m.Column3.xyz();
            }
        }


        public Vertex3f RightHandPosition
        {
            get
            {
                int index = GetBoneByName("mixamorig_RightHand").Index;
                Matrix4x4f m = _transform.Matrix4x4f * AnimatedTransforms[index];
                return m.Column3.xyz();
            }
        }

        public Vertex3f HeadPosition
        {
            get
            {
                int index = GetBoneByName("mixamorig_Head").Index;
                Matrix4x4f m = _transform.Matrix4x4f * AnimatedTransforms[index];
                return m.Column3.xyz();
            }
        }

        public Vertex3f LeftFootPosition
        {
            get
            {
                int index = GetBoneByName("mixamorig_LeftFoot").Index;
                Matrix4x4f m = _transform.Matrix4x4f * AnimatedTransforms[index];
                return m.Column3.xyz();
            }
        }

        public Vertex3f RightFootPosition
        {
            get
            {
                int index = GetBoneByName("mixamorig_RightFoot").Index;
                Matrix4x4f m = _transform.Matrix4x4f * AnimatedTransforms[index];
                return m.Column3.xyz();
            }
        }

        public Human(string name, AniRig aniRig) : base(name, aniRig)
        {

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

        /// <summary>
        /// 랜덤한 모션을 지정해 준다.
        /// </summary>
        public ACTION RandomAction => (ACTION)Rand.NextInt(0, (int)(ACTION.RANDOM - 1));

        /// <summary>
        /// 즉시 캐릭터의 모션을 지정해 준다.
        /// </summary>
        /// <param name="action"></param>
        public void SetMotionImmediately(ACTION action)
        {
            if (action == ACTION.RANDOM) action = RandomAction;

            _animator.Play();
            _prevMotion = _curMotion;
            _curMotion = action;

            // 모션을 지정해 준다.
            SetMotion(Actions.ActionMap[action], blendingInterval: 0.0f);
        }

        /// <summary>
        /// 캐릭터의 모션을 지정해 준다.
        /// </summary>
        /// <param name="action"></param>
        public void SetMotion(ACTION action)
        {
            if (action == ACTION.RANDOM) action = RandomAction;

            _animator.Play();

            _prevMotion = _curMotion;
            _curMotion = action;

            // 모션을 지정해 준다.
            SetMotion(Actions.ActionMap[action]);
        }

        /// <summary>
        /// 다음 모션을 한번만 하고 이후에는 이전 모션으로 돌아간다.
        /// </summary>
        /// <param name="action"></param>
        public void SetMotionOnce(ACTION action)
        {
            if (action == ACTION.RANDOM) action = RandomAction;
            _animator.Play();
            SetMotionOnce(Actions.ActionMap[action]);
        }
    }
}