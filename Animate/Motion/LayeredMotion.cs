using OpenGL;
using System;
using System.Collections.Generic;

namespace Animate
{
    /// <summary>
    /// TODO: 여러 행동을 뼈대별로 레이어링하여 블렌딩된 모션을 나타낼 때 주기시간을 어떻게 정할지 고민 필요
    /// 다시 이야기하면 레이어 모션간 주기시간이 다를 때 어떻게 처리할지 고민 필요
    /// </summary>
    public class LayeredMotion : Motionable
    {
        // -----------------------------------------------------------------------
        // 멤버변수
        // -----------------------------------------------------------------------

        private string _name;
        private float _periodTime;
        private float _speed;
        private FootStepAnalyzer.MovementType _movementType;

        private Motionable _defaultMotion;
        private Dictionary<MixamoBone, Motionable> _layered;
        private Bone[] _bones;
        private Motionable[] _motionables;

        // 최적화를 위한 캐시
        private List<Bone> _boneListCache;
        private List<Motionable> _motionListCache;
        private Dictionary<Motionable, Dictionary<string, Matrix4x4f>> _poseCaches;

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        public string Name => _name;
        public float PeriodTime => _periodTime;
        public float Speed => _speed;
        public FootStepAnalyzer.MovementType MovementType => _movementType;
        public Bone RootBone => _defaultMotion.RootBone;

        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        /// <summary>
        /// 레이어 모션 생성자
        /// motionable은 이동을 위하여 하체를 위주로 먼저 설정한다.
        /// </summary>
        /// <param name="newName">모션 이름</param>
        /// <param name="motionable">기본 모션</param>
        /// <param name="periodTime">주기 시간 (-1이면 기본 모션 시간 사용)</param>
        public LayeredMotion(string newName, Motionable motionable, float periodTime = -1.0f)
        {
            _name = newName;
            _defaultMotion = motionable;
            _periodTime = periodTime;
            _speed = motionable.Speed;
            _movementType = motionable.MovementType;

            if (periodTime < 0.0f)
                _periodTime = motionable.PeriodTime;
        }

        // -----------------------------------------------------------------------
        // 공개 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// 특정 뼈대에 레이어 모션 추가
        /// </summary>
        /// <param name="maskBoneName">적용할 뼈대</param>
        /// <param name="motionable">적용할 모션</param>
        public void AddLayer(MixamoBone maskBoneName, Motionable motionable)
        {
            if (_layered == null)
            {
                _layered = new Dictionary<MixamoBone, Motionable>();
            }

            _layered[maskBoneName] = motionable;
        }

        /// <summary>
        /// 블렌딩된 모션의 주기 시간을 설정합니다.
        /// </summary>
        public void SetPeriodTime(float periodTime)
        {
            _periodTime = periodTime;
        }

        /// <summary>
        /// 뼈대 순회 캐시 구축
        /// </summary>
        /// <param name="rootBone">루트 뼈대</param>
        public void BuildTraverseBoneNamesCache(Bone rootBone)
        {
            // 캐시 초기화
            InitializeCaches();

            // 포즈 캐시 초기화
            if (_poseCaches == null)
            {
                _poseCaches = new Dictionary<Motionable, Dictionary<string, Matrix4x4f>>();

                // 기본 모션 캐시 생성
                _poseCaches[_defaultMotion] = new Dictionary<string, Matrix4x4f>();

                // 레이어 모션 캐시 생성
                foreach (var kvp in _layered)
                {
                    _poseCaches[kvp.Value] = new Dictionary<string, Matrix4x4f>();
                }
            }

            // 뼈대 계층구조 순회
            TraverseBoneHierarchy(rootBone);

            // 배열로 변환
            _bones = _boneListCache.ToArray();
            _motionables = _motionListCache.ToArray();
        }

        /// <summary>
        /// 지정된 시간의 포즈 보간
        /// </summary>
        /// <param name="motionTime">모션 시간</param>
        /// <param name="outPose">출력 포즈</param>
        /// <param name="searchStartBone">검색 시작 뼈대</param>
        /// <returns>성공 여부</returns>
        public bool InterpolatePoseAtTime(float motionTime, ref Dictionary<string, Matrix4x4f> outPose, Bone searchStartBone = null)
        {
            // 뼈가 지정되지 않은 경우 루트 본을 사용
            if (searchStartBone == null)
                searchStartBone = _defaultMotion.RootBone;

            // 초기화 확인
            if (_bones == null || _motionables == null)
            {
                throw new Exception("사용하기 전에 BuildTraverseBoneNamesCache 함수를 먼저 실행하세요.");
            }

            // 기본 모션 포즈 계산
            var defaultPose = _poseCaches[_defaultMotion];
            defaultPose.Clear();
            float newMotionTime = motionTime * _defaultMotion.PeriodTime / _periodTime;
            _defaultMotion.InterpolatePoseAtTime(newMotionTime, ref defaultPose);

            // 레이어 모션들 포즈 계산
            foreach (var kvp in _layered)
            {
                Motionable motionable = kvp.Value;
                var pose = _poseCaches[motionable];
                pose.Clear();

                newMotionTime = motionTime * motionable.PeriodTime / _periodTime;
                motionable.InterpolatePoseAtTime(newMotionTime, ref pose);
            }

            // 최종 포즈 조합
            outPose.Clear();

            // 각 본에 대해 해당하는 모션에서 포즈를 가져와서 결합
            for (int i = 0; i < _bones.Length; i++)
            {
                Bone bone = _bones[i];
                Motionable motion = _motionables[i];

                if (motion == null) continue;

                // 해당 본의 변환 행렬이 있으면 출력 포즈에 추가
                if (_poseCaches[motion].ContainsKey(bone.Name))
                {
                    outPose[bone.Name] = _poseCaches[motion][bone.Name];
                }
            }

            return outPose.Count > 0;
        }

        /// <summary>
        /// 지정된 시간의 키프레임 복제
        /// </summary>
        /// <param name="time">복제할 시간</param>
        /// <returns>복제된 키프레임</returns>
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

        // -----------------------------------------------------------------------
        // 내부 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// 캐시 초기화
        /// </summary>
        private void InitializeCaches()
        {
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
        }

        /// <summary>
        /// 뼈대 계층구조 순회
        /// </summary>
        /// <param name="rootBone">루트 뼈대</param>
        private void TraverseBoneHierarchy(Bone rootBone)
        {
            Stack<Bone> boneStack = new Stack<Bone>();
            Stack<Motionable> motionStack = new Stack<Motionable>();

            // 루트 본과 기본 모션을 스택에 추가
            boneStack.Push(rootBone);
            motionStack.Push(_defaultMotion);

            while (boneStack.Count > 0)
            {
                // 스택에서 본과 모션을 꺼냄
                Bone bone = boneStack.Pop();
                Motionable currentMotion = motionStack.Pop();

                _boneListCache.Add(bone);
                _motionListCache.Add(currentMotion);

                // 하위 본 탐색
                for (int i = bone.Children.Count - 1; i >= 0; i--)
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
                        boneStack.Push(child);
                    }
                }
            }
        }
    }
}