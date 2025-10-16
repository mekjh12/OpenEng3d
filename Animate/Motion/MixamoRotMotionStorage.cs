using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using ZetaExt;

namespace Animate
{
    public class MixamoRotMotionStorage
    {
        private const float ZERO_EPSILON = 0.001f;
        Dictionary<string, Motionable> _motions = new Dictionary<string, Motionable>();

        /// <summary>
        /// 모션 스토리지를 비운다.
        /// </summary>
        public void Clear()
        {
           _motions?.Clear();
        }

        /// <summary>
        /// 모션을 추가한다.
        /// </summary>
        /// <param name="motion"></param>
        public void AddMotion(Motionable motion)
        {
            string motionName = motion.Name;
            if (_motions.ContainsKey(motionName))
            {
                _motions[motionName] = motion;
            }
            else
            {
                _motions.Add(motionName, motion);
            }
        }

        [Obsolete("테스트 중...")]
        public void TransferByNamePairs(ArmatureLinker armatureLinker, AnimRig targetAniRig)
        {
            // 모션을 리타겟팅
            foreach (KeyValuePair<string, Motionable> motionItem in _motions)
            {
                if (motionItem.Value.GetType() != typeof(Motion)) continue;

                // 모션 이름과 모션 객체를 가져온다.
                string motionName = motionItem.Key;
                Motion srcMotion = (Motion)motionItem.Value;

                // 리타켓팅 알고리즘 구현하기
                if (srcMotion.PeriodTime > 0 && targetAniRig.DicBones != null)
                {
                    // SRC 모션의 정보를 가져온다.
                    string[] dstBoneNames = targetAniRig.Armature.DicBones.Keys.ToArray();
                    Dictionary<string, float> dstBonesLength = new Dictionary<string, float>();
                    for (int i = 0; i < dstBoneNames.Length; i++)
                    {
                        Bone bone = targetAniRig.DicBones[dstBoneNames[i]];
                        dstBonesLength[dstBoneNames[i]] = bone.BoneMatrixSet.LocalBindPosition.Length();
                    }

                    // 새로운 모션 객체를 생성하고, 모션의 매시간마다 키프레임을 생성한다.
                    Motion destMotion = new Motion(srcMotion.Name, srcMotion.PeriodTime);
                    foreach (var item in srcMotion.Keyframes)
                    {
                        float timeStamp = item.Key;
                        destMotion.AddKeyFrame(timeStamp);
                    }

                    foreach (var item in destMotion.Keyframes)
                    {
                        float timeStamp = item.Key;
                        KeyFrame destKeyFrame = item.Value;
                        KeyFrame srcKeyFrame = srcMotion[timeStamp];
                        foreach (Bone dstBone in armatureLinker.DestBones)
                        {
                            if (dstBone == null) continue;
                            if (armatureLinker.GetBones(dstBone).Length == 0) continue;
                            Bone srcBone = armatureLinker.GetBones(dstBone)[0];

                            float destBoneLength = dstBonesLength[dstBone.Name];
                            Vertex3f newPosition = destKeyFrame[dstBone.Name].Position.Normalized * destBoneLength;
                            destKeyFrame[dstBone.Name] = srcKeyFrame[srcBone.Name];
                        }
                    }

                    // 지정된 애니메이션 모델에 모션을 추가한다.
                    targetAniRig.AddMotion(destMotion);
                }
            }
        }

        /// <summary>
        /// 소스 모션을 지정된 애니메이션 그대로 복사합니다.
        /// </summary>
        /// <param name="targetAniRig"></param>
        public void TransferSourceMotionsAsIs(AnimRig targetAniRig)
        {
            foreach (KeyValuePair<string, Motionable> motionItem in _motions)
            {
                if (motionItem.Value.GetType() != typeof(Motion)) continue;
                string motionName = motionItem.Key;
                Motion srcMotion = (Motion)motionItem.Value;

                // 주기시간이 없거나 애니메이션 DAE에 본이 없으면 건너뜀
                if (srcMotion.PeriodTime <= 0 || targetAniRig.DicBones == null) continue;

                // 지정된 애니메이션 모델에 모션을 추가한다.
                targetAniRig.AddMotion(srcMotion);
            }

            return;
        }


        /// <summary>
        /// 모션스토리지에서 모션을 리타겟팅하여 지정된 애니메이션 DAE에 모션을 수정합니다.
        /// </summary>
        /// <param name="targetAniRig"></param>
        public void TransferRetargetMotions(AnimRig targetAniRig, AnimRig sourceAniRig)
        {
            // 믹사모 모션을 애니메이션 DAE에 리타겟팅
            foreach (KeyValuePair<string, Motionable> motionItem in _motions)
            {
                if (motionItem.Value.GetType() != typeof(Motion)) continue;

                // --------------------------------------------------------
                // 모션 이름과 모션 객체를 가져온다.
                // --------------------------------------------------------
                string motionName = motionItem.Key;
                Motion srcMotion = (Motion)motionItem.Value;

                // 주기시간이 없거나 애니메이션 DAE에 본이 없으면 건너뜀
                if (srcMotion.PeriodTime <= 0 || targetAniRig.DicBones == null) continue;

                // 개별 모션을 리타겟팅한다.
                TransferRetargetMotion(srcMotion, sourceAniRig, targetAniRig);
            }

            TransferRetargetMotion(sourceAniRig.BindMotion, sourceAniRig, targetAniRig);
            
            return;
        }

        private void TransferRetargetMotion(Motion srcMotion, AnimRig sourceAniRig, AnimRig targetAniRig)
        {
            // src 모션의 첫번째 키프레임에서 본 포즈를 가져온다.
            BoneTransform[] bonePoses = srcMotion.FirstKeyFrame.BoneTransforms;
            string[] boneNames = srcMotion.ExtractBoneName();
            Dictionary<string, float> bonesLength = new Dictionary<string, float>();

            // 각 본의 길이를 1로 초기화
            for (int i = 0; i < boneNames.Length; i++)
                bonesLength[boneNames[i]] = 1.0f;

            // 새로운 모션 객체를 생성하고, src 모션의 키프레임을 복사한다.
            Motion destMotion = srcMotion.Clone();

            foreach (KeyFrame keyframe in destMotion.Keyframes.Values)
            {
                for (int i = 0; i < boneNames.Length; i++)
                {
                    string boneName = boneNames[i];

                    // [키프레임---본]
                    if (targetAniRig.DicBones.ContainsKey(boneName))
                    {
                        // 각 본의 위치를 믹사모에서 가져온 길이로 설정
                        Bone targetBone = targetAniRig.DicBones[boneName];
                        BoneTransform dstBonePose = keyframe[boneName];

                        // 새로운 위치로 BoneTransform 생성하여 다시 할당
                        if (targetBone.IsHipBone)
                        {
                            float ratio = targetAniRig.Armature.HipHeight / sourceAniRig.Armature.HipHeight;
                            Vertex3f newPosition = dstBonePose.Position * ratio;
                            keyframe[boneName] = dstBonePose.WithPosition(newPosition);
                        }
                        else
                        {
                            if (dstBonePose.Position.Length() < ZERO_EPSILON) continue;
                            float destBoneLength = targetBone.BoneMatrixSet.LocalBindPosition.Length();
                            Vertex3f newPosition = dstBonePose.Position.Normalized * destBoneLength;
                            keyframe[boneName] = dstBonePose.WithPosition(newPosition);
                        }
                    }
                }
            }

            // 지정된 애니메이션 모델에 모션을 추가한다.
            targetAniRig.AddMotion(destMotion);
        }

        /// <summary>
        /// 모션스토리지에서 모션을 리타겟팅하여 지정된 애니메이션 DAE에 모션을 수정합니다.
        /// </summary>
        /// <param name="targetAniRig"></param>
        public void RetargetMotionsTransfer2(AnimRig targetAniRig)
        {
            Animator animator;

            // 믹사모 모션을 애니메이션 DAE에 리타겟팅
            foreach (KeyValuePair<string, Motionable> motionItem in _motions)
            {
                if (motionItem.Value.GetType() != typeof(Motion)) continue;

                // --------------------------------------------------------
                // 모션 이름과 모션 객체를 가져온다.
                // --------------------------------------------------------
                string motionName = motionItem.Key;
                Motion srcMotion = (Motion)motionItem.Value;

                // 주기시간이 없거나 애니메이션 DAE에 본이 없으면 건너뜀
                if (srcMotion.PeriodTime <= 0 || targetAniRig.DicBones == null) continue;

                // src 모션의 첫번째 키프레임에서 본 포즈를 가져온다.
                BoneTransform[] bonePoses = srcMotion.FirstKeyFrame.BoneTransforms;
                string[] boneNames = srcMotion.ExtractBoneName();
                Dictionary<string, float> bonesLength = new Dictionary<string, float>();

                for (int i = 0; i < boneNames.Length; i++)
                {
                    bonesLength[boneNames[i]] = 1.0f;
                }

                // 새로운 모션 객체를 생성하고, src 모션의 키프레임을 복사한다.
                Motion destMotion = srcMotion.Clone();

                foreach (KeyFrame keyframe in destMotion.Keyframes.Values)
                {
                    break;
                    for (int i = 0; i < boneNames.Length; i++)
                    {
                        string boneName = boneNames[i];

                        // 설정할 본이 애니메이션 DAE에 있는지 확인한다.
                        if (targetAniRig.DicBones.ContainsKey(boneName))
                        {
                            // 각 본의 위치를 믹사모에서 가져온 길이로 설정
                            Bone targetBone = targetAniRig.DicBones[boneName];
                            float destBoneLength = targetBone.BoneMatrixSet.LocalBindPosition.Length();
                            BoneTransform dstBonePose = keyframe[boneName];
                            keyframe[boneName] = dstBonePose;


                            // 새로운 위치로 BoneTransform 생성하여 다시 할당
                            if (targetBone.IsHipBone)
                            {
                                //keyframe[boneName] = dstBonePose.WithPosition(dstBonePose.Position);
                            }
                            else
                            {
                                //Vertex3f newPosition = dstBonePose.Position.Normalized * destBoneLength;
                                //keyframe[boneName] = dstBonePose.WithPosition(newPosition);
                            }
                        }
                    }
                }


                foreach (KeyFrame keyframe in destMotion.Keyframes.Values)
                {
                    break;
                    for (int i = 0; i < boneNames.Length; i++)
                    {
                        string boneName = boneNames[i];
                        if (targetAniRig.DicBones.ContainsKey(boneName))
                        {
                            Bone targetBone = targetAniRig.DicBones[boneName];
                            if (targetBone.IsHipBone)
                            {
                                BoneTransform dstBonePose = keyframe[boneName];
                                Vertex3f newPosition = dstBonePose.Position;
                                keyframe[boneName] = dstBonePose.WithPosition(newPosition);
                            }
                        }
                    }
                }


                // 모션의 발 위치를 계산한다.
                // 애니메이터를 생성한다.
                animator = new Animator(targetAniRig.Armature.RootBone);
                animator.SetMotion(destMotion);
                animator.Update(0);
                FootStepAnalyzer.FootStepResult footstep =
                    MotionExtensions.AnalyzeFootStep(destMotion, animator, targetAniRig.Armature.RootBone);

                destMotion.Speed = footstep.Speed;
                destMotion.FootStepDistance = footstep.FootStepDistance;
                destMotion.MovementType = footstep.Type;

                // 지정된 애니메이션 모델에 모션을 추가한다.
                targetAniRig.AddMotion(destMotion);
            }

            return;
        }
    }
}
