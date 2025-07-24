using OpenGL;
using System;
using System.Collections.Generic;

namespace Animate
{
    public class MixamoRotMotionStorage
    {
        Dictionary<string, Motion> _motions = new Dictionary<string, Motion>();

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

        /// <summary>
        /// 모션스토리지에서 모션을 리타겟팅하여 지정된 애니메이션 DAE에 모션을 수정합니다.
        /// </summary>
        /// <param name="targetAniRig"></param>
        public void RetargetMotionsTransfer(AniRig targetAniRig)
        {
            // 믹사모 모션을 애니메이션 DAE에 리타겟팅
            foreach (KeyValuePair<string, Motion> motionItem in _motions)
            {
                // 모션 이름과 모션 객체를 가져온다.
                string motionName = motionItem.Key;
                Motion srcMotion = motionItem.Value;

                // 리타켓팅 알고리즘 구현하기
                if (srcMotion.Length > 0 && targetAniRig.DicBones != null)
                {
                    // src 모션의 첫번째 키프레임에서 본 포즈를 가져온다.
                    BoneTransform[] bonePoses = srcMotion.FirstKeyFrame.BoneTransforms;
                    string[] boneNames = srcMotion.FirstKeyFrame.BoneNames;
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
                            // 설정할 본이 애니메이션 DAE에 있는지 확인한다.
                            if (targetAniRig.DicBones.ContainsKey(boneNames[i]))
                            {
                                // 각 본의 위치를 믹사모에서 가져온 길이로 설정
                                Bone b = targetAniRig.DicBones[boneNames[i]];
                                float destBoneLength = b.BoneTransforms.LocalPivot.Norm();
                                BoneTransform dstBonePose = keyframe[boneNames[i]];

                                // 새로운 위치로 BoneTransform 생성하여 다시 할당
                                Vertex3f newPosition = dstBonePose.Position.Normalized * destBoneLength;
                                keyframe[boneNames[i]] = dstBonePose.WithPosition(newPosition);
                            }
                        }                      
                    }

                    // 지정된 애니메이션 모델에 모션을 추가한다.
                    targetAniRig.AddMotion(destMotion);
                }
            }

            return;
        }
    }
}
