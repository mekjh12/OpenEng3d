using Common;
using OpenGL;
using System;
using ZetaExt;

namespace Sky
{
    /// <summary>
    /// 구형 지구 모델에서 정규화된 입력값(0.0-1.0)으로 태양 위치를 계산하는 클래스입니다.
    /// TODO: 태양 자전축 기울기, 날짜 고려한 태양 적위 계산 추가
    /// </summary>
    public class SimpleSunPositionCalculator
    {
        // 상수 정의
        private const double PI = Math.PI;
        private const double TWO_PI = Math.PI * 2.0;

        // 클래스 속성
        private float _latitude;      // 위도 (0.0-1.0)
        private float _longitude;     // 경도 (0.0-1.0)
        private float _dayOfYear;     // 연중 날짜 (0.0-1.0)
        private float _timeOfDay;     // 하루 중 시각 (0.0-1.0)
        private float _timeSpeed;     // 시간 흐름 속도 배율

        // 계산된 결과
        private Vertex3f _sunDirection;  // 태양 방향 벡터

        /// <summary>
        /// 태양 방향 벡터를 가져옵니다. 방향은 시점이 중심점이고 종점이 태양을 향하는 방향입니다.
        /// </summary>
        public Vertex3f SunDirection => _sunDirection;

        /// <summary>
        /// SimpleSunPositionCalculator의 생성자입니다.
        /// </summary>
        /// <param name="latitude">위도 (0.0-1.0)</param>
        /// <param name="longitude">경도 (0.0-1.0)</param>
        /// <param name="dayOfYear">연중 날짜 (0.0-1.0)</param>
        /// <param name="timeOfDay">하루 중 시각 (0.0-1.0)</param>
        /// <param name="timeSpeed">시간 흐름 속도</param>
        public SimpleSunPositionCalculator(
            float latitude = 0.90f,
            float longitude = 0.0f,
            float dayOfYear = 0.25f,
            float timeOfDay = 0.4f,
            float timeSpeed = 1.0f)
        {
            _latitude = latitude.Clamp(0.0f, 1.0f);
            _longitude = longitude.Clamp(0.0f, 1.0f);
            _dayOfYear = dayOfYear.Clamp(0.0f, 1.0f);
            _timeOfDay = timeOfDay.Clamp(0.0f, 1.0f);
            _timeSpeed = Math.Max(0.0f, timeSpeed);

            CalculateSunPosition();
        }

        /// <summary>
        /// 태양의 방향을 직접 설정합니다.
        /// </summary>
        /// <param name="direction">태양 방향 벡터 (정규화됨)</param>
        public void SetSunDirection(Vertex3f direction)
        {
            // 방향 벡터 정규화
            float length = (float)Math.Sqrt(direction.x * direction.x + direction.y * direction.y + direction.z * direction.z);

            if (length > 0.0001f) // 0으로 나누기 방지
            {
                _sunDirection = new Vertex3f(
                    direction.x / length,
                    direction.y / length,
                    direction.z / length
                );
            }
        }

        /// <summary>
        /// 시간을 업데이트합니다.
        /// </summary>
        /// <param name="duration">경과 시간 (초)</param>
        public void Update(float duration)
        {
            if (_timeSpeed <= 0.0f)
                return;

            _timeOfDay += duration;

            // 날짜 변경 처리
            while (_timeOfDay >= 1.0f)
            {
                _timeOfDay -= 1.0f;
                _dayOfYear += 1.0f / 365.0f;

                if (_dayOfYear >= 1.0f)
                    _dayOfYear -= 1.0f;
            }

            CalculateSunPosition();
        }

        /// <summary>
        /// 태양 위치를 계산합니다.
        /// </summary>
        private void CalculateSunPosition()
        {
            // 위도를 -90도에서 90도 범위로 변환
            float latitudeInRadians = (_latitude * 2.0f - 1.0f) * (float)(PI / 2.0);

            // 경도를 0도에서 360도 범위로 변환
            float longitudeInRadians = _longitude * (float)TWO_PI;

            // 하루 중 시각을 각도로 변환 (0.0-1.0 -> -pi ~ pi)
            float hourAngle = (_timeOfDay * 2.0f - 1.0f) * (float)PI;

            // 연중 일자에 따른 태양의 적위 계산 
            float declinationAngle = (float)Math.Sin((_dayOfYear - 0.25f) * TWO_PI) * (float)(23.5 * PI / 180.0);

            // 태양 방향 계산
            float sinLat = (float)Math.Sin(latitudeInRadians);
            float cosLat = (float)Math.Cos(latitudeInRadians);
            float sinDecl = (float)Math.Sin(declinationAngle);
            float cosDecl = (float)Math.Cos(declinationAngle);
            float sinHour = (float)Math.Sin(hourAngle);
            float cosHour = (float)Math.Cos(hourAngle);

            // 지역 좌표계에서의 태양 위치 계산 (z가 하늘 방향)
            // 좌표계: x(동서), y(남북), z(상하)
            float x = -sinHour;
            float y = -cosHour * sinLat;
            float z = cosHour * cosLat;

            // 경도 회전 적용 (x-y 평면 회전)
            float x2 = x * (float)Math.Cos(longitudeInRadians) - y * (float)Math.Sin(longitudeInRadians);
            float y2 = x * (float)Math.Sin(longitudeInRadians) + y * (float)Math.Cos(longitudeInRadians);

            // 정규화된 방향 벡터 계산
            Vertex3f direction = new Vertex3f(x2, y2, z);
            float length = (float)Math.Sqrt(direction.x * direction.x + direction.y * direction.y + direction.z * direction.z);

            // 정규화된 방향 벡터를 저장
            _sunDirection = new Vertex3f(
                direction.x / length,
                direction.y / length,
                direction.z / length
            );
        }

        /// <summary>
        /// 태양 위치 매개변수를 설정합니다.
        /// </summary>
        /// <param name="latitude">위도 (0.0-1.0, 0.0=남극, 0.5=적도, 1.0=북극)</param>
        /// <param name="longitude">경도 (0.0-1.0, 0.0=0°, 0.25=90°동경, 0.5=180°, 0.75=270°동경)</param>
        /// <param name="dayOfYear">연중 날짜 (0.0-1.0, 0.0=1월초, 0.25=춘분, 0.5=6월중순/하지, 0.75=추분, 1.0=12월말)</param>
        /// <param name="timeOfDay">하루 중 시각 (0.0-1.0, 0.0=자정, 0.25=일출, 0.5=정오, 0.75=일몰, 1.0=다음날 자정)</param>
        public void SetParameters(float latitude, float longitude, float dayOfYear, float timeOfDay)
        {
            _latitude = latitude.Clamp(0.0f, 1.0f);
            _longitude = longitude.Clamp(0.0f, 1.0f);
            _dayOfYear = dayOfYear.Clamp(0.0f, 1.0f);
            _timeOfDay = timeOfDay.Clamp(0.0f, 1.0f);
            CalculateSunPosition();
        }

        /// <summary>
        /// 시간 흐름 속도를 설정합니다.
        /// </summary>
        /// <param name="timeSpeed">시간 흐름 속도 (0.0 이상)</param>
        public void SetTimeSpeed(float timeSpeed)
        {
            _timeSpeed = Math.Max(0.0f, timeSpeed);
        }
    }
}