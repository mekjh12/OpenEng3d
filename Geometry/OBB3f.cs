using FastMath;
using OpenGL;
using System.Runtime.InteropServices;

namespace Geometry
{
    /// <summary>
    /// 3차원 지향 바운딩 박스 (float 정밀도)
    /// <code>
    /// Matrix4x4f의 열을 직접 활용:
    /// - Column0: X축 (Right) - 크기 포함
    /// - Column1: Y축 (Forward) - 크기 포함
    /// - Column2: Z축 (Up) - 크기 포함
    /// - Column3: 중심점
    /// 
    /// 좌표계:
    ///       ------------
    ///      /           /|
    ///     /           / |
    ///     -----------/  | 
    ///    |           |  |
    ///    |     Z Y   |  |
    ///    |     |/__  |  /
    ///    |     C   X | / 
    ///    |___________|/
    /// </code>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct OBB3f
    {
        public Matrix4x4f Transform;

        /// <summary>
        /// Transform 행렬로 OBB 생성
        /// </summary>
        public OBB3f(Matrix4x4f transform)
        {
            Transform = transform;
        }

        /// <summary>
        /// 중심점과 크기, 축 정보로 OBB 생성
        /// </summary>
        /// <param name="center">중심점</param>
        /// <param name="size">각 축 방향의 전체 크기 (반지름 아님)</param>
        /// <param name="xAxis">X축 방향 (정규화된 벡터)</param>
        /// <param name="yAxis">Y축 방향 (정규화된 벡터)</param>
        /// <param name="zAxis">Z축 방향 (정규화된 벡터)</param>
        public OBB3f(Vertex3f center, Vertex3f size, Vertex3f xAxis, Vertex3f yAxis, Vertex3f zAxis)
        {
            // 각 축에 크기를 곱해서 저장
            Vertex3f scaledX = xAxis * size.x;
            Vertex3f scaledY = yAxis * size.y;
            Vertex3f scaledZ = zAxis * size.z;

            Transform = new Matrix4x4f(
                // Column0 (X축 * 크기)
                scaledX.x, scaledX.y, scaledX.z, 0,
                // Column1 (Y축 * 크기)
                scaledY.x, scaledY.y, scaledY.z, 0,
                // Column2 (Z축 * 크기)
                scaledZ.x, scaledZ.y, scaledZ.z, 0,
                // Column3 (중심점)
                center.x, center.y, center.z, 1
            );
        }

        /// <summary>
        /// AABB로부터 OBB 생성 (회전 없음, 축 정렬)
        /// </summary>
        public OBB3f(in AABB3f aabb)
        {
            Vertex3f center = aabb.Center;
            Vertex3f halfSize = aabb.Size * 0.5f;

            Transform = new Matrix4x4f(
                // X축 (크기의 절반 = 반지름)
                halfSize.x, 0, 0, 0,
                // Y축 (크기의 절반 = 반지름)
                0, halfSize.y, 0, 0,
                // Z축 (크기의 절반 = 반지름)
                0, 0, halfSize.z, 0,
                // 중심점
                center.x, center.y, center.z, 1
            );
        }

        #region 프로퍼티

        /// <summary>
        /// 중심점
        /// </summary>
        public Vertex3f Center
        {
            get => new Vertex3f(Transform[3, 0], Transform[3, 1], Transform[3, 2]);
        }

        /// <summary>
        /// X축 벡터 (크기 포함)
        /// </summary>
        public Vertex3f XAxis => new Vertex3f(Transform[0, 0], Transform[0, 1], Transform[0, 2]);

        /// <summary>
        /// Y축 벡터 (크기 포함)
        /// </summary>
        public Vertex3f YAxis => new Vertex3f(Transform[1, 0], Transform[1, 1], Transform[1, 2]);

        /// <summary>
        /// Z축 벡터 (크기 포함)
        /// </summary>
        public Vertex3f ZAxis => new Vertex3f(Transform[2, 0], Transform[2, 1], Transform[2, 2]);

        /// <summary>
        /// 정규화된 X축 방향
        /// </summary>
        public Vertex3f XAxisNormalized => XAxis.Normalized;

        /// <summary>
        /// 정규화된 Y축 방향
        /// </summary>
        public Vertex3f YAxisNormalized => YAxis.Normalized;

        /// <summary>
        /// 정규화된 Z축 방향
        /// </summary>
        public Vertex3f ZAxisNormalized => ZAxis.Normalized;

        /// <summary>
        /// 각 축의 반지름 (half-extents)
        /// </summary>
        public Vertex3f HalfExtents => new Vertex3f(
            XAxis.Module(),
            YAxis.Module(),
            ZAxis.Module()
        );

        /// <summary>
        /// 각 축의 전체 크기
        /// </summary>
        public Vertex3f Size => HalfExtents * 2f;

        /// <summary>
        /// OBB를 감싸는 구의 반지름
        /// </summary>
        public float Radius
        {
            get
            {
                Vertex3f halfExtents = HalfExtents;
                return MathFast.Sqrt(
                    halfExtents.x * halfExtents.x +
                    halfExtents.y * halfExtents.y +
                    halfExtents.z * halfExtents.z
                );
            }
        }

        /// <summary>
        /// 표면 겉넓이
        /// </summary>
        public float Area
        {
            get
            {
                Vertex3f size = Size;
                return 2.0f * (size.x * size.y + size.y * size.z + size.z * size.x);
            }
        }

        #endregion
    }
}