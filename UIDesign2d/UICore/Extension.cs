namespace Ui2d
{
    public static class Extension
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="number"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static bool Between(this float number, float min, float max)
        {
            return number >= min && number <= max;
        }

        public static int NumOfLine(this string txt, string separator)
        {
            txt = txt.Replace(separator, "$");
            string[] lines = txt.Split(new char[] { '$' });
            return lines.Length;
        }

        /// <summary>
        /// min <= value <= max
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int Clamp(this int value, int min, int max)
        {
            if (value < min) value = min;
            if (value > max) value = max;
            return value;
        }

        /// <summary>
        ///  min <= value <= max
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static float Clamp(this float value, float min, float max)
        {
            if (value < min) value = min;
            if (value > max) value = max;
            return value;
        }

        /// <summary>
        /// 소숫점 아래에서 자른다.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        public static float Round(this float value, int num)
        {
            float step = 1;
            for (int i = 0; i < num; i++)
            {
                step *= 10;
            }
            value *= step;
            value = (int)value;
            value = value / step;
            return value;
        }

    }
}
