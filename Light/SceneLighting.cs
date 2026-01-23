using OpenGL;
using System.Numerics;

namespace Light
{
    // 편의용 데이터 클래스
    public class SceneLighting
    {
        public Vertex3f AmbientColor { get; set; }
        public float AmbientIntensity { get; set; }

        public Vertex3f SunDirection { get; set; }
        public Vertex3f SunColor { get; set; }
        public float SunIntensity { get; set; }

        public SceneLighting()
        {
            // 기본값
            AmbientColor = new Vertex3f(0.5f, 0.5f, 0.6f);
            AmbientIntensity = 1.0f;

            SunDirection = new Vertex3f(0.3f, 0.5f, -0.8f).Normalized;
            SunColor = new Vertex3f(1.0f, 0.95f, 0.85f);
            SunIntensity = 1.2f;
        }

        // UBO 구조체로 변환
        public LightingUBO ToUBO()
        {
            return new LightingUBO
            {
                ambientColor = new Vector3(
                    AmbientColor.x * AmbientIntensity,
                    AmbientColor.y * AmbientIntensity,
                    AmbientColor.z * AmbientIntensity
                ),
                lightDirection = new Vector3(
                    SunDirection.x,
                    SunDirection.y,
                    SunDirection.z
                ),
                lightColor = new Vector3(
                    SunColor.x * SunIntensity,
                    SunColor.y * SunIntensity,
                    SunColor.z * SunIntensity
                )
            };
        }

        // 시간대별 라이팅 (선택사항)
        public void SetTimeOfDay(float hour)
        {
            if (hour >= 6 && hour < 18) // 낮 (6:00 ~ 18:00)
            {
                SunIntensity = 1.2f;
                SunColor = new Vertex3f(1.0f, 0.95f, 0.85f);
                SunDirection = new Vertex3f(0.3f, 0.5f, -0.8f).Normalized;

                AmbientColor = new Vertex3f(0.4f, 0.4f, 0.5f);
                AmbientIntensity = 1.0f;
            }
            else if (hour >= 18 && hour < 20) // 저녁 (18:00 ~ 20:00)
            {
                SunIntensity = 0.8f;
                SunColor = new Vertex3f(1.0f, 0.7f, 0.5f);  // 붉은 석양
                SunDirection = new Vertex3f(0.8f, 0.3f, -0.5f).Normalized;

                AmbientColor = new Vertex3f(0.5f, 0.3f, 0.4f);
                AmbientIntensity = 0.8f;
            }
            else // 밤 (20:00 ~ 6:00)
            {
                SunIntensity = 0.2f;
                SunColor = new Vertex3f(0.7f, 0.7f, 0.9f);  // 달빛
                SunDirection = new Vertex3f(0.0f, 0.0f, -1.0f);

                AmbientColor = new Vertex3f(0.1f, 0.1f, 0.2f);
                AmbientIntensity = 0.5f;
            }
        }
    }
}
