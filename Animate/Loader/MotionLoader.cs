using Microsoft.SqlServer.Server;
using OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using ZetaExt;

namespace Animate
{
    public static class MotionLoader
    {
        private const string ARMATURE = "Armature";
        private const string CHANNEL = "channel";
        private const string SOURCE = "source";
        private const string SAMPLER = "sampler";
        private const string SEMANTIC = "semantic";
        private const string INPUT = "INPUT";
        private const string OUTPUT = "OUTPUT";
        private const string INTERPOLATION = "INTERPOLATION";
        private const string FLOAT_ARRAY = "float_array";
        private const string NAME_ARRAY = "Name_array";
        private const string ANIMATION = "animation";
        private const string TARGET = "target";

        /// <summary>
        /// 대입한 모델의 엉덩이 뼈를 기준으로 엉덩이 뼈의 바닥으로부터의 상대적 높이를 반환한다.
        /// </summary>
        /// <param name="animationData">이식을 가져올 애니메이션 행렬 모음</param>
        /// <param name="dicBones">이식할 뼈대의 모음</param>
        /// <returns></returns>
        private static float CalculateHipScaleRatio(Dictionary<string, Dictionary<float, Matrix4x4f>> animationData, Dictionary<string, Bone> dicBones)
        {
            // 알고리즘 설명: 0초의 엉덩이 뼈를 찾아 상대적 비를 계산한다.
            // 
            //               dstSize       이식할 뼈대의 hip Bone의 pivot의 크기
            //  hipScaled = ---------  = ---------------------------------------
            //               srcSize       이식을 가져올 hip Bone의 pivot의 크기
            //
            if (dicBones == null) return 1.0f;

            // 딕셔너리 정보는 <뼈, <시간, 행렬>>로 구성되어 있다.
            foreach (KeyValuePair<string, Dictionary<float, Matrix4x4f>> item in animationData)
            {
                string boneName = item.Key;
                Dictionary<float, Matrix4x4f> timeFrames = item.Value;

                if (!dicBones.ContainsKey(boneName)) continue;

                Bone bone = dicBones[boneName];
                if (bone.IsHipBone && timeFrames.ContainsKey(0.0f)) //
                {
                    float dstSize = bone.BoneTransforms.Pivot.Norm();//.BoneTransforms.InverseBindPoseTransform.Inversed().Position.Norm();
                    float srcSize = timeFrames[0.0f].Position.Norm();
                    return dstSize / srcSize; // 찾으면 즉시 반환
                }
            }

            return 1.0f; // 루트 본을 찾지 못한 경우 기본값
        }

        /// <summary>
        /// * Mixamo에서 Export한 Dae파일을 그대로 읽어온다. <br/>
        /// - Without Skin, Only Armature <br/>
        /// - "3D Mesh Processing and Character Animation", p.183 Animation Retargeting
        /// </summary>
        /// <param name="targetAniRig"></param>
        /// <param name="motionFileName"></param>
        public static Motion LoadMixamoMotion(AniRig targetAniRig, string motionFileName)
        {
            // Xml을 준비한다.
            XmlDocument xml = new XmlDocument();
            xml.Load(motionFileName);
            string motionName = Path.GetFileNameWithoutExtension(motionFileName);

            // dae 파일 구조에서 애니메이션 구조를 읽어온다.
            XmlNodeList libraryAnimations = xml.GetElementsByTagName("library_animations");
            if (libraryAnimations.Count == 0)
            {
                Console.WriteLine($"{motionName} dae 파일 구조에서 애니메이션 구조를 읽어올 수 없습니다.");
                return null;
            }

            // 애니메이션 정보를 담을 딕셔너리 생성 (boneName, Dictionary<time, Matrix4x4f>)
            Dictionary<string, Dictionary<float, Matrix4x4f>> animationData = new Dictionary<string, Dictionary<float, Matrix4x4f>>();
            float maxTimeLength = 0.0f;

            // 각 뼈의 애니메이션 소스를 읽어온다.
            // 행렬 정보는 4x4 행렬로 되어있고, 시간은 float로 되어있다.
            // 행렬은 캐릭터의 발 밑 가운데를 원점으로 하는 캐릭터 공간 변환행렬이다.
            foreach ((XmlNode parentNode, XmlNode node) in libraryAnimations[0].TraverseXmlNodesWithParent())
            {
                if (node.Name != ANIMATION) continue;

                if (node[CHANNEL] == null) continue;

                // boneAnimation은 <animation> 태그로 되어있다.
                string bid = "";
                if (node[CHANNEL].HasAttribute(TARGET))
                {
                    bid = node[CHANNEL].GetAttribute(TARGET).Replace("/transform", "");
                }

                string boneName = node.Attributes["name"].Value;
                foreach (Bone bone in targetAniRig.DicBones.Values)
                {
                    if (bone.ID == bid)
                    {
                        boneName = bone.Name;
                        break;
                    }
                }

                //boneName = boneName.Substring(0, boneName.Length);

                if (boneName == ARMATURE) continue;

                // 애니메이션 소스의 시간과 행렬을 담을 리스트를 생성한다.
                List<float> sourceInput = new List<float>();
                List<Matrix4x4f> sourceOutput = new List<Matrix4x4f>();
                List<string> interpolationInput = new List<string>();

                // 채널과 샘플러를 가져온다.
                XmlNode channel = node[CHANNEL];

                string channelName = channel.Attributes[SOURCE].Value;
                XmlNode sampler = node[SAMPLER];
                if (channelName != "#" + sampler.Attributes["id"].Value) continue;

                // sampler의 INPUT, OUTPUT, INTERPOLATION을 읽어온다.
                string inputName = "";
                string outputName = "";
                string interpolationName = "";
                foreach (XmlNode input in sampler.ChildNodes)
                {
                    if (input.Attributes[SEMANTIC].Value == INPUT) inputName = input.Attributes[SOURCE].Value;
                    if (input.Attributes[SEMANTIC].Value == OUTPUT) outputName = input.Attributes[SOURCE].Value;
                    if (input.Attributes[SEMANTIC].Value == INTERPOLATION) interpolationName = input.Attributes[SOURCE].Value;
                }

                // 각 뼈마다 시간과 행렬을 가져온다.
                foreach (XmlNode source in node.ChildNodes)
                {
                    if (source.Name == SOURCE)
                    {
                        // source의 id를 읽어온다.
                        string sourcesId = source.Attributes["id"].Value;
                        if ("#" + sourcesId == inputName)
                        {
                            // 시간 배열을 가져오고 최대시간을 얻는다.
                            string[] value = source[FLOAT_ARRAY].InnerText.Trim().Replace("\n", " ").Split(' ');
                            float[] items = new float[value.Length];
                            for (int i = 0; i < value.Length; i++)
                            {
                                items[i] = float.Parse(value[i].Trim());
                                maxTimeLength = Math.Max(items[i], maxTimeLength);
                            }
                            sourceInput.AddRange(items);
                        }

                        // source의 행렬을 읽어온다. 
                        // 행렬은 각 본의 로컬 공간 변환행렬 (부모 본에 대한 상대적 변환)
                        if ("#" + sourcesId == outputName)
                        {
                            string[] value = source[FLOAT_ARRAY].InnerText.Trim().Replace("\n", " ").Split(' ');
                            float[] items = new float[value.Length];
                            for (int i = 0; i < value.Length; i++) items[i] = float.Parse(value[i]);
                            for (int i = 0; i < value.Length; i += 16)
                            {
                                List<float> mat = new List<float>();
                                for (int j = 0; j < 16; j++) mat.Add(items[i + j]);

                                // Mixamo에서 Export한 Dae파일은 행렬이 Transposed되어 있다.
                                Matrix4x4f matrix = new Matrix4x4f(mat.ToArray());

                                // 0열부터 2열은 회전정보이고, 3열은 위치정보이다.
                                sourceOutput.Add(matrix.Transposed);
                            }
                        }

                        // source의 INTERPOLATION을 읽어온다. (예) LINEAR, BEZIER 등
                        if ("#" + sourcesId == interpolationName)
                        {
                            string[] value = source[NAME_ARRAY].InnerText.Trim().Replace("\n", " ").Split(' ');
                            interpolationInput.AddRange(value);
                        }
                    }
                }

                // 가져온 소스로 키프레임을 만든다.
                Dictionary<float, Matrix4x4f> keyframe = new Dictionary<float, Matrix4x4f>();
                for (int i = 0; i < sourceInput.Count; i++)
                {
                    keyframe.Add(sourceInput[i], sourceOutput[i]);
                }

                if (!animationData.ContainsKey(boneName))
                {
                    animationData.Add(boneName, keyframe);
                }
            }

            // *** [중요] 바닥으로부터 엉덩이 위치를 맞추기 위하여 hipHeightScale을 구한다.
            // Interpolation Pose만 0초에서 정상적 T-pose를 취하고 있어서 이 부분에서 가져와야 한다.
            if (motionName == "a-T-Pose") //Interpolation Pose
            {
                targetAniRig.Armature.HipHeightScaled = CalculateHipScaleRatio(animationData, targetAniRig.DicBones);
                Console.WriteLine($"{targetAniRig.Name} XmeDae HipScaled={targetAniRig.Armature.HipHeightScaled}");
            }

            // 애니메이션을 생성한다.
            Motion motion = new Motion(motionName, maxTimeLength);
            if (maxTimeLength > 0 && targetAniRig.DicBones != null)
            {
                // 뼈마다 순회 (뼈, 시간, 로컬변환행렬)
                foreach (KeyValuePair<string, Dictionary<float, Matrix4x4f>> item in animationData)
                {
                    string boneName = item.Key;
                    Dictionary<float, Matrix4x4f> source = item.Value;

                    boneName = boneName.Replace("_pose_m", "");
                    boneName = boneName.Replace("Armature_Armature_", "");

                    Bone bone = targetAniRig.Armature[boneName];
                    if (bone == null) continue;

                    // 시간마다 순회 (시간, 로컬변환행렬)
                    foreach (KeyValuePair<float, Matrix4x4f> subsource in source)
                    {
                        float time = subsource.Key;
                        Matrix4x4f mat = subsource.Value;

                        // 본포즈를 설정한다.
                        Vertex3f position = bone.IsHipBone ?
                            mat.Position * targetAniRig.Armature.HipHeightScaled :
                            bone.BoneTransforms.LocalPivot;

                        ZetaExt.Quaternion q = mat.ToQuaternion();
                        q.Normalize();
                        BoneTransform boneTransform = new BoneTransform(position, q);

                        // 키프레임을 추가하고 본포즈를 추가한다.
                        motion.AddKeyFrame(time);
                        motion[time].AddBoneTransform(boneName, boneTransform);
                    }
                }
            }

            return motion;
        }

    }
}
