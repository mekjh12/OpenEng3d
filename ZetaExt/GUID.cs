namespace ZetaExt
{
    public class GUID
    {
        public static uint ID = 1;

        public static uint GenID
        {
            get
            {
                GUID.ID++;
                return GUID.ID;
            }
        }

    }
}
