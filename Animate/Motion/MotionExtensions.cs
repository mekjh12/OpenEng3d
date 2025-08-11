namespace Animate
{
    /// <summary>
    /// Motion 클래스 확장 메서드
    /// </summary>
    public static class MotionExtensions
    {
        /// <summary>
        /// 모션의 발자국 분석을 수행합니다.
        /// </summary>
        public static FootStepAnalyzer.FootStepResult AnalyzeFootStep(this Motion motion, Animator animator, Bone rootBone)
        {
            return FootStepAnalyzer.AnalyzeFootStep(motion, animator, rootBone);
        }


    }
}
