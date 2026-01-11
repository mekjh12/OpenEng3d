using OpenGL;
using System;

namespace Noise
{
    /// <summary>
    /// 2D Simplex Noise 구현
    /// Ken Perlin의 Simplex Noise 알고리즘 (2001)
    /// </summary>
    public static class SimplexNoise
    {
        // 순열 테이블 (0-255)
        private static readonly byte[] perm = new byte[512];

        // 3D 그라디언트 벡터 (2D에서는 x, y만 사용)
        private static readonly Vertex3f[] grad3 = new Vertex3f[]
        {
            new Vertex3f(1,1,0), new Vertex3f(-1,1,0), new Vertex3f(1,-1,0), new Vertex3f(-1,-1,0),
            new Vertex3f(1,0,1), new Vertex3f(-1,0,1), new Vertex3f(1,0,-1), new Vertex3f(-1,0,-1),
            new Vertex3f(0,1,1), new Vertex3f(0,-1,1), new Vertex3f(0,1,-1), new Vertex3f(0,-1,-1)
        };

        static SimplexNoise()
        {
            // 기본 순열 테이블 초기화
            byte[] p = new byte[256];
            for (int i = 0; i < 256; i++)
                p[i] = (byte)i;

            // Fisher-Yates 셔플
            Random rand = new Random(0); // 고정 시드 (일관된 노이즈)
            for (int i = 255; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                byte temp = p[i];
                p[i] = p[j];
                p[j] = temp;
            }

            // 순열 테이블 복제 (래핑 방지)
            for (int i = 0; i < 512; i++)
                perm[i] = p[i & 255];
        }

        /// <summary>
        /// 2D Simplex Noise 생성
        /// </summary>
        /// <param name="x">X 좌표</param>
        /// <param name="y">Y 좌표</param>
        /// <returns>-1.0 ~ 1.0 범위의 노이즈 값</returns>
        public static float Generate(float x, float y)
        {
            const float F2 = 0.366025403f; // 0.5 * (sqrt(3.0) - 1.0)
            const float G2 = 0.211324865f; // (3.0 - sqrt(3.0)) / 6.0

            float n0, n1, n2; // 3개 코너의 기여도

            // 스큐 입력 공간 → 단순 그리드
            float s = (x + y) * F2;
            int i = FastFloor(x + s);
            int j = FastFloor(y + s);

            float t = (i + j) * G2;
            float X0 = i - t; // 단순 그리드 → 입력 공간으로 언스큐
            float Y0 = j - t;
            float x0 = x - X0; // 첫 번째 코너로부터의 거리
            float y0 = y - Y0;

            // 두 번째 코너 오프셋 결정
            int i1, j1;
            if (x0 > y0) { i1 = 1; j1 = 0; } // 하단 삼각형
            else { i1 = 0; j1 = 1; } // 상단 삼각형

            // 세 코너로부터의 오프셋
            float x1 = x0 - i1 + G2;
            float y1 = y0 - j1 + G2;
            float x2 = x0 - 1.0f + 2.0f * G2;
            float y2 = y0 - 1.0f + 2.0f * G2;

            // 그라디언트 인덱스 계산
            int ii = i & 255;
            int jj = j & 255;
            int gi0 = perm[ii + perm[jj]] % 12;
            int gi1 = perm[ii + i1 + perm[jj + j1]] % 12;
            int gi2 = perm[ii + 1 + perm[jj + 1]] % 12;

            // 각 코너의 기여도 계산
            float t0 = 0.5f - x0 * x0 - y0 * y0;
            if (t0 < 0) n0 = 0.0f;
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * Dot(grad3[gi0], x0, y0);
            }

            float t1 = 0.5f - x1 * x1 - y1 * y1;
            if (t1 < 0) n1 = 0.0f;
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * Dot(grad3[gi1], x1, y1);
            }

            float t2 = 0.5f - x2 * x2 - y2 * y2;
            if (t2 < 0) n2 = 0.0f;
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * Dot(grad3[gi2], x2, y2);
            }

            // 결과를 -1.0 ~ 1.0 범위로 스케일
            return 70.0f * (n0 + n1 + n2);
        }

        /// <summary>
        /// 다중 옥타브 노이즈 (프랙탈 브라운 운동)
        /// </summary>
        /// <param name="x">X 좌표</param>
        /// <param name="y">Y 좌표</param>
        /// <param name="octaves">옥타브 수 (레이어)</param>
        /// <param name="persistence">각 옥타브의 진폭 감소율 (0~1)</param>
        /// <param name="lacunarity">각 옥타브의 주파수 증가율 (보통 2.0)</param>
        /// <returns>-1.0 ~ 1.0 범위의 노이즈 값</returns>
        public static float GenerateOctave(float x, float y, int octaves = 4,
                                          float persistence = 0.5f, float lacunarity = 2.0f)
        {
            float total = 0;
            float frequency = 1;
            float amplitude = 1;
            float maxValue = 0;

            for (int i = 0; i < octaves; i++)
            {
                total += Generate(x * frequency, y * frequency) * amplitude;

                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return total / maxValue;
        }

        // 헬퍼 메서드
        private static int FastFloor(float x)
        {
            int xi = (int)x;
            return x < xi ? xi - 1 : xi;
        }

        private static float Dot(Vertex3f g, float x, float y)
        {
            return g.x * x + g.y * y;
        }
    }
}