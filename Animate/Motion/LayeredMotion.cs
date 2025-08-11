using OpenGL;
using System;
using System.Collections.Generic;

namespace Animate
{
    public class LayeredMotion : Motionable
    {
        // -----------------------------------------------------------------------
        // 멤버변수
        // -----------------------------------------------------------------------

        string _name;
        float _periodTime;
        Motionable _defaultMotion;
        Dictionary<MixamoBone, Motionable> _layered;
        Bone[] _bones;
        Motionable[] _motionables;

        // 최적화를 위한 캐시
        List<Bone> _boneListCache;
        List<Motionable> _motionListCache;
        Dictionary<string, Matrix4x4f> _tempPose1;
        Dictionary<string, Matrix4x4f> _tempPose2;

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        public string Name => _name;

        public float PeriodTime => _periodTime;

        public float Speed => 0.0f;

        public FootStepAnalyzer.MovementType MovementType => FootStepAnalyzer.MovementType.Stationary;



        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        public LayeredMotion(string newName, Motionable motionable, float periodTime = -1.0f)
        {
            _name = newName;
            _defaultMotion = motionable;
            _periodTime = periodTime;
            if (periodTime < 0.0f)  _periodTime = motionable.PeriodTime;
        }

        public void AddLayer(MixamoBone maskBoneName, Motionable motionable)
        {
            if (_layered == null)
            {
                _layered = new Dictionary<MixamoBone, Motionable>();
            }

            if (_layered.ContainsKey(maskBoneName))
            {
                _layered[maskBoneName] = motionable; // 이미 존재하면 업데이트
            }
            else
            {
                _layered.Add(maskBoneName, motionable); // 새로 추가
            }
        }

        /// <summary>
        /// 지정된 시간의 키프레임을 복제합니다.
        /// 레이어 블렌드 모션에서는 각 레이어의 키프레임을 조합합니다.
        /// </summary>
        public KeyFrame CloneKeyFrame(float time)
        {
            KeyFrame resultFrame = new KeyFrame(time);

            if (_bones == null || _motionables == null)
            {
                Console.WriteLine("경고: BuildTraverseBoneNamesCache가 실행되지 않았습니다.");
                return resultFrame;
            }

            // 각 본에 대해 해당 모션의 키프레임을 가져와서 조합
            for (int i = 0; i < _bones.Length; i++)
            {
                Bone bone = _bones[i];
                Motionable motion = _motionables[i];

                if (motion == null) continue;

                // Motion 타입인 경우에만 CloneKeyFrame 호출 가능
                if (motion is Motion regularMotion)
                {
                    KeyFrame motionFrame = regularMotion.CloneKeyFrame(time);

                    if (motionFrame != null && motionFrame.ContainsKey(bone.Name))
                    {
                        resultFrame.AddBoneTransform(bone.Name, motionFrame[bone.Name]);
                    }
                }
            }

            return resultFrame;
        }

        public void BuildTraverseBoneNamesCache(Bone rootBone)
        {
            // 최적화된 캐시를 사용하기 위해 기존 리스트를 재사용합니다.
            if (_boneListCache == null) 
            {
                _boneListCache = new List<Bone>();
            }
            else
            {
                _boneListCache.Clear();
            }

            if (_motionListCache == null)
            {
                _motionListCache = new List<Motionable>();
            }
            else
            {
                _motionListCache.Clear();
            }

            if (_layered == null)
            {
                _layered = new Dictionary<MixamoBone, Motionable>();
            }

            // 스택을 사용하여 본을 순회합니다.
            Stack<Bone> stack = new Stack<Bone>();
            Stack<Motionable> motionStack = new Stack<Motionable>();

            // 루트 본과 기본 모션을 스택에 추가합니다.
            stack.Push(rootBone);
            Motionable currentMotion = _defaultMotion;
            motionStack.Push(currentMotion);

            while (stack.Count > 0)
            {
                // 스택에서 본을 꺼내고 모션을 꺼냅니다.
                Bone bone = stack.Pop();
                currentMotion = motionStack.Pop();

                _boneListCache.Add(bone);
                _motionListCache.Add(currentMotion);

                // 하위 본을 탐색한다.
                for (int i=bone.Children.Count-1; i >= 0; i--)
                {
                    Bone child = bone.Children[i];

                    // 해당 본에 레이어 모션이 있으면 사용, 없으면 부모의 모션 상속
                    MixamoBone? mixamoBone = child.Name.FromMixamoName();

                    if (mixamoBone != null)
                    {
                        Motionable childMotion = _layered.ContainsKey((MixamoBone)mixamoBone)
                            ? _layered[(MixamoBone)mixamoBone]
                            : currentMotion;

                        motionStack.Push(childMotion);
                        stack.Push(child);
                    }
                }
            }

            _bones = _boneListCache.ToArray();
            _motionables = _motionListCache.ToArray();

            /*
            for (int i = 0; i < _bones.Length; i++)
            {
                Console.WriteLine($"{_bones[i].Name} / {_motionables[i].Name}");
            }
            */
        }

        public bool InterpolatePoseAtTime(float motionTime, ref Dictionary<string, Matrix4x4f> outPose)
        {
            if (_bones == null || _motionables == null)
            {
                throw new System.Exception("사용하기 전에 BuildTraverseBoneNamesCache 함수를 먼저 실행하세요.");
            }

            if (_tempPose1==null) 
            {
                _tempPose1 = new Dictionary<string, Matrix4x4f>();
            }
            else
            {
                _tempPose1.Clear();
            }

            outPose.Clear();

            // 각 본에 대해 해당하는 모션에서 포즈를 가져와서 결합
            for (int i = 0; i < _bones.Length; i++)
            {
                Bone bone = _bones[i];
                Motionable motion = _motionables[i];

                if (motion == null) continue;

                // 임시 포즈 딕셔너리 클리어
                _tempPose1.Clear();

                // 해당 모션에서 현재 시간의 포즈를 가져옴
                float newMotionTime = motionTime * motion.PeriodTime / _periodTime;

                if (motion.InterpolatePoseAtTime(newMotionTime, ref _tempPose1))
                {
                    // 해당 본의 변환 행렬이 있으면 출력 포즈에 추가
                    if (_tempPose1.ContainsKey(bone.Name))
                    {
                        outPose[bone.Name] = _tempPose1[bone.Name];
                    }
                }
            }

            return outPose.Count > 0;
        }
    }
}
