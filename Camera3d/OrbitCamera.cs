using Common.Abstractions;
using OpenGL;
using System;
using ZetaExt;

namespace Camera3d
{
    public class OrbitCamera : Camera
    {
        public static float Epsilon = 0.5f;
        private const float MIN_FARAWAY_DISTANCE = 0.1f;
        private Vertex3f _orbitPosition;

        public override Vertex3f Position 
        {
            get => _position - _cameraForward * _distance;
            set => _orbitPosition = value;
        }

        public override Vertex3f PivotPosition
        {
            get => _position;
            set => _position = value;
        }

        public OrbitCamera(string name, float x, float y, float z, float distance) : base(name, x, y, z)
        {
            _distance = distance;
        }

        // 모델*뷰*투영 행렬

        public Matrix4x4f OrbitModelMatrix
        {
            get
            {
                return Matrix4x4F.CreateViewMatrix(_orbitPosition, _cameraRight, _cameraUp, _cameraForward).Inverse;
            }
        }

        public Matrix4x4f CameraModelMatrix
        {
            get
            {
                return Matrix4x4F.CreateViewMatrix(_position, _cameraRight, _cameraUp, _cameraForward).Inverse;
            }
        }

        public override Matrix4x4f ViewMatrix
            => Matrix4x4F.CreateViewMatrix(_orbitPosition, _cameraRight, _cameraUp, _cameraForward);

        /// <summary>
        /// 역투영 행렬을 계산합니다.
        /// </summary>
        public override Matrix4x4f ProjectiveRevMatrix
        {
            get
            {
                throw new NotImplementedException();
                return Matrix4x4f.Identity;
                float s = (float)_width / (float)_height;
                //return ProjectionMatrix4x4.MakeRevFrustumProjection(FOV, s, NEAR, FAR);
            }
        }

        public override void Update(int deltaTime)
        {
            base.Update(deltaTime);
            _orbitPosition = _position - _cameraForward * _distance;
        }

        protected override void UpdateCameraVectors()
        {
            Vertex3f direction = Vertex3f.Zero;
            float yawRad = _yaw.ToRadian();
            float pitchRad = _pitch.ToRadian();
            direction.x = Cos(yawRad) * Cos(pitchRad);
            direction.y = Sin(yawRad) * Cos(pitchRad);
            direction.z = Sin(pitchRad);

            _cameraForward = -direction.Normalized;
            _cameraRight = _cameraForward.Cross(Vertex3f.UnitZ).Normalized;
            _cameraUp = _cameraRight.Cross(_cameraForward).Normalized;

            float Cos(float radian) => (float)Math.Cos(radian);
            float Sin(float radian) => (float)Math.Sin(radian);
        }

        public void FarAway(float deltaDistance)
        {
            _distance += deltaDistance;
            if (_distance < MIN_FARAWAY_DISTANCE) { _distance = MIN_FARAWAY_DISTANCE; }
        }

        public override void GoForward(float deltaDistance)
        {
            Vertex3f forward = new Vertex3f(_cameraForward.x, _cameraForward.y, 0.0f);
            _position += forward * deltaDistance;
        }

        public override void GoRight(float deltaDistance)
        {
            Vertex3f right = new Vertex3f(_cameraRight.x, _cameraRight.y, 0.0f);
            _position += right * deltaDistance;
        }

    }
}
