using Animates;
using AutoGenEnums;
using Common.Abstractions;
using OpenGL;
using Shader;
using System;
using ZetaExt;

namespace Animate
{
    public class Human : Primate<HUMAN_ACTION>
    {
        FootTwoBoneIK _leftFootTwoBoneIK;
        FootTwoBoneIK _rightFootTwoBoneIK;

        ArmTwoBoneIK _leftArmTwoBoneIK;
        ArmTwoBoneIK _rightArmTwoBoneIK;
        
        ThreeBoneRoll _leftArmRollIK;
        ThreeBoneRoll _rightArmRollIK;

        ThreeBoneLookAtIK _threeBoneLookAtIK;

        Vertex3f _testPoint;


        Bone _hip;

        Bone _head;
        Bone _neck;
        Bone _spine2;

        Bone _leftLeg;
        Bone _leftFoot;
        Bone _leftFootToe;
        Bone _rightLeg;
        Bone _rightFoot;
        Bone _rightFootToe;

        Bone _leftArm;
        Bone _leftForeArm;
        Bone _leftHand;
        Bone _rightArm;
        Bone _rightForeArm;
        Bone _rightHand;

        FootGroundInfo _leftFootGroundInfo;
        FootGroundInfo _rightFootGroundInfo;
        bool _isFootIKEnabled;

        // 팔 IK 정보 추가
        bool _isWorldTarget = true;
        ArmTargetInfo _leftArmWorldTargetInfo;
        ArmTargetInfo _rightArmWorldTargetInfo;
        ArmTargetInfo _leftArmCharacterTargetInfo;
        ArmTargetInfo _rightArmCharacterTargetInfo;

        bool _isArmIKEnabled;

        // 계산용 임시 변수
        Vertex3f _leftFootToeWorldPosition;
        Vertex3f _leftFootWorldPosition;
        Vertex3f _leftKneeWorldPosition;
        Vertex3f _rightFootToeWorldPosition;
        Vertex3f _rightFootWorldPosition;
        Vertex3f _rightKneeWorldPosition;
        Vertex3f _footDirection;
        Vertex3f _target;
        
        Vertex3f _leftHandWorldPosition;
        Vertex3f _rightHandWorldPosition;

        Vertex3f _hipWorldPosition;
        Vertex3f _headWorldPosition;

        // 디버깅용
        Vertex3f[] _vertices;
        ColorShader _colorShader;


        public Human(string name, AnimRig aniRig) : base(name, aniRig, HUMAN_ACTION.A_T_POSE)
        {
            _colorShader = ShaderManager.Instance.GetShader<ColorShader>();

            SetupFootIK();
            SetupArmIK();
            SetupHeadIK();

            _hip = _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_Hips];
            _neck = _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_Neck];
            _spine2 = _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_Spine2];
        }

        private void SetupHeadIK()
        {
            _head = _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_Head];
            _neck = _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_Neck];
            _spine2 = _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_Spine2];

            _threeBoneLookAtIK = new ThreeBoneLookAtIK(_head, _neck, _spine2);
            _threeBoneLookAtIK.FirstLookAt.SetAngleLimits(90, 60);
            _threeBoneLookAtIK.SecondLookAt.SetAngleLimits(90, 60);
            _threeBoneLookAtIK.ThirdLookAt.SetAngleLimits(90, 60);
        }

        public void LookAt(Vertex3f targetPosition)
        {
            _threeBoneLookAtIK.LookAt(targetPosition, ModelMatrix, _animator);
        }

        private void SetupArmIK()
        {
            // 본 설정
            _leftArm = _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_LeftArm];
            _leftForeArm = _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_LeftForeArm];
            _leftHand = _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_LeftHand];

            _rightArm = _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_RightArm];
            _rightForeArm = _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_RightForeArm];
            _rightHand = _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_RightHand];

            // TwoBoneIK 생성
            _leftArmTwoBoneIK = new ArmTwoBoneIK(_leftArm, _leftForeArm, _leftHand, true);
            _rightArmTwoBoneIK = new ArmTwoBoneIK(_rightArm, _rightForeArm, _rightHand, false);

            _leftArmRollIK = new ThreeBoneRoll(_leftArm, _leftForeArm, _leftHand, LocalSpaceAxis.Y);
            _rightArmRollIK = new ThreeBoneRoll(_rightArm, _rightForeArm, _rightHand, LocalSpaceAxis.Y);

            // 관절 제약조건 설정
            //AddSwingTwistConstraint(MIXAMORIG_BONENAME.mixamorig_LeftArm, 110, 30, LocalSpaceAxis.Y);
            //AddSwingTwistConstraint(MIXAMORIG_BONENAME.mixamorig_LeftForeArm, 110, 30, LocalSpaceAxis.Y);
            //AddSwingTwistConstraint(MIXAMORIG_BONENAME.mixamorig_RightArm, 110, 30, LocalSpaceAxis.Y);
            //AddSwingTwistConstraint(MIXAMORIG_BONENAME.mixamorig_RightForeArm, 110, 30, LocalSpaceAxis.Y);
        }

        private void SetupFootIK()
        {
            _leftFootToe = _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_LeftToeBase];
            _rightFootToe = _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_RightToeBase];
            _leftFoot = _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_LeftFoot];
            _rightFoot = _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_RightFoot];
            _leftLeg = _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_LeftLeg];
            _rightLeg = _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_RightLeg];

            _leftFootTwoBoneIK = new FootTwoBoneIK(
                _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_LeftUpLeg],
                _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_LeftLeg],
                _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_LeftFoot]);

            _rightFootTwoBoneIK = new FootTwoBoneIK(
                _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_RightUpLeg],
                _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_RightLeg],
                _aniRig.Armature[MIXAMORIG_BONENAME.mixamorig_RightFoot]);

        }
        // ====== 팔 IK 메서드 (신규) ======

        /// <summary>
        /// 팔 타겟 위치 설정
        /// </summary>
        public void SetArmWorldTarget(bool isLeft, Vertex3f targetPosition, Vertex3f lookat)
        {
            _isWorldTarget = true;

            if (isLeft)
                _leftArmWorldTargetInfo = new ArmTargetInfo
                {
                    HasTarget = true,
                    TargetPosition = targetPosition,
                    LookAt = lookat
                };
            else
                _rightArmWorldTargetInfo = new ArmTargetInfo
                {
                    HasTarget = true,
                    TargetPosition = targetPosition,
                    LookAt = lookat
                };
        }

        public void SetArmCharaterSpaceTarget(bool isLeft, Vertex3f targetPosition, Vertex3f lookat)
        {
            _isWorldTarget = false;

            if (isLeft)
                _leftArmCharacterTargetInfo = new ArmTargetInfo
                {
                    HasTarget = true,
                    TargetPosition = targetPosition,
                    LookAt = lookat
                };
            else
                _rightArmCharacterTargetInfo = new ArmTargetInfo
                {
                    HasTarget = true,
                    TargetPosition = targetPosition,
                    LookAt = lookat
                };
        }

        /// <summary>
        /// 팔 타겟 해제
        /// </summary>
        public void ClearArmTarget(bool isLeft)
        {
            if (isLeft)
                _leftArmWorldTargetInfo.HasTarget = false;
            else
                _rightArmWorldTargetInfo.HasTarget = false;
        }

        /// <summary>
        /// 팔 IK 적용
        /// </summary>
        public Vertex3f ApplyArmIK()
        {
            if (_isWorldTarget)
            {
                // 월드 좌표 타겟 IK 적용
                if (_leftArmWorldTargetInfo.HasTarget)
                {
                    _leftArmTwoBoneIK.Solve(
                        _leftArmWorldTargetInfo.TargetPosition,
                        _transform.Forward,
                        ModelMatrix,
                        _animator);

                    _leftArmRollIK.Solve(_hipWorldPosition, ModelMatrix, _animator);
                }

                if (_rightArmWorldTargetInfo.HasTarget)
                {
                    _rightArmTwoBoneIK.Solve(
                        _rightArmWorldTargetInfo.TargetPosition,
                        _transform.Forward,
                        ModelMatrix,
                        _animator);

                    _rightArmRollIK.Solve(_hipWorldPosition, ModelMatrix, _animator);
                }

                return Vertex3f.Zero;
            }
            else
            {
                // 캐릭터 로컬 좌표 타겟 IK 적용
                if (_leftArmCharacterTargetInfo.HasTarget)
                {
                    Matrix4x4f worldTransform = ModelMatrix * _animator.GetRootTransform(_leftHand);
                    Vertex3f target = worldTransform.Rot3x3f() * _leftArmCharacterTargetInfo.TargetPosition
                        + worldTransform.Position;

                    _leftArmTwoBoneIK.Solve(
                        target,
                        _transform.Forward,
                        ModelMatrix,
                        _animator);

                    _leftArmRollIK.Solve(_hipWorldPosition, ModelMatrix, _animator);

                    return target;
                }

                return Vertex3f.Zero;
            }
        }

        /// <summary>
        /// 손 월드 위치 가져오기
        /// </summary>
        public Vertex3f GetLeftHandPosition()
        {
            _leftHandWorldPosition = (ModelMatrix * _animator.RootTransforms[_leftHand.Index]).Position;
            return _leftHandWorldPosition;
        }

        public Vertex3f GetRightHandPosition()
        {
            _rightHandWorldPosition = (ModelMatrix * _animator.RootTransforms[_rightHand.Index]).Position;
            return _rightHandWorldPosition;
        }

        /// <summary>
        /// 발이 지면에 묻혔는지 검사
        /// </summary>
        /// <param name="isLeft">true면 왼쪽 발, false면 오른쪽 발</param>
        /// <param name="groundHeight">지면 높이 (z좌표)</param>
        /// <returns>발끝이 지면 아래 있으면 true</returns>
        public bool IsFootPenetrating(bool isLeft, float groundHeight)
        {
            if (isLeft)
            {
                return _leftFootToeWorldPosition.z < groundHeight;
            }
            else
            {
                return _rightFootToeWorldPosition.z < groundHeight;
            }
        }

        /// <summary>
        /// 발이 지면에 묻혔으니 발을 조정 (IK 적용)
        /// </summary>
        /// <param name="isLeft">true면 왼쪽 발, false면 오른쪽 발</param>
        /// <param name="groundPoint">지면 접촉점</param>
        /// <param name="groundNormal">지면 법선</param>
        public void AdjustFootToGround(bool isLeft, Vertex3f groundPoint, Vertex3f groundNormal)
        {
            // 지면 정보 설정
            if (isLeft)
                _leftFootGroundInfo = new FootGroundInfo
                {
                    IsGrounded = true,
                    GroundPoint = groundPoint,
                    GroundNormal = groundNormal
                };
            else
                _rightFootGroundInfo = new FootGroundInfo
                {
                    IsGrounded = true,
                    GroundPoint = groundPoint,
                    GroundNormal = groundNormal
                };

            // 해당 발의 IK 적용
            if (isLeft && _leftFootGroundInfo.IsGrounded)
            {
                _footDirection = _leftFootToeWorldPosition - _leftFootWorldPosition;
                _target = _leftFootGroundInfo.GroundPoint - _footDirection;
                _leftFootTwoBoneIK.Solve(_target, _transform.Forward, ModelMatrix, _animator);
            }
            else if (!isLeft && _rightFootGroundInfo.IsGrounded)
            {
                _footDirection = _rightFootToeWorldPosition - _rightFootWorldPosition;
                _target = _rightFootGroundInfo.GroundPoint - _footDirection;
                _rightFootTwoBoneIK.Solve(_target, _transform.Forward, ModelMatrix, _animator);
            }
        }

        public void EnableFootIK()
        {
            if (_isFootIKEnabled) return;
            _isFootIKEnabled = true;
        }

        public void DisableFootIK()
        {
            if (!_isFootIKEnabled) return;
            _isFootIKEnabled = false;
        }

        public Vertex3f GetLeftFootToePosition()
        {
            _leftFootToeWorldPosition = (ModelMatrix * _animator.RootTransforms[_leftFootToe.Index]).Position;
            return _leftFootToeWorldPosition;
        }

        public Vertex3f GetRightFootToePosition()
        {
            _rightFootToeWorldPosition = (ModelMatrix * _animator.RootTransforms[_rightFootToe.Index]).Position;
            return _rightFootToeWorldPosition;
        }

        protected void AddHingeConstraint(string boneName, float minBendAngle, float maxBendAngle, float minTwistAngle, float maxTwistAngle, LocalSpaceAxis axis)
        {
            Bone bone = _aniRig.Armature[boneName];
            HingeConstraint constraint = new HingeConstraint(bone, minBendAngle, maxBendAngle, minTwistAngle, maxTwistAngle, axis);
            bone.SetJointConstraint(constraint);
        }

        protected void AddSwingTwistConstraint(string boneName, float swingAngle, float twistAngle, LocalSpaceAxis forward)
        {
            Bone bone = _aniRig.Armature[boneName];
            SwingTwistConstraint constraint = new SwingTwistConstraint(bone, swingAngle, -twistAngle, twistAngle, forward);
            bone.SetJointConstraint(constraint);
        }

        private void AddJointSphericalConstraint(string boneName, float coneAngle, float twistAngle, LocalSpaceAxis forward, LocalSpaceAxis up)
        {
            Bone bone = _aniRig.Armature[boneName];
            SphericalConstraint constraint = new SphericalConstraint(bone, coneAngle, twistAngle, forward, up);
            bone.SetJointConstraint(constraint);
        }

        public override HUMAN_ACTION RandomAction => (HUMAN_ACTION)Rand.NextInt(0, (int)(HUMAN_ACTION.RANDOM - 1));

        public Vertex3f HipWorldPosition { get => _hipWorldPosition; }
        public Vertex3f LeftFootToeWorldPosition { get => _leftFootToeWorldPosition; }
        public Vertex3f RightFootToeWorldPosition { get => _rightFootToeWorldPosition; }
        public Vertex3f TestPoint { get => _testPoint; }

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
        /// 손을 감싸쥔다.
        /// </summary>
        /// <param name="whereHand"></param>
        public void FoldHand(bool isLeft, float intensity = 60.0f)
        {
            string handName = (isLeft ? "LeftHand" : "RightHand");

            if (!_actions.ContainsKey("fold" + handName))
            {
                Action action = () =>
                {
                    // 손을 가져온다.
                    Bone hand = AniRig.Armature["mixamorig_" + handName];

                    foreach (Bone bone in hand.ToBFSList(exceptBone: hand))
                    {
                        // 엄지 손가락이 아닌 경우
                        if (bone.Name.IndexOf("Thumb") < 0)
                        {
                            bone.BoneMatrixSet.LocalTransform = bone.BoneMatrixSet.LocalBindTransform * Matrix4x4f.RotatedX(40);
                        }
                    }

                    // 손의 모든 자식본을 업데이트한다.
                    //hand.UpdateAnimatorTransforms(_animator, isSelfIncluded: false);
                };

                if (isLeft) _actions["fold" + handName] = action;
                if (!isLeft) _actions["fold" + handName] = action;

                _updateAfter += action;
            }
        }

        public void UnfoldHand(bool isLeft)
        {
            string keyName = "fold" + (isLeft ? "Left" : "Right") + "Hand";
            if (_actions.ContainsKey(keyName))
            {
                Action action = _actions[keyName];
                _updateAfter -= action;
                _actions.Remove(keyName);
            }
        }

        public override void Render(Camera camera, Matrix4x4f vp, AnimateShader ashader)
        {
            base.Render(camera, vp, ashader);

            // 디버깅용 발끝 위치 렌더링
            Renderer3d.RenderPoint(_colorShader, _leftKneeWorldPosition, camera, new Vertex4f(1, 1, 0, 1), 0.015f);
            Renderer3d.RenderPoint(_colorShader, _leftFootWorldPosition, camera, new Vertex4f(1, 1, 0, 1), 0.015f);
            Renderer3d.RenderPoint(_colorShader, _leftFootToeWorldPosition, camera, new Vertex4f(1, 1, 0, 1), 0.015f);
            
            // 디버깅용 손 위치 렌더링
            if (_leftArmWorldTargetInfo.HasTarget)
            {
                Renderer3d.RenderPoint(_colorShader, _leftHandWorldPosition, camera, new Vertex4f(0, 1, 0, 1), 0.02f);
                Renderer3d.RenderPoint(_colorShader, _leftArmWorldTargetInfo.TargetPosition, camera, new Vertex4f(1, 0, 0, 1), 0.025f);
            }

            if (_rightArmWorldTargetInfo.HasTarget)
            {
                Renderer3d.RenderPoint(_colorShader, _rightHandWorldPosition, camera, new Vertex4f(0, 1, 0, 1), 0.02f);
                Renderer3d.RenderPoint(_colorShader, _rightArmWorldTargetInfo.TargetPosition, camera, new Vertex4f(1, 0, 0, 1), 0.025f);
            }
        }

        public override void Update(int deltaTime)
        {
            // 기본 업데이트 수행(최종 애니메이션 행렬 갱신 등)
            base.Update(deltaTime);

            _hipWorldPosition = (ModelMatrix * _animator.RootTransforms[_hip.Index]).Position;
            _headWorldPosition = (ModelMatrix * _animator.RootTransforms[_head.Index]).Position;

            // 발끝의 월드좌표 계산
            _leftFootToeWorldPosition = (ModelMatrix * _animator.RootTransforms[_leftFootToe.Index]).Position;
            _rightFootToeWorldPosition = (ModelMatrix * _animator.RootTransforms[_rightFootToe.Index]).Position;
            _leftFootWorldPosition = (ModelMatrix * _animator.RootTransforms[_leftFoot.Index]).Position;
            _rightFootWorldPosition = (ModelMatrix * _animator.RootTransforms[_rightFoot.Index]).Position;
            _leftKneeWorldPosition = (ModelMatrix * _animator.RootTransforms[_leftLeg.Index]).Position;
            _rightKneeWorldPosition = (ModelMatrix * _animator.RootTransforms[_rightLeg.Index]).Position;

            // 손의 월드좌표 계산
            _leftHandWorldPosition = (ModelMatrix * _animator.RootTransforms[_leftHand.Index]).Position;
            _rightHandWorldPosition = (ModelMatrix * _animator.RootTransforms[_rightHand.Index]).Position;
        }
    }

    /// <summary>
    /// 발 하나의 지면 정보
    /// </summary>
    public struct FootGroundInfo
    {
        public bool IsGrounded;      // 지면과 접촉 중인가
        public Vertex3f GroundPoint;  // 지면 접촉점
        public Vertex3f GroundNormal; // 지면 법선
    }

    /// <summary>
    /// 팔 하나의 타겟 정보
    /// </summary>
    public struct ArmTargetInfo
    {
        public bool HasTarget;
        public Vertex3f TargetPosition;
        public Vertex3f LookAt;
    }

    public static class MIXAMORIG_BONENAME
    {
        // Hips (Root)
        public const string mixamorig_Hips = "mixamorig_Hips";

        // Spine
        public const string mixamorig_Spine = "mixamorig_Spine";
        public const string mixamorig_Spine1 = "mixamorig_Spine1";
        public const string mixamorig_Spine2 = "mixamorig_Spine2";

        // Neck and Head
        public const string mixamorig_Neck = "mixamorig_Neck";
        public const string mixamorig_Head = "mixamorig_Head";
        public const string mixamorig_HeadTop_End = "mixamorig_HeadTop_End";

        // Left Leg
        public const string mixamorig_LeftUpLeg = "mixamorig_LeftUpLeg";
        public const string mixamorig_LeftLeg = "mixamorig_LeftLeg";
        public const string mixamorig_LeftFoot = "mixamorig_LeftFoot";
        public const string mixamorig_LeftToeBase = "mixamorig_LeftToeBase";
        public const string mixamorig_LeftToe_End = "mixamorig_LeftToe_End";

        // Right Leg
        public const string mixamorig_RightUpLeg = "mixamorig_RightUpLeg";
        public const string mixamorig_RightLeg = "mixamorig_RightLeg";
        public const string mixamorig_RightFoot = "mixamorig_RightFoot";
        public const string mixamorig_RightToeBase = "mixamorig_RightToeBase";
        public const string mixamorig_RightToe_End = "mixamorig_RightToe_End";

        // Left Arm
        public const string mixamorig_LeftShoulder = "mixamorig_LeftShoulder";
        public const string mixamorig_LeftArm = "mixamorig_LeftArm";
        public const string mixamorig_LeftForeArm = "mixamorig_LeftForeArm";
        public const string mixamorig_LeftHand = "mixamorig_LeftHand";

        // Left Hand Fingers
        public const string mixamorig_LeftHandThumb1 = "mixamorig_LeftHandThumb1";
        public const string mixamorig_LeftHandThumb2 = "mixamorig_LeftHandThumb2";
        public const string mixamorig_LeftHandThumb3 = "mixamorig_LeftHandThumb3";
        public const string mixamorig_LeftHandThumb4 = "mixamorig_LeftHandThumb4";
        public const string mixamorig_LeftHandIndex1 = "mixamorig_LeftHandIndex1";
        public const string mixamorig_LeftHandIndex2 = "mixamorig_LeftHandIndex2";
        public const string mixamorig_LeftHandIndex3 = "mixamorig_LeftHandIndex3";
        public const string mixamorig_LeftHandIndex4 = "mixamorig_LeftHandIndex4";
        public const string mixamorig_LeftHandMiddle1 = "mixamorig_LeftHandMiddle1";
        public const string mixamorig_LeftHandMiddle2 = "mixamorig_LeftHandMiddle2";
        public const string mixamorig_LeftHandMiddle3 = "mixamorig_LeftHandMiddle3";
        public const string mixamorig_LeftHandMiddle4 = "mixamorig_LeftHandMiddle4";
        public const string mixamorig_LeftHandRing1 = "mixamorig_LeftHandRing1";
        public const string mixamorig_LeftHandRing2 = "mixamorig_LeftHandRing2";
        public const string mixamorig_LeftHandRing3 = "mixamorig_LeftHandRing3";
        public const string mixamorig_LeftHandRing4 = "mixamorig_LeftHandRing4";
        public const string mixamorig_LeftHandPinky1 = "mixamorig_LeftHandPinky1";
        public const string mixamorig_LeftHandPinky2 = "mixamorig_LeftHandPinky2";
        public const string mixamorig_LeftHandPinky3 = "mixamorig_LeftHandPinky3";
        public const string mixamorig_LeftHandPinky4 = "mixamorig_LeftHandPinky4";

        // Right Arm
        public const string mixamorig_RightShoulder = "mixamorig_RightShoulder";
        public const string mixamorig_RightArm = "mixamorig_RightArm";
        public const string mixamorig_RightForeArm = "mixamorig_RightForeArm";
        public const string mixamorig_RightHand = "mixamorig_RightHand";

        // Right Hand Fingers
        public const string mixamorig_RightHandThumb1 = "mixamorig_RightHandThumb1";
        public const string mixamorig_RightHandThumb2 = "mixamorig_RightHandThumb2";
        public const string mixamorig_RightHandThumb3 = "mixamorig_RightHandThumb3";
        public const string mixamorig_RightHandThumb4 = "mixamorig_RightHandThumb4";
        public const string mixamorig_RightHandIndex1 = "mixamorig_RightHandIndex1";
        public const string mixamorig_RightHandIndex2 = "mixamorig_RightHandIndex2";
        public const string mixamorig_RightHandIndex3 = "mixamorig_RightHandIndex3";
        public const string mixamorig_RightHandIndex4 = "mixamorig_RightHandIndex4";
        public const string mixamorig_RightHandMiddle1 = "mixamorig_RightHandMiddle1";
        public const string mixamorig_RightHandMiddle2 = "mixamorig_RightHandMiddle2";
        public const string mixamorig_RightHandMiddle3 = "mixamorig_RightHandMiddle3";
        public const string mixamorig_RightHandMiddle4 = "mixamorig_RightHandMiddle4";
        public const string mixamorig_RightHandRing1 = "mixamorig_RightHandRing1";
        public const string mixamorig_RightHandRing2 = "mixamorig_RightHandRing2";
        public const string mixamorig_RightHandRing3 = "mixamorig_RightHandRing3";
        public const string mixamorig_RightHandRing4 = "mixamorig_RightHandRing4";
        public const string mixamorig_RightHandPinky1 = "mixamorig_RightHandPinky1";
        public const string mixamorig_RightHandPinky2 = "mixamorig_RightHandPinky2";
        public const string mixamorig_RightHandPinky3 = "mixamorig_RightHandPinky3";
        public const string mixamorig_RightHandPinky4 = "mixamorig_RightHandPinky4";
    }


}