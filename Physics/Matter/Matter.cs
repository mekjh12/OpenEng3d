namespace Physics
{
    /// <summary>
    /// 물질
    /// </summary>
    public abstract class Matter
    {
        protected float _specificGravity = 1.0f;

        /// <summary>
        /// 물질의 비중
        /// </summary>
        public float SpecificGravity => _specificGravity;

        public Matter()
        {

        }

        public static Matter Tree => new MatterTree();

        public static Matter Iron => new MatterIron();

        public static Matter Water => new MatterWater();

    }
}
