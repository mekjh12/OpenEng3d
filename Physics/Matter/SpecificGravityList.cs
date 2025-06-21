namespace Physics
{
    /// <summary>
    /// * 비중 = (물질의 밀도) / (표준 물질의 밀도)</br>
    /// - 기준으로 정한 물질의 밀도에 대해, 상대적으로 얼마의 밀도를 가지고 있는지에 대해 알 수 있는 상대적 밀도</br>
    /// - 고체와 액체의 경우 1기압, 4℃에서의 물이 이용.</br>
    /// - 기체의 경우에는 1기압, 0℃에서의 공기가 이용.</br>
    /// </summary>
    public static class SpecificGravityList
    {
        /// <summary>
        /// 표준 물질의 밀도(고체와 액체의 경우 1기압, 4℃에서의 물이 이용)
        /// </summary>
        public static float Water = 1.0f;

        /// <summary>
        /// 철의 비중
        /// </summary>
        public static float Fe = 7.87f;

        /// <summary>
        /// 금의 비중
        /// </summary>
        public static float Au = 7.87f;

        /// <summary>
        /// 모래의 비중
        /// </summary>
        public static float Sand = 1.4f;

        /// <summary>
        /// 알루미늄의 비중
        /// </summary>
        public static float Aluminum = 2.7f;

        /// <summary>
        /// 유리의 비중
        /// </summary>
        public static float Grass = 2.5f;

        /// <summary>
        /// 나무의 비중
        /// </summary>
        public static float Tree = 0.45f;

        /// <summary>
        /// 최대의 비중
        /// </summary>
        public static float Infinity = 1000 * 1000.0f; // 1000톤
    }
}
