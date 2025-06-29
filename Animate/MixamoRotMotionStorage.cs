using System.Collections.Generic;

namespace Animate
{
    public class MixamoRotMotionStorage
    {
        Dictionary<string, Motion> _motions = new Dictionary<string, Motion>();

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

        public void RetargetMotionsTransfer(AniDae targetAniDae)
        {
            // src:abe, dst:hero, 

            // 믹사모 모션을 애니메이션 DAE에 리타겟팅
            foreach (KeyValuePair<string, Motion> motionItem in _motions)
            {
                // 모션 이름과 모션 객체를 가져온다.
                string motionName = motionItem.Key;
                Motion srcMotion = motionItem.Value;

                // 리타켓팅 알고리즘 구현하기
                // TODO 목이 긴 abe캐릭터에서 모션을 읽어와 전이하여 아직 구현이 안된 목이 긴 캐릭터
                if (srcMotion.Length > 0 && targetAniDae.DicBones != null)
                {
                    // dest의 bone크기 가져오기
                    //Bone[] bones = destAniDae.DicBones.Values;

                    // src 모션의 첫번째 키프레임에서 본 포즈를 가져온다.
                    BonePose[] bonePoses = srcMotion.FirstKeyFrame.Pose.BonePoses;
                    string[] boneNames = srcMotion.FirstKeyFrame.Pose.JointNames;
                    Dictionary<string, float> bonesLength = new Dictionary<string, float>();
                    for (int i=0; i < boneNames.Length; i++ )
                    {
                        bonesLength[boneNames[i]] = 1.0f;
                    }

                    // 새로운 모션 객체를 생성하고, src 모션의 키프레임을 복사한다.
                    Motion destMotion = srcMotion.Clone();
                    foreach (KeyFrame frame in destMotion.Keyframes.Values)
                    {
                        for (int i = 0; i < boneNames.Length; i++)
                        {
                            BonePose dstBonePose = frame.Pose[boneNames[i]];
                            BonePose srcBonePose = bonePoses[i];
                            float destBoneLength = bonesLength[boneNames[i]];
                            // 위치를 믹사모에서 가져온 길이로 설정
                            //dstBonePose.Position = dstBonePose.Position.Normalized * destBoneLength;
                        }                      
                    }

                    // 지정된 애니메이션 모델에 모션을 추가한다.
                    targetAniDae.AddMotion(destMotion);
                }
            }

            return;
        }
    }
}
