namespace Animate
{
    public class MotionBlender
    {
        private static Motion _blendMotion;


        /// <summary>
        /// 두 모션을 블렌딩하여 새로운 모션을 생성합니다.
        /// </summary>
        /// <param name="name">블렌딩된 모션의 이름</param>
        /// <param name="prevMotion">이전 모션</param>
        /// <param name="prevTime">이전 모션에서의 블렌딩 시작 시간</param>
        /// <param name="nextMotion">다음 모션</param>
        /// <param name="nextTime">다음 모션에서의 블렌딩 종료 시간</param>
        /// <param name="blendingInterval">블렌딩 간격</param>
        /// <returns>블렌딩된 새로운 모션</returns>
        public static Motion BlendMotion(
            string name, 
            Motion prevMotion, 
            float prevTime, 
            Motion nextMotion, 
            float nextTime, 
            float blendingInterval)
        {
            KeyFrame k0 = prevMotion.CloneKeyFrame(prevTime);
            k0.TimeStamp = 0;
            KeyFrame k1 = nextMotion.CloneKeyFrame(nextTime);
            k1.TimeStamp = blendingInterval;
            Motion blendMotion = new Motion(name, blendingInterval);
            if (k0 != null) blendMotion.AddKeyFrame(k0);
            if (k1 != null) blendMotion.AddKeyFrame(k1);
            return blendMotion;
        }

        public static Motion BlendMotionFast(string name,
            Motion prevMotion, float prevTime,
            Motion nextMotion, float nextTime,
            float blendingInterval)
        {
            KeyFrame k0 = prevMotion.GetFastKeyFrame(prevTime);
            k0.TimeStamp = 0;
            KeyFrame k1 = nextMotion.GetFastKeyFrame(nextTime);
            k1.TimeStamp = blendingInterval;

            _blendMotion = new Motion(name, blendingInterval);
            if (k0 != null) _blendMotion.AddKeyFrame(k0);
            if (k1 != null) _blendMotion.AddKeyFrame(k1);
            return _blendMotion;
        }
    }
}
