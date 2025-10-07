using OpenGL;
using System;
using ZetaExt;

namespace Animate
{
    /// <summary>
    /// 회전 행렬과 오일러 각도 변환 유틸리티<br/>
    /// 6가지 오일러 각도 순서(XYZ, XZY, YXZ, YZX, ZXY, ZYX) 모두 지원
    /// </summary>
    public static class EulerConverter
    {
        private const float GIMBAL_LOCK_THRESHOLD = 0.99999f;
        private const float RAD_TO_DEG = 180f / (float)Math.PI;
        private const float DEG_TO_RAD = (float)Math.PI / 180f;

        // ====================================================================
        // Matrix → Euler Angles (6가지 순서)
        // ====================================================================

        /// <summary>
        /// XYZ 순서: R = Rz(yaw) * Ry(roll) * Rx(pitch)<br/>
        /// 반환: (pitch, roll, yaw) - 도 단위
        /// </summary>
        public static Vertex3f MatrixToEulerXYZ(Matrix4x4f m)
        {
            float pitch, roll, yaw;

            float sy = m[0, 2];  // sin(roll)

            if (Math.Abs(sy) < GIMBAL_LOCK_THRESHOLD)
            {
                pitch = (float)Math.Atan2(-m[1, 2], m[2, 2]);
                roll = (float)Math.Asin(sy);
                yaw = (float)Math.Atan2(-m[0, 1], m[0, 0]);
            }
            else  // Gimbal lock
            {
                pitch = (float)Math.Atan2(m[1, 0], m[1, 1]);
                roll = sy > 0 ? (float)Math.PI / 2 : -(float)Math.PI / 2;
                yaw = 0f;
            }

            return new Vertex3f(pitch * RAD_TO_DEG, roll * RAD_TO_DEG, yaw * RAD_TO_DEG);
        }

        /// <summary>
        /// XZY 순서: R = Ry(yaw) * Rz(roll) * Rx(pitch)<br/>
        /// 반환: (pitch, yaw, roll) - 도 단위
        /// </summary>
        public static Vertex3f MatrixToEulerXZY(Matrix4x4f m)
        {
            float pitch, yaw, roll;

            float sz = m[1, 0];  // sin(roll)

            if (Math.Abs(sz) < GIMBAL_LOCK_THRESHOLD)
            {
                roll = (float)Math.Asin(sz);
                pitch = (float)Math.Atan2(-m[1, 2], m[1, 1]);
                yaw = (float)Math.Atan2(-m[2, 0], m[0, 0]);
            }
            else  // Gimbal lock
            {
                roll = sz > 0 ? (float)Math.PI / 2 : -(float)Math.PI / 2;
                pitch = (float)Math.Atan2(m[2, 1], m[2, 2]);
                yaw = 0f;
            }

            return new Vertex3f(pitch * RAD_TO_DEG, yaw * RAD_TO_DEG, roll * RAD_TO_DEG);
        }

        /// <summary>
        /// YXZ 순서: R = Rz(roll) * Rx(pitch) * Ry(yaw)<br/>
        /// 반환: (pitch, yaw, roll) - 도 단위
        /// </summary>
        public static Vertex3f MatrixToEulerYXZ(Matrix4x4f m)
        {
            float pitch, yaw, roll;

            float sx = -m[1, 2];  // -sin(pitch)

            if (Math.Abs(sx) < GIMBAL_LOCK_THRESHOLD)
            {
                pitch = (float)Math.Asin(sx);
                yaw = (float)Math.Atan2(m[0, 2], m[2, 2]);
                roll = (float)Math.Atan2(m[1, 0], m[1, 1]);
            }
            else  // Gimbal lock
            {
                pitch = sx > 0 ? (float)Math.PI / 2 : -(float)Math.PI / 2;
                yaw = (float)Math.Atan2(-m[0, 1], m[0, 0]);
                roll = 0f;
            }

            return new Vertex3f(pitch * RAD_TO_DEG, yaw * RAD_TO_DEG, roll * RAD_TO_DEG);
        }

        /// <summary>
        /// YZX 순서: R = Rx(pitch) * Rz(roll) * Ry(yaw)<br/>
        /// 반환: (pitch, yaw, roll) - 도 단위
        /// </summary>
        public static Vertex3f MatrixToEulerYZX(Matrix4x4f m)
        {
            float pitch, yaw, roll;

            float sz = -m[0, 1];  // -sin(roll)

            if (Math.Abs(sz) < GIMBAL_LOCK_THRESHOLD)
            {
                roll = (float)Math.Asin(sz);
                yaw = (float)Math.Atan2(m[0, 2], m[0, 0]);
                pitch = (float)Math.Atan2(m[2, 1], m[1, 1]);
            }
            else  // Gimbal lock
            {
                roll = sz > 0 ? (float)Math.PI / 2 : -(float)Math.PI / 2;
                yaw = (float)Math.Atan2(-m[2, 0], m[2, 2]);
                pitch = 0f;
            }

            return new Vertex3f(pitch * RAD_TO_DEG, yaw * RAD_TO_DEG, roll * RAD_TO_DEG);
        }

        /// <summary>
        /// ZXY 순서: R = Ry(yaw) * Rx(pitch) * Rz(roll)<br/>
        /// 반환: (pitch, yaw, roll) - 도 단위
        /// </summary>
        public static Vertex3f MatrixToEulerZXY(Matrix4x4f m)
        {
            float pitch, yaw, roll;

            float sx = m[2, 1];  // sin(pitch)

            if (Math.Abs(sx) < GIMBAL_LOCK_THRESHOLD)
            {
                pitch = (float)Math.Asin(sx);
                roll = (float)Math.Atan2(-m[2, 0], m[2, 2]);
                yaw = (float)Math.Atan2(-m[0, 1], m[1, 1]);
            }
            else  // Gimbal lock
            {
                pitch = sx > 0 ? (float)Math.PI / 2 : -(float)Math.PI / 2;
                roll = (float)Math.Atan2(m[0, 2], m[0, 0]);
                yaw = 0f;
            }

            return new Vertex3f(pitch * RAD_TO_DEG, yaw * RAD_TO_DEG, roll * RAD_TO_DEG);
        }

        /// <summary>
        /// ZYX 순서: R = Rx(pitch) * Ry(yaw) * Rz(roll)<br/>
        /// 반환: (pitch, yaw, roll) - 도 단위
        /// </summary>
        public static Vertex3f MatrixToEulerZYX(Matrix4x4f m)
        {
            float pitch, yaw, roll;

            float sy = -m[2, 0];  // -sin(yaw)

            if (Math.Abs(sy) < GIMBAL_LOCK_THRESHOLD)
            {
                yaw = (float)Math.Asin(sy);
                pitch = (float)Math.Atan2(m[2, 1], m[2, 2]);
                roll = (float)Math.Atan2(m[1, 0], m[0, 0]);
            }
            else  // Gimbal lock
            {
                yaw = sy > 0 ? (float)Math.PI / 2 : -(float)Math.PI / 2;
                pitch = (float)Math.Atan2(-m[0, 2], m[1, 2]);
                roll = 0f;
            }

            return new Vertex3f(pitch * RAD_TO_DEG, yaw * RAD_TO_DEG, roll * RAD_TO_DEG);
        }

        // ====================================================================
        // Euler Angles → Matrix (6가지 순서)
        // ====================================================================

        /// <summary>
        /// XYZ 순서: R = Rz(yaw) * Ry(roll) * Rx(pitch)
        /// </summary>
        public static Matrix4x4f EulerToMatrixXYZ(float pitch, float roll, float yaw)
        {
            Matrix4x4f rotX = Matrix4x4f.RotatedX(pitch * DEG_TO_RAD);
            Matrix4x4f rotY = Matrix4x4f.RotatedY(roll * DEG_TO_RAD);
            Matrix4x4f rotZ = Matrix4x4f.RotatedZ(yaw * DEG_TO_RAD);
            return rotZ * rotY * rotX;
        }

        /// <summary>
        /// XZY 순서: R = Ry(yaw) * Rz(roll) * Rx(pitch)
        /// </summary>
        public static Matrix4x4f EulerToMatrixXZY(float pitch, float yaw, float roll)
        {
            Matrix4x4f rotX = Matrix4x4f.RotatedX(pitch * DEG_TO_RAD);
            Matrix4x4f rotY = Matrix4x4f.RotatedY(yaw * DEG_TO_RAD);
            Matrix4x4f rotZ = Matrix4x4f.RotatedZ(roll * DEG_TO_RAD);
            return rotY * rotZ * rotX;
        }

        /// <summary>
        /// YXZ 순서: R = Rz(roll) * Rx(pitch) * Ry(yaw)
        /// </summary>
        public static Matrix4x4f EulerToMatrixYXZ(float pitch, float yaw, float roll)
        {
            Matrix4x4f rotX = Matrix4x4f.RotatedX(pitch * DEG_TO_RAD);
            Matrix4x4f rotY = Matrix4x4f.RotatedY(yaw * DEG_TO_RAD);
            Matrix4x4f rotZ = Matrix4x4f.RotatedZ(roll * DEG_TO_RAD);
            return rotZ * rotX * rotY;
        }

        /// <summary>
        /// YZX 순서: R = Rx(pitch) * Rz(roll) * Ry(yaw)
        /// </summary>
        public static Matrix4x4f EulerToMatrixYZX(float pitch, float yaw, float roll)
        {
            Matrix4x4f rotX = Matrix4x4f.RotatedX(pitch * DEG_TO_RAD);
            Matrix4x4f rotY = Matrix4x4f.RotatedY(yaw * DEG_TO_RAD);
            Matrix4x4f rotZ = Matrix4x4f.RotatedZ(roll * DEG_TO_RAD);
            return rotX * rotZ * rotY;
        }

        /// <summary>
        /// ZXY 순서: R = Ry(yaw) * Rx(pitch) * Rz(roll)
        /// </summary>
        public static Matrix4x4f EulerToMatrixZXY(float pitch, float yaw, float roll)
        {
            Matrix4x4f rotX = Matrix4x4f.RotatedX(pitch * DEG_TO_RAD);
            Matrix4x4f rotY = Matrix4x4f.RotatedY(yaw * DEG_TO_RAD);
            Matrix4x4f rotZ = Matrix4x4f.RotatedZ(roll * DEG_TO_RAD);
            return rotY * rotX * rotZ;
        }

        /// <summary>
        /// ZYX 순서: R = Rx(pitch) * Ry(yaw) * Rz(roll)
        /// </summary>
        public static Matrix4x4f EulerToMatrixZYX(float pitch, float yaw, float roll)
        {
            Matrix4x4f rotX = Matrix4x4f.RotatedX(pitch * DEG_TO_RAD);
            Matrix4x4f rotY = Matrix4x4f.RotatedY(yaw * DEG_TO_RAD);
            Matrix4x4f rotZ = Matrix4x4f.RotatedZ(roll * DEG_TO_RAD);
            return rotX * rotY * rotZ;
        }

        // ====================================================================
        // 유틸리티 함수
        // ====================================================================

        /// <summary>
        /// 오일러 각도 순서를 Enum으로 지정하여 변환
        /// </summary>
        public static Vertex3f MatrixToEuler(Matrix4x4f m, EulerOrder order)
        {
            switch (order)
            {
                case EulerOrder.XYZ: return MatrixToEulerXYZ(m);
                case EulerOrder.XZY: return MatrixToEulerXZY(m);
                case EulerOrder.YXZ: return MatrixToEulerYXZ(m);
                case EulerOrder.YZX: return MatrixToEulerYZX(m);
                case EulerOrder.ZXY: return MatrixToEulerZXY(m);
                case EulerOrder.ZYX: return MatrixToEulerZYX(m);
                default: throw new ArgumentException($"Unknown euler order: {order}");
            }
        }

        /// <summary>
        /// 오일러 각도를 행렬로 변환
        /// </summary>
        public static Matrix4x4f EulerToMatrix(float pitch, float yaw, float roll, EulerOrder order)
        {
            switch (order)
            {
                case EulerOrder.XYZ: return EulerToMatrixXYZ(pitch, yaw, roll);
                case EulerOrder.XZY: return EulerToMatrixXZY(pitch, yaw, roll);
                case EulerOrder.YXZ: return EulerToMatrixYXZ(pitch, yaw, roll);
                case EulerOrder.YZX: return EulerToMatrixYZX(pitch, yaw, roll);
                case EulerOrder.ZXY: return EulerToMatrixZXY(pitch, yaw, roll);
                case EulerOrder.ZYX: return EulerToMatrixZYX(pitch, yaw, roll);
                default: throw new ArgumentException($"Unknown euler order: {order}");
            }
        }

        /// <summary>
        /// 4x4 행렬을 스케일과 위치를 보존하면서 오일러 각도로 변환 후 재구성
        /// </summary>
        public static Matrix4x4f ApplyEulerConstraint(Matrix4x4f transform,
            Func<Vertex3f, Vertex3f> constraintFunc, EulerOrder order)
        {
            // 위치와 스케일 보존
            Vertex3f position = transform.Position;
            float scaleX = transform.Column0.xyz().Length();
            float scaleY = transform.Column1.xyz().Length();
            float scaleZ = transform.Column2.xyz().Length();

            // 오일러 각도 추출
            Vertex3f angles = MatrixToEuler(transform, order);

            // 제약 함수 적용
            Vertex3f constrainedAngles = constraintFunc(angles);

            // 행렬 재구성
            Matrix4x4f rotation = EulerToMatrix(constrainedAngles.x, constrainedAngles.y, constrainedAngles.z, order);
            Matrix4x4f scale = Matrix4x4f.Scaled(scaleX, scaleY, scaleZ);
            Matrix4x4f result = rotation * scale;

            // 위치 복원
            result[3, 0] = position.x;
            result[3, 1] = position.y;
            result[3, 2] = position.z;

            return result;
        }
    }

    /// <summary>
    /// 오일러 각도 회전 순서
    /// </summary>
    public enum EulerOrder
    {
        XYZ,  // X → Y → Z
        XZY,  // X → Z → Y
        YXZ,  // Y → X → Z
        YZX,  // Y → Z → X
        ZXY,  // Z → X → Y
        ZYX   // Z → Y → X
    }
}