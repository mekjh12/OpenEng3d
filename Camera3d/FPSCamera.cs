using Common.Abstractions;
using Geometry;
using OpenGL;
using System;
using ZetaExt;

namespace Camera3d
{
    public class FPSCamera : Camera
    {
        public FPSCamera(string name, float x, float y, float z, float yaw, float pitch) : base(name, x, y, z)
        {
            _pitch = pitch;
            _yaw = yaw;
            UpdateCameraVectors();
        }

        public override Matrix4x4f ViewMatrix =>
            Matrix4x4F.CreateViewMatrix(_position, _cameraRight, _cameraUp, _cameraForward);

        /// <summary>
        /// 역투영 행렬을 계산합니다.
        /// </summary>
        public override Matrix4x4f ProjectiveRevMatrix
        {
            get
            {
                float s = (float)_width / (float)_height;
                return Matrix4x4f.Identity;
                //return ProjectionMatrix4x4.MakeRevFrustumProjection(FOV, s, NEAR, FAR);
            }
        }

        public override void Update(int deltaTime)
        {
            base.Update(deltaTime);
        }

        protected override void UpdateCameraVectors()
        {
            Vertex3f direction = Vertex3f.Zero;
            float yawRad = _yaw.ToRadian();
            float pitchRad = _pitch.ToRadian();
            direction.x = Cos(yawRad) * Cos(pitchRad);
            direction.y = Sin(yawRad) * Cos(pitchRad);
            direction.z = Sin(pitchRad);

            _cameraForward = direction.Normalized;
            _cameraRight = _cameraForward.Cross(Vertex3f.UnitZ).Normalized;
            _cameraUp = _cameraRight.Cross(_cameraForward).Normalized;

            float Cos(float radian) => (float)Math.Cos(radian);
            float Sin(float radian) => (float)Math.Sin(radian);
        }

    }
}