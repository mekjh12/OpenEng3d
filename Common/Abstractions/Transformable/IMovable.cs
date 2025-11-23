namespace Common.Abstractions
{
    /// <summary>
    /// 이동 기능을 제공한다.
    /// </summary>
    public interface IMovable : IPositionable
    {
        /// <summary>
        /// 위치를 이동한다.
        /// </summary>
        /// <param name="dx">x축 이동량</param>
        /// <param name="dy">Y축 이동량</param>
        /// <param name="dz">Z축 이동량</param>
        void Translate(float dx, float dy, float dz);
    }
}
