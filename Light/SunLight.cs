using Common.Abstractions;
using OpenGL;
using System.Numerics;
using ZetaExt;

namespace Lights
{
    public class SunLight : DirectionalLight
    {
        private SolarAngles _angles;

        public struct SolarAngles
        {
            public float azimuth;    // 방위각 (0-360도)
            public float elevation;  // 고도각 (0-90도)

            public SolarAngles(float azimuth, float elevation)
            {
                this.azimuth = azimuth;
                this.elevation = elevation;
            }
        }

        public SunLight(float azimuth, float elevation)
        {
            _angles = new SolarAngles(azimuth, elevation);
            UpdateDirection();
        }

        public void SetSolarAngles(SolarAngles angles)
        {
            _angles = angles;
            UpdateDirection();
        }

        /// <summary>
        /// 방위각만 변경
        /// </summary>
        /// <param name="azimuth"></param>
        public void SetAzimuth(float azimuth)
        {
            // 0-360도 범위로 정규화
            azimuth = azimuth % 360f;
            if (azimuth < 0) azimuth += 360f;

            _angles.azimuth = azimuth;
            UpdateDirection();
        }

        public void SetDeltaAzimuth(float deltaAzimuth)
        {
            // 0-360도 범위로 정규화
            _angles.azimuth = (_angles.azimuth + deltaAzimuth) % 360f;
            if (_angles.azimuth < 0) _angles.azimuth += 360f;
            UpdateDirection();
        }

        /// <summary>
        /// 고도각만 변경
        /// </summary>
        /// <param name="elevation"></param>
        public void SetElevation(float elevation)
        {
            // 0-90도 범위로 제한
            elevation = elevation.Clamp(0f, 90f);

            _angles.elevation = elevation;
            UpdateDirection();
        }

        public void SetDeltaElevation(float deltaElevation)
        {
            // 0-90도 범위로 제한
            _angles.elevation += deltaElevation;
            _angles.elevation = _angles.elevation.Clamp(0f, 90f);
            UpdateDirection();
        }


        /// <summary>
        /// 태양에서 지표면을 향하는 벡터로 직접 계산
        /// </summary>
        private void UpdateDirection()
        {
            float rad_azimuth = _angles.azimuth.ToRadian();
            float rad_elevation = _angles.elevation.ToRadian();

            // 관찰자 위치에서 태양을 향하는 벡터 계산
            Vertex3f sunDirection = new Vertex3f(
                MathF.Cos(rad_elevation) * MathF.Cos(rad_azimuth),
                MathF.Cos(rad_elevation) * MathF.Sin(rad_azimuth),
                MathF.Sin(rad_elevation)
            ).Normalized;

            // 태양에서 지표면을 향하는 벡터는 이의 반대 방향
            _direction = -sunDirection;
        }
    }
}
