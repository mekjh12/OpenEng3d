using Common.Abstractions;

namespace Animate
{
    /// <summary>
    /// 애니메이션 컴포넌트의 기본 구현체<br/>
    /// - 단일 본 가중치 모드를 지원하여 특정 본에만 완전히 바인딩된 객체 처리<br/>
    /// - 무기, 액세서리 등 하나의 본에만 붙어있는 아이템들에 최적화
    /// </summary>
    public class AnimateComponent : IAnimateComponent
    {
        protected bool _isOnlyOneJointWeight = false; // 단일 본 가중치 모드 활성화 여부
        protected int _boneIndexOnlyOneJoint;         // 바인딩할 본의 인덱스

        /// <summary>
        /// 단일 본 가중치 모드에서 사용할 본의 인덱스<br/>
        /// 이 본에만 100% 가중치가 적용되어 완전히 따라 움직임
        /// </summary>
        public int BoneIndexOnlyOneJoint
        {
            get => _boneIndexOnlyOneJoint;
            set => _boneIndexOnlyOneJoint = value;
        }

        /// <summary>
        /// 단일 본 가중치 모드 사용 여부<br/>
        /// true: 하나의 본에만 완전 바인딩 (무기, 모자 등)<br/>
        /// false: 일반적인 다중 본 스키닝 사용
        /// </summary>
        public bool IsOnlyOneJointWeight
        {
            get => _isOnlyOneJointWeight;
            set => _isOnlyOneJointWeight = value;
        }
    }
}