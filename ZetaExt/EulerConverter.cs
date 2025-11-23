using OpenGL;
using System;

namespace ZetaExt
{
    /// <summary>
    /// 회전 행렬과 오일러 각도 변환 유틸리티
    /// <br/>
    /// 6가지 오일러 각도 순서(xYZ, xZY, YxZ, YZx, ZxY, ZYx) 모두 지원
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
        /// xYZ 순서: R = Rz(yaw) * Ry(roll) * Rx(pitch)<br/>
        /// 반환: (pitch, roll, yaw) - 도 단위
        /// </summary>
        public static EulerAngle MatrixToEulerxYZ(Matrix4x4f m)
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

            return new EulerAngle(pitch * RAD_TO_DEG, roll * RAD_TO_DEG, yaw * RAD_TO_DEG);
        }

        /// <summary>
        /// xZY 순서: R = Ry(yaw) * Rz(roll) * Rx(pitch)<br/>
        /// 반환: (pitch, roll, yaw) - 도 단위
        /// </summary>
        public static EulerAngle MatrixToEulerxZY(Matrix4x4f m)
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

            return new EulerAngle(pitch * RAD_TO_DEG, roll * RAD_TO_DEG, yaw * RAD_TO_DEG);
        }

        /// <summary>
        /// YxZ 순서: R = Rz(roll) * Rx(pitch) * Ry(yaw)<br/>
        /// 반환: (pitch, roll, yaw) - 도 단위
        /// </summary>
        public static EulerAngle MatrixToEulerYxZ(Matrix4x4f m)
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

            return new EulerAngle(pitch * RAD_TO_DEG, roll * RAD_TO_DEG, yaw * RAD_TO_DEG);
        }

        /// <summary>
        /// YZx 순서: R = Rx(pitch) * Rz(roll) * Ry(yaw)<br/>
        /// 반환: (pitch, roll, yaw) - 도 단위
        /// </summary>
        public static EulerAngle MatrixToEulerYZx(Matrix4x4f m)
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

            return new EulerAngle(pitch * RAD_TO_DEG, roll * RAD_TO_DEG, yaw * RAD_TO_DEG);
        }

        /// <summary>
        /// ZxY 순서: R = Ry(yaw) * Rx(pitch) * Rz(roll)<br/>
        /// 반환: (pitch, roll, yaw) - 도 단위
        /// </summary>
        public static EulerAngle MatrixToEulerZxY(Matrix4x4f m)
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

            return new EulerAngle(pitch * RAD_TO_DEG, roll * RAD_TO_DEG, yaw * RAD_TO_DEG);
        }

        /// <summary>
        /// ZYx 순서: R = Rx(pitch) * Ry(yaw) * Rz(roll)<br/>
        /// 반환: (pitch, roll, yaw) - 도 단위
        /// </summary>
        public static EulerAngle MatrixToEulerZYx(Matrix4x4f m)
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

            return new EulerAngle(pitch * RAD_TO_DEG, roll * RAD_TO_DEG, yaw * RAD_TO_DEG);
        }

        // ====================================================================
        // Euler Angles → Matrix (6가지 순서)
        // ====================================================================

        /// <summary>
        /// xYZ 순서: R = Rz(yaw) * Ry(roll) * Rx(pitch)
        /// </summary>
        public static Matrix4x4f EulerToMatrixxYZ(float pitch, float roll, float yaw)
        {
            Matrix4x4f rotx = Matrix4x4f.RotatedX(pitch);
            Matrix4x4f rotY = Matrix4x4f.RotatedY(roll);
            Matrix4x4f rotZ = Matrix4x4f.RotatedZ(yaw);
            return rotZ * rotY * rotx;
        }

        /// <summary>
        /// xYZ 순서: R = Rz(yaw) * Ry(roll) * Rx(pitch)
        /// </summary>
        public static Matrix4x4f EulerToMatrixxYZ(EulerAngle angle)
        {
            return EulerToMatrixxYZ(angle.Pitch, angle.Roll, angle.Yaw);
        }

        /// <summary>
        /// xZY 순서: R = Ry(yaw) * Rz(roll) * Rx(pitch)
        /// </summary>
        public static Matrix4x4f EulerToMatrixxZY(float pitch, float roll, float yaw)
        {
            Matrix4x4f rotx = Matrix4x4f.RotatedX(pitch);
            Matrix4x4f rotY = Matrix4x4f.RotatedY(yaw);
            Matrix4x4f rotZ = Matrix4x4f.RotatedZ(roll);
            return rotY * rotZ * rotx;
        }

        /// <summary>
        /// xZY 순서: R = Ry(yaw) * Rz(roll) * Rx(pitch)
        /// </summary>
        public static Matrix4x4f EulerToMatrixxZY(EulerAngle angle)
        {
            return EulerToMatrixxZY(angle.Pitch, angle.Roll, angle.Yaw);
        }

        /// <summary>
        /// YxZ 순서: R = Rz(roll) * Rx(pitch) * Ry(yaw)
        /// </summary>
        public static Matrix4x4f EulerToMatrixYxZ(float pitch, float roll, float yaw)
        {
            Matrix4x4f rotx = Matrix4x4f.RotatedX(pitch);
            Matrix4x4f rotY = Matrix4x4f.RotatedY(yaw);
            Matrix4x4f rotZ = Matrix4x4f.RotatedZ(roll);
            return rotZ * rotx * rotY;
        }

        /// <summary>
        /// YxZ 순서: R = Rz(roll) * Rx(pitch) * Ry(yaw)
        /// </summary>
        public static Matrix4x4f EulerToMatrixYxZ(EulerAngle angle)
        {
            return EulerToMatrixYxZ(angle.Pitch, angle.Roll, angle.Yaw);
        }

        /// <summary>
        /// YZx 순서: R = Rx(pitch) * Rz(roll) * Ry(yaw)
        /// </summary>
        public static Matrix4x4f EulerToMatrixYZx(float pitch, float roll, float yaw)
        {
            Matrix4x4f rotx = Matrix4x4f.RotatedX(pitch);
            Matrix4x4f rotY = Matrix4x4f.RotatedY(yaw);
            Matrix4x4f rotZ = Matrix4x4f.RotatedZ(roll);
            return rotx * rotZ * rotY;
        }

        /// <summary>
        /// YZx 순서: R = Rx(pitch) * Rz(roll) * Ry(yaw)
        /// </summary>
        public static Matrix4x4f EulerToMatrixYZx(EulerAngle angle)
        {
            return EulerToMatrixYZx(angle.Pitch, angle.Roll, angle.Yaw);
        }

        /// <summary>
        /// ZxY 순서: R = Ry(yaw) * Rx(pitch) * Rz(roll)
        /// </summary>
        public static Matrix4x4f EulerToMatrixZxY(float pitch, float roll, float yaw)
        {
            Matrix4x4f rotx = Matrix4x4f.RotatedX(pitch);
            Matrix4x4f rotY = Matrix4x4f.RotatedY(yaw);
            Matrix4x4f rotZ = Matrix4x4f.RotatedZ(roll);
            return rotY * rotx * rotZ;
        }

        /// <summary>
        /// ZxY 순서: R = Ry(yaw) * Rx(pitch) * Rz(roll)
        /// </summary>
        public static Matrix4x4f EulerToMatrixZxY(EulerAngle angle)
        {
            return EulerToMatrixZxY(angle.Pitch, angle.Roll, angle.Yaw);
        }

        /// <summary>
        /// ZYx 순서: R = Rx(pitch) * Ry(yaw) * Rz(roll)
        /// </summary>
        public static Matrix4x4f EulerToMatrixZYx(float pitch, float roll, float yaw)
        {
            Matrix4x4f rotx = Matrix4x4f.RotatedX(pitch);
            Matrix4x4f rotY = Matrix4x4f.RotatedY(yaw);
            Matrix4x4f rotZ = Matrix4x4f.RotatedZ(roll);
            return rotx * rotY * rotZ;
        }

        /// <summary>
        /// ZYx 순서: R = Rx(pitch) * Ry(yaw) * Rz(roll)
        /// </summary>
        public static Matrix4x4f EulerToMatrixZYx(EulerAngle angle)
        {
            return EulerToMatrixZYx(angle.Pitch, angle.Roll, angle.Yaw);
        }

        // ====================================================================
        // 유틸리티 함수
        // ====================================================================

        /// <summary>
        /// 오일러 각도 순서를 Enum으로 지정하여 변환
        /// </summary>
        public static EulerAngle MatrixToEuler(Matrix4x4f m, EulerOrder order)
        {
            switch (order)
            {
                case EulerOrder.xYZ: return MatrixToEulerxYZ(m);
                case EulerOrder.xZY: return MatrixToEulerxZY(m);
                case EulerOrder.YxZ: return MatrixToEulerYxZ(m);
                case EulerOrder.YZx: return MatrixToEulerYZx(m);
                case EulerOrder.ZxY: return MatrixToEulerZxY(m);
                case EulerOrder.ZYx: return MatrixToEulerZYx(m);
                default: throw new ArgumentException($"Unknown euler order: {order}");
            }
        }

        /// <summary>
        /// 오일러 각도를 행렬로 변환 (개별 float 값)
        /// </summary>
        public static Matrix4x4f EulerToMatrix(float pitch, float roll, float yaw, EulerOrder order)
        {
            switch (order)
            {
                case EulerOrder.xYZ: return EulerToMatrixxYZ(pitch, roll, yaw);
                case EulerOrder.xZY: return EulerToMatrixxZY(pitch, roll, yaw);
                case EulerOrder.YxZ: return EulerToMatrixYxZ(pitch, roll, yaw);
                case EulerOrder.YZx: return EulerToMatrixYZx(pitch, roll, yaw);
                case EulerOrder.ZxY: return EulerToMatrixZxY(pitch, roll, yaw);
                case EulerOrder.ZYx: return EulerToMatrixZYx(pitch, roll, yaw);
                default: throw new ArgumentException($"Unknown euler order: {order}");
            }
        }

        /// <summary>
        /// 오일러 각도를 행렬로 변환 (EulerAngle 구조체)
        /// </summary>
        public static Matrix4x4f EulerToMatrix(EulerAngle angle, EulerOrder order)
        {
            return EulerToMatrix(angle.Pitch, angle.Roll, angle.Yaw, order);
        }

        /// <summary>
        /// 4x4 행렬을 스케일과 위치를 보존하면서 오일러 각도로 변환 후 재구성
        /// </summary>
        public static Matrix4x4f ApplyEulerConstraint(Matrix4x4f transform,
            Func<EulerAngle, EulerAngle> constraintFunc, EulerOrder order)
        {
            // 위치와 스케일 보존
            Vertex3f position = transform.Position;
            float scalex = transform.Column0.xyz().Length();
            float scaleY = transform.Column1.xyz().Length();
            float scaleZ = transform.Column2.xyz().Length();

            // 오일러 각도 추출
            EulerAngle angles = MatrixToEuler(transform, order);

            // 제약 함수 적용
            EulerAngle constrainedAngles = constraintFunc(angles);

            // 행렬 재구성
            Matrix4x4f rotation = EulerToMatrix(constrainedAngles, order);
            Matrix4x4f scale = Matrix4x4f.Scaled(scalex, scaleY, scaleZ);
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
        xYZ,  // x → Y → Z
        xZY,  // x → Z → Y
        YxZ,  // Y → x → Z
        YZx,  // Y → Z → x
        ZxY,  // Z → x → Y
        ZYx   // Z → Y → x
    }
}