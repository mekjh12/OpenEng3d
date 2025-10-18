using OpenGL;
using System;

namespace ZetaExt
{
    /// <summary>
    /// Quaternion 클래스에 대한 확장 메서드
    /// </summary>
    public static class QuaternionExtensions
    {
        /// <summary>
        /// 쿼터니언을 축-각도 표현으로 변환한다
        /// </summary>
        /// <param name="quaternion">변환할 쿼터니언</param>
        /// <param name="axis">회전축 (출력, 정규화된 벡터)</param>
        /// <param name="angle">회전 각도 (출력, 라디안)</param>
        public static void ToAxisAngle(this Quaternion quaternion, out Vertex3f axis, out float angle)
        {
            // 쿼터니언이 단위 쿼터니언인지 확인
            quaternion.Normalize();
            Quaternion q = quaternion;

            // w가 1에 가까우면 회전이 거의 없음 (0도 회전)
            if (Math.Abs(q.W) >= 1.0f)
            {
                axis = Vertex3f.UnitY;
                angle = 0f;
                return;
            }

            // 각도 계산: angle = 2 * acos(w)
            angle = 2.0f * (float)Math.Acos(q.W);

            // 축 계산: axis = (x, y, z) / sin(angle/2)
            float sinHalfAngle = (float)Math.Sqrt(1.0f - q.W * q.W);

            if (sinHalfAngle < 0.001f)
            {
                // sin(angle/2)가 0에 가까우면 임의의 축 사용
                axis = Vertex3f.UnitY;
            }
            else
            {
                axis = new Vertex3f(
                    q.X / sinHalfAngle,
                    q.Y / sinHalfAngle,
                    q.Z / sinHalfAngle
                );
            }
        }

        /// <summary>
        /// 한 벡터에서 다른 벡터로의 회전을 나타내는 쿼터니언을 생성한다
        /// </summary>
        /// <param name="from">시작 벡터</param>
        /// <param name="to">목표 벡터</param>
        /// <returns>from을 to로 회전시키는 쿼터니언</returns>
        public static Quaternion FromToRotation(Vertex3f from, Vertex3f to)
        {
            // 벡터 정규화
            Vertex3f v1 = from.Normalized;
            Vertex3f v2 = to.Normalized;

            // 내적 계산
            float dot = v1.Dot(v2);

            // 벡터가 거의 같은 방향이면 단위 쿼터니언 반환
            if (dot >= 0.999999f)
            {
                return Quaternion.Identity;
            }

            // 벡터가 거의 반대 방향이면 수직 축을 찾아 180도 회전
            if (dot <= -0.999999f)
            {
                // 임의의 수직 축 찾기
                Vertex3f axisFind = Vertex3f.UnitX.Cross(v1);

                if (axisFind.Length() < 0.000001f)
                {
                    // UnitX가 평행하면 UnitY 사용
                    axisFind = Vertex3f.UnitY.Cross(v1);
                }

                axisFind = axisFind.Normalized;
                return new Quaternion(axisFind, 180f);
            }

            // 일반적인 경우: 회전축과 각도 계산
            Vertex3f axis = v1.Cross(v2);

            // 쿼터니언 생성 (최적화된 공식)
            float s = (float)Math.Sqrt((1.0f + dot) * 2.0f);
            float invS = 1.0f / s;

            return new Quaternion(
                axis.x * invS,
                axis.y * invS,
                axis.z * invS,
                s * 0.5f
            );
        }
    }
}