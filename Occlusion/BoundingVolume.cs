using Geometry;
using OpenGL;
using System;
using ZetaExt;

namespace Occlusion
{
    public class BoundingVolume
    {
        /// <summary>
        /// 다면체 안에 스팟광원이 포함되는지를 판별한다.
        /// </summary>
        /// <param name="planes"></param>
        /// <param name="spotLightPosition"></param>
        /// <param name="maxDistance"></param>
        /// <param name="lightDirection"></param>
        /// <param name="outerCutoff"></param>
        /// <returns></returns>
        public static bool SpotLightVisible(Plane[] planes, Vertex3f spotLightPosition, float maxDistance, Vertex3f lightDirection, float outerCutoff)
        {
            int contactCount = 0;
            foreach (Plane plane in planes)
            {
                Vertex3f p = spotLightPosition;
                float R = maxDistance;
                Vertex3f d = lightDirection.Normalized;
                Vertex3f n = plane.Normal.Normalized;
                float cutoff = outerCutoff;

                float d0 = plane * p;

                // test1
                if (d0 >= 0) { contactCount++; continue; }

                // test2
                if (d0 <= -R) return false;

                // test3
                float dn = d.Dot(n);
                if (dn >= -d0 / R) { contactCount++; continue; }

                // test 4
                float r = (float)Math.Sqrt(R * R - d0 * d0);
                float dXn = Math.Abs(d.Cross(n).Length());
                if (cutoff < ((r * dXn) - d0 * dn) / R) { contactCount++; continue; }
            }

            return (contactCount == 6);
        }

        /// <summary>
        /// 평면들 중 적어도 하나에 완전히 앞쪽에 있는지 여부를 검사한다.
        /// </summary>
        /// <param name="planes"></param>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static bool SphereHidableAtLeastPlanes(Plane[] planes, Vertex3f center, float radius)
        {
            if (planes == null) return false;

            for (int i = 0; i < planes.Length; i++)
            {
                if ((planes[i] * center) > radius) return true;
            }

            return false;
        }

        /// <summary>
        /// 평면들에 의하여 모두 완전히 앞쪽에 있는지 여부를 검사한다.
        /// </summary>
        /// <param name="planes"></param>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static bool SphereHidable(Plane[] planes, Vertex3f center, float radius)
        {
            if (planes == null) return false;

            for (int i = 0; i < planes.Length; i++)
            {
                // 적어도 하나가 완전히 앞쪽에 있지 못하면 거짓을 반환한다.
                if ((planes[i] * center) < radius) return false;
            }

            return true;
        }

        /// <summary>
        /// 평면들에 의하여 모두 완전히 뒤쪽에 있는지 여부를 검사한다.
        /// </summary>
        /// <param name="planes"></param>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static bool SphereVisible(Plane[] planes, Vertex3f center, float radius)
        {
            if (planes == null) return true;

            float negativeRadius = -radius;

            for (int i = 0; i < planes.Length; i++)
            {
                if ((planes[i] * center) < negativeRadius) return false;
            }

            return true;
        }

        /// <summary>
        /// OOB가 구의 접촉하는지 검사한다.
        /// </summary>
        /// <param name="lightPosition"></param>
        /// <param name="rmax"></param>
        /// <param name="obb"></param>
        /// <returns></returns>
        public static bool OrientedBoxIlluminated(Vertex3f spherePosition, float sphereRadius, OBB obb)
        {
            if (obb == null) return false;

            Vertex3f v = obb.Center - spherePosition;
            float vs = v.Dot(obb.Axis[0]).Abs();
            float vt = v.Dot(obb.Axis[1]).Abs();
            float vu = v.Dot(obb.Axis[2]).Abs();

            float v2 = v.Dot(v);
            float m = (obb.Size.x * vs + obb.Size.y * vt + obb.Size.z * vu) * (float)(Math.Sqrt(v2)) + sphereRadius;

            if (v2 >= m * m) return false;

            return (Math.Max(Math.Max(vs - obb.Size.x, vt - obb.Size.y), vu - obb.Size.z) < sphereRadius);
        }

        /// <summary>
        /// # 다면체 안에 OBB도 포함되어지는지 판별한다.<br/>
        /// - 평면들의 안쪽으로 이루어진 프러스텀 안에 OBB가 모두 포함되는지 여부를 반환한다.<br/>
        /// - OBB의 모든 점이 프러스텀 안에 들어오면 포함되어지는 것으로 판별한다. <br/>
        /// </summary>
        /// <param name="planes"></param>
        /// <param name="obb"></param>
        /// <returns></returns>
        public static bool OrientedBoxIncluded(Plane[] planes, OBB obb)
        {
            if (planes == null) return true;

            Vertex3f center = obb.Center;
            Vertex3f size = obb.Size;
            Vertex3f[] axis = obb.Axis;

            for (int k = 0; k < planes.Length; k++)
            {
                Plane plane = planes[k];
                Vertex3f normal = plane.Normal;

                // rg는 OBB의 중심으로부터 평면에 수직방향으로 가장 먼점까지의 거리
                float rg = Math.Abs(normal.Dot(axis[0] * size.x)) +
                     Math.Abs(normal.Dot(axis[1] * size.y)) +
                      Math.Abs(normal.Dot(axis[2] * size.z));

                float distance = plane * center;
                if (distance - rg < 0) return false;
            }

            return true;
        }

        /// <summary>
        /// # 다면체 안에 OBB도 접촉하는지 판별한다.<br/>
        /// - 평면들의 안쪽으로 이루어진 프러스텀 안에 OBB가 접촉하는지 여부를 반환한다.<br/>
        /// - OBB의 한 점이라도 프러스텀안에 들어오면 접촉하는 것으로 판별한다. <br/>
        /// </summary>
        /// <param name="planes"></param>
        /// <param name="obb"></param>
        /// <returns></returns>
        public static bool OrientedBoxVisible(Plane[] planes, OBB obb)
        {
            if (planes == null) return true;

            Vertex3f center = obb.Center;
            Vertex3f size = obb.Size;
            Vertex3f[] axis = obb.Axis;

            for (int i = 0; i < planes.Length; i++)
            {
                Plane plane = planes[i];
                Vertex3f normal = plane.Normal;

                // rg는 OBB의 중심으로부터 평면에 수직방향으로 가장 먼점까지의 거리
                float rg = Math.Abs(normal.Dot(axis[0] * size.x)) +
                     Math.Abs(normal.Dot(axis[1] * size.y)) +
                      Math.Abs(normal.Dot(axis[2] * size.z));

                float distance = plane * center;
                if (distance + rg <= 0) return false;
            }
            return true;
        }

        /// <summary>
        /// AABB 바운딩 객체를 만든다.
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="center"></param>
        /// <param name="size"></param>
        public static void CalculateAABB(Vertex3f[] vertices, out Vertex3f center, out Vertex3f size)
        {
            Vertex3f vmin = vertices[0], vmax = vertices[0];
            for (int i = 1; i < vertices.Length; i++)
            {
                vmin = Min(vmin, vertices[i]);
                vmax = Max(vmax, vertices[i]);
            }

            center = (vmin + vmax) * 0.5f;
            size = (vmax - vmin) * 0.5f;

            Vertex3f Min(Vertex3f a, Vertex3f b)
            {
                float x = Math.Min(a.x, b.x);
                float y = Math.Min(a.y, b.y);
                float z = Math.Min(a.z, b.z);
                return new Vertex3f(x, y, z);
            }

            Vertex3f Max(Vertex3f a, Vertex3f b)
            {
                float x = Math.Max(a.x, b.x);
                float y = Math.Max(a.y, b.y);
                float z = Math.Max(a.z, b.z);
                return new Vertex3f(x, y, z);
            }
        }

        /// <summary>
        /// 바운딩 구를 만든다. 
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        public static void CalculateBoundingSphere(Vertex3f[] vertices, out Vertex3f center, out float radius)
        {
            // Determine initial center and radius.
            float d2 = OBBUtility.CalculateDiameter(vertices, out int a, out int b);
            center = (vertices[a] + vertices[b]) * 0.5f;
            radius = d2 * 0.5f;

            // Make pass through vertices and adjust sphere as necessary.
            for (int i = 0; i < vertices.Length; i++)
            {
                Vertex3f pv = vertices[i] - center;
                float m2 = pv.Length();
                if (m2 > radius)
                {
                    Vertex3f q = center - (pv * (radius / m2));
                    center = (q + vertices[i]) * 0.5f;
                    radius = (q - center).Length();
                }
            }
        }
    }

}
