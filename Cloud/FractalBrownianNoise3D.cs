using System.Runtime.InteropServices;

namespace Cloud
{
    public class FractalBrownianNoise3D
    {
        public static byte[] Generate(int size)
        {
            byte[] data = new byte[size*size*size];
            float[,,] src = GenerateFBM3D(size, 1, 0.9f);
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j<size; j++)
                {
                    for(int k = 0; k<size; k++)
                    {
                        int idx = i * size * size + j * size + k;
                        data[idx] = (byte)(255.0f * src[i, j, k]);
                    }
                }
            }

            return data;
        }


        private static float[,,] GenerateFBM3D(int size, int octaves, float persistence)
        {
            float[,,] volume = new float[size, size, size];
            float frequency = 0.01f; // 샘플링 간격

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        float noiseValue = 0;
                        float amplitude = 1;
                        float maxValue = 0;

                        for (int i = 0; i < octaves; i++)
                        {
                            noiseValue += PerlinNoise3D.Generate(x * frequency, y * frequency, z * frequency) * amplitude;
                            maxValue += amplitude;
                            amplitude *= persistence;
                            frequency *= 2;
                        }

                        volume[x, y, z] = noiseValue / maxValue;
                    }
                }
            }

            return volume;
        }
    }
}
