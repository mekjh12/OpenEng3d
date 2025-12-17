using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZetaExt
{
    public static class Assert
    {
        public static void Notify(bool condition, string message)
        {
            if (condition)
            {
                throw new Exception($"Assertion failed: {message}");
            }
        }


        public static void Notify(bool condition)
        {
            if (!condition)
            {
                throw new ArgumentException("조건에 맞지 않습니다.");
            }
        }

    }
}
