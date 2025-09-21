using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using ZetaExt;

namespace Animate
{
    public class MixamoRotMotionStorage
    {
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
        public void AddMotion(Motion motion)
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

        public void Transfer(ArmatureLinker armatureLinker, AnimRig targetAniRig)
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
                        dstBonesLength[dstBoneNames[i]] = bone.BoneMatrixSet.LocalPivot.Length();
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
        /// 모션스토리지에서 모션을 리타겟팅하여 지정된 애니메이션 DAE에 모션을 수정합니다.
        /// </summary>
        /// <param name="targetAniRig"></param>
        public void RetargetMotionsTransfer(AnimRig targetAniRig)
        {
            Animator animator;

            // 믹사모 모션을 애니메이션 DAE에 리타겟팅
            foreach (KeyValuePair<string, Motionable> motionItem in _motions)
            {
                if (motionItem.Value.GetType() != typeof(Motion)) continue;

                // 모션 이름과 모션 객체를 가져온다.
                string motionName = motionItem.Key;
                Motion srcMotion = (Motion)motionItem.Value;

                // 리타켓팅 알고리즘 구현하기
                if (srcMotion.PeriodTime > 0 && targetAniRig.DicBones != null)
                {
                    // src 모션의 첫번째 키프레임에서 본 포즈를 가져온다.
                    BoneTransform[] bonePoses = srcMotion.FirstKeyFrame.BoneTransforms;
                    string[] boneNames = srcMotion.FirstKeyFrame.BoneNames;// srcMotion.ExtractBoneName();
                    Dictionary<string, float> bonesLength = new Dictionary<string, float>();

                    for (int i=0; i < boneNames.Length; i++ )
                    {
                        bonesLength[boneNames[i]] = 1.0f;
                    }

                    // 새로운 모션 객체를 생성하고, src 모션의 키프레임을 복사한다.
                    Motion destMotion = srcMotion.Clone();

                    foreach (KeyFrame keyframe in destMotion.Keyframes.Values)
                    {
                        for (int i = 0; i < boneNames.Length; i++)
                        {
                            string boneName = boneNames[i];

                            // 설정할 본이 애니메이션 DAE에 있는지 확인한다.
                            if (targetAniRig.DicBones.ContainsKey(boneName))
                            {
                                // 각 본의 위치를 믹사모에서 가져온 길이로 설정
                                Bone targetBone = targetAniRig.DicBones[boneName];
                                float destBoneLength = targetBone.BoneMatrixSet.LocalPivot.Length();
                                BoneTransform dstBonePose = keyframe[boneName];

                                // 새로운 위치로 BoneTransform 생성하여 다시 할당
                                Vertex3f newPosition = dstBonePose.Position.Normalized * destBoneLength;
                                keyframe[boneName] = dstBonePose.WithPosition(newPosition);
                            }
                        }                      
                    }

                    // 모션의 발 위치를 계산한다.
                    animator = new Animator(targetAniRig.Armature.RootBone);
                    FootStepAnalyzer.FootStepResult footstep =
                        MotionExtensions.AnalyzeFootStep(destMotion, animator, targetAniRig.Armature.RootBone);

                    destMotion.Speed = footstep.Speed;
                    destMotion.FootStepDistance = footstep.FootStepDistance;
                    destMotion.MovementType = footstep.Type;

                    // 지정된 애니메이션 모델에 모션을 추가한다.
                    targetAniRig.AddMotion(destMotion);
                }
            }

            return;
        }
    }
}
