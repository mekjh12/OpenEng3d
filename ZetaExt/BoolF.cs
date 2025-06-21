using OpenGL;
using System;

namespace ZetaExt
{
    public static class BoolF
    {
        public static void Assert(bool condition)
        {
            if (!condition)
            {
                throw new ArgumentException("조건에 맞지 않습니다.");
            }
        }

    }
}
