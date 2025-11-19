using OpenGL;
using System;
using System.Resources;
using ZetaExt;

namespace Common.Abstractions
{
    /// <summary>
    /// 3D 카메라의 기본 기능을 제공하는 추상 클래스입니다.
    /// 카메라의 위치, 방향, 투영 행렬 등을 처리합니다.
    /// 
    /// 일반적인 경우:
    /// * 16:9 화면비, 60도 수직 FOV → 약 90도 수평 FOV
    /// * 21:9 화면비, 60도 수직 FOV → 약 100도 수평 FOV
    /// * 4:3 화면비, 60도 수직 FOV → 약 74도 수평 FOV
    /// 게임 엔진에서는 보통 수직 FOV를 기준으로 projection matrix를 만들고, 
    /// aspect ratio에 따라 수평 FOV가 자동으로 조정되는 구조를 사용합니다.
    /// </summary>
    /// <summary>3D 카메라의 기본 기능을 제공하는 추상 클래스입니다.</summary>
    public abstract class Camera : ICamera
    {
        private const float MAX_VARIANCE = 20;          // 프레임당 최대 회전 각도 (도)

        protected string _name = "";                    // 카메라 식별자
        protected const float SENSITIVITY = 1.0f;       // 카메라 회전 감도

        protected float FOV_DEGREE = 60;               // 시야각 (도)
        protected float NEAR_PLANE = 0.1f;             // 근평면 거리
        protected float FAR_PLANE = 10* 1000.0f;        // 원평면 거리(최대 10km)

        protected int _width;                          // 뷰포트 너비
        protected int _height;                         // 뷰포트 높이

        // 카메라 기저 벡터
        protected Vertex3f _cameraForward = Vertex3f.UnitZ;  // 전방 벡터 
        protected Vertex3f _cameraUp = Vertex3f.UnitY;       // 상향 벡터
        protected Vertex3f _cameraRight = Vertex3f.UnitX;    // 우측 벡터
        protected Vertex3f _position;                        // 카메라 위치
        protected float _distance = 0.0f;                    // 초점까지의 거리

        protected const float MAX_PITCH = 89;          // 최대 상하 회전 각도 (도)
        protected float _pitch = 0.0f;                 // 현재 상하 회전 각도
        protected float _yaw = 0.0f;                   // 현재 좌우 회전 각도

        public int Width => _width;                    // 뷰포트 너비
        public int Height => _height;                  // 뷰포트 높이

        /// <summary>초점거리: 1/tan(fov/2)</summary>
        public float FocalLength => 1.0f / (float)Math.Tan((FOV_DEGREE * 0.5f).ToRadian());

        /// <summary>초점과의 거리</summary>
        public float FocusDistance => 1.0f / (float)Math.Tan((FOV_DEGREE * 0.5f).ToRadian());

        /// <summary>종횡비 (width/height)</summary>
        public float AspectRatio => ((float)_width / (float)_height);

        // 상하 회전각
        public float CameraPitch { get => _pitch; set => _pitch = value; }
        // 좌우 회전각
        public float CameraYaw { get => _yaw; set => _yaw = value; }

        /// <summary>
        /// 현재 yaw 각도를 8방향(동, 북동, 북, 북서, 서, 남서, 남, 남동)으로 변환하여 반환합니다.
        /// 좌표계: x축 양의 방향 = 동쪽(0°), y축 양의 방향 = 북쪽(90°)
        /// </summary>
        public string Direction
        {
            get
            {
                const float CIRCLE = 360.0f;      // 원 각도
                const float STEP = 45.0f;         // 방위각 간격
                const float RANGE = 22.5f;        // 방위 범위
                string[] directions = new string[] { "동", "북동", "북", "북서", "서", "남서", "남", "남동" };

                // yaw 각도 정규화 (0~360도 범위로)
                float angle = _yaw % CIRCLE;
                if (angle < 0) angle += CIRCLE;

                // 각 방향은 다음과 같은 각도 범위를 가짐:
                // 동(0°), 북동(45°), 북(90°), 북서(135°), 서(180°), 남서(225°), 남(270°), 남동(315°)

                for (int i = 0; i < directions.Length; i++)
                {
                    float centerAngle = STEP * i;         // 방위 중심각
                    float startAngle = centerAngle - RANGE;    // 방위 시작각
                    float endAngle = centerAngle + RANGE;      // 방위 종료각

                    // 각도가 방위 범위 내에 있는지 확인
                    if (startAngle <= angle && angle < endAngle)
                    {
                        return $"{directions[i]}({angle:F1}°)";
                    }
                }

                // 남동-동 경계 케이스 처리 (337.5° ~ 360° 및 0° ~ 22.5°)
                if (angle >= 337.5f || angle < 22.5f)
                {
                    return $"{directions[0]}({angle:F1}°)";
                }

                // 기본값 (도달하지 않아야 함)
                return $"방향 미정({angle:F1}°)";
            }
        }

        /// <summary>시야각 (도)</summary>
        public float FOV { get => FOV_DEGREE; set => FOV_DEGREE = value; }

        // 카메라 이름
        public string Name => _name;

        // 근평면 거리
        public float NEAR { get => NEAR_PLANE; set => NEAR_PLANE = value; }
        // 원평면 거리
        public float FAR { get => FAR_PLANE; set => FAR_PLANE = value; }

        // 카메라 위치
        public virtual Vertex3f Position { get => _position; set => _position = value; }
        // 피봇 위치 (일반카메라는 카메라의 위치와 같음)
        public virtual Vertex3f PivotPosition { get => _position; set => _position = value; }
        // 전방 벡터
        public Vertex3f Forward { get => _cameraForward; set => _cameraForward = value; }
        // 상향 벡터
        public Vertex3f Up { get => _cameraUp; set => _cameraUp = value; }
        // 우측 벡터
        public Vertex3f Right { get => _cameraRight; set => _cameraRight = value; }

        // 모델 행렬
        public virtual Matrix4x4f ModelMatrix => ViewMatrix.Inverse;
        // 뷰*투영 행렬
        public virtual Matrix4x4f VPMatrix => ProjectiveMatrix * ViewMatrix;
        // 모델*뷰*투영 행렬
        public Matrix4x4f MVPMatrix => ProjectiveMatrix * ViewMatrix * ModelMatrix;

        /// <summary>카메라의 뷰 행렬을 계산합니다.</summary>
        public virtual Matrix4x4f ViewMatrix => Matrix4x4f.LookAtDirection(Position, _cameraForward, _cameraUp);
        /// <summary>피봇 위치를 기준으로 한 뷰 행렬을 계산합니다.</summary>

        /// <summary>투영 행렬을 계산합니다.</summary>
        public Matrix4x4f ProjectiveMatrix
        {
            get
            {
                float s = (float)_width / (float)_height;      // 종횡비
                return Matrix4x4F.CreateProjectionMatrix(FOV, s, NEAR, FAR);
            }
        }

        // 역투영 행렬
        public abstract Matrix4x4f ProjectiveRevMatrix { get; }

        // 초점까지의 거리
        public float Distance
        {
            get => _distance;
            set => _distance = value;
        }

        /// <summary>카메라를 초기화합니다.</summary>
        public Camera(string name, float x, float y, float z)
        {
            _name = name;
            _position = new Vertex3f(x, y, z);
        }

        public virtual void Init(int width, int height) => SetResolution(width, height);

        /// <summary>뷰포트 해상도를 설정합니다.</summary>
        public virtual void SetResolution(int width, int height)
        {
            _width = width;
            _height = height;
        }

        // 카메라 시작
        public virtual void Start() { }
        // 카메라 정지
        public virtual void Stop() { }
        // 카메라 재개
        public virtual void Resume() { }
        // 카메라 종료
        public virtual void ShutDown() { }

        public virtual void Update(int deltaTime) => UpdateCameraVectors();

        /// <summary>카메라의 기저 벡터들을 갱신합니다.</summary>
        protected abstract void UpdateCameraVectors();

        /// <summary>좌우 회전 (도)</summary>
        public virtual void Yaw(float deltaDegree)
        {
            if (Math.Abs(deltaDegree) > MAX_VARIANCE)
            {
                deltaDegree = MAX_VARIANCE;
            }

            _yaw += SENSITIVITY * deltaDegree;
            if (_yaw < -180) _yaw += 360;         // -180도 미만이면 360도 더함
            if (_yaw > 180) _yaw -= 360;          // 180도 초과면 360도 뺌
        }

        /// <summary>z축 기준 회전 (도)</summary>
        public virtual void Roll(float deltaDegree) { }

        /// <summary>상하 회전 (도)</summary>
        public virtual void Pitch(float deltaDegree)
        {
            if (Math.Abs(deltaDegree) > MAX_VARIANCE) return;
            _pitch += SENSITIVITY * deltaDegree;
            _pitch = _pitch.Clamp(-MAX_PITCH, MAX_PITCH);     // -89도에서 89도 사이로 제한
        }

        /// <summary>전방 이동</summary>
        public virtual void GoForward(float deltaDistance) => _position += _cameraForward * deltaDistance;

        /// <summary>상향 이동</summary>
        public virtual void GoUp(float deltaDistance) => _position += _cameraUp * deltaDistance;

        /// <summary>우측 이동</summary>
        public virtual void GoRight(float deltaDistance) => _position += _cameraRight * deltaDistance;

        /// <summary>지정된 위치로 이동</summary>
        public virtual void GoTo(Vertex3f position) => _position = position;
    }
}