using OpenGL;
using System;

namespace ZetaExt
{
    public static class Rand
    {
        private static Random _rnd;

        static Rand()
        {
            _rnd = new Random();
        }

        public static void InitSeed(int seed)
        {
            _rnd = new Random(seed);
        }

        /// <summary>
        /// 참과 거짓의 난수를 가져온다.
        /// </summary>
        public static bool NextBoolean
        {
            get => _rnd.NextDouble() > 0.5f ? true : false;
        }

        /// <summary>
        /// 
        /// </summary>
        public static Vertex3f NextColor3f => new Vertex3f(NextColor, NextColor, NextColor);

        /// <summary>
        /// 0.0보다 크거나 같고 1.0보다 작은 부동 소수점 난수입니다.
        /// </summary>
        /// <returns></returns>
        public static float NextFloat
        {
            get => (float)_rnd.NextDouble();
        }

        /// <summary>
        ///  -1.0보다 크거나 같고 1.0보다 작은 부동 소수점 난수입니다.
        /// </summary>
        public static float NextFloat2
        {
            get => (float)(2.0f * _rnd.NextDouble() - 1.0f);
        }

        /// <summary>
        /// start보다 크거나 같고 end보다 작은 부동 소수점 난수입니다.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static float Next(float start = 0.0f, float end = 1.0f)
        {
            return start + (end - start) * (float)_rnd.NextDouble();
        }

        public static float NextAngle360 => 360 * (float)_rnd.NextDouble();


        /// <summary>
        /// 지정된 범위 내의 임의의 정수를 반환합니다.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static int Next(int start = 0, int end = 100)
        {
            return _rnd.Next(start, end);
        }

        public static Vertex2f NextVertex2f(float range = 1.0f)
        {
            return new Vertex2f(2 * range * (float)_rnd.NextDouble() - range, 2 * range * (float)_rnd.NextDouble() - range);
        }

        /// <summary>
        /// 색깔의 랜덤을 반환한다. 0부터 1이다.
        /// </summary>
        /// <returns></returns>
        public static float NextColor => (float)_rnd.NextDouble();

        /// <summary>
        /// 0.0보다 크거나 같고 1.0보다 작은 부동 소수점 난수입니다.
        /// </summary>
        /// <returns></returns>
        public static double NextDouble()
        {
            return _rnd.NextDouble();
        }

        /// <summary>
        /// 최솟값과 최댓값 사이의 랜덤한 정수를 반환한다.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int NextInt(int min = -1, int max = 1)
        {
            return _rnd.Next(min, max + 1);
        }


        public static Vertex3f NextVector3(Vertex3f a,  Vertex3f b)
        {
            float x = Rand.Next(a.x, b.x);
            float y = Rand.Next(a.y, b.y);
            float z = Rand.Next(a.z, b.z);
            return new Vertex3f(x, y, z);
        }

        /// <summary>
        /// (-1,-1,0)-(1,1,0)사이의 랜덤한 벡터를 가져온다.
        /// </summary>
        public static Vertex3f NextVector3f => NextVector3(-Vertex3f.One, Vertex3f.One);

        public static Vertex3f NextVector3fWithPlane2d
        {
            get
            {
                Vertex3f v = NextVector3(-Vertex3f.One, Vertex3f.One);
                v.z = 0.0f;
                return v;
            }
        }

        /// <summary>
        /// (0,0,0)-(1,1,1)사이의 랜덤한 벡터를 가져온다.
        /// </summary>
        public static Vertex3f NextVectorFromZeroToOne => NextVector3(Vertex3f.Zero, Vertex3f.One);
    }
}
