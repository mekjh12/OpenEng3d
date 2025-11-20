using Common;
using Common.Abstractions;
using Geometry;
using OpenGL;
using System;
using System.Collections.Generic;
using ZetaExt;

namespace Occlusion
{
    /// <summary>
    /// 안개 효과를 이용한 오클루전 컬링을 처리하는 클래스입니다.
    /// 카메라 위치와 안개 평면을 기반으로 오클루전 평면들을 생성하고,
    /// BVH(Bounding Volume Hierarchy)를 사용하여 보이지 않는 객체들을 효율적으로 제거합니다.
    /// </summary>
    public class FogOcclusion
    {
        /// <summary>
        /// 카메라의 위치와 시점을 기반으로 안개 영역을 계산하여 컬링 평면들의 배열을 반환
        /// </summary>
        /// <param name="camera">시점 카메라</param>
        /// <param name="fogPlane">안개 평면</param>
        /// <param name="fogDensity">안개 밀도</param>
        /// <returns>계산된 컬링 평면들의 배열</returns>
        static protected Plane[] PlanesByFogHalfSpace(Camera camera, Vertex4f fogPlane, float fogDensity)
        {
            /*
             *                          안개 영역
             *              ═════════════↑═════════════
             *              ║            ║            ║
             *              ║            ║            ║
             *     안개영역 ←║     Eye    ║→ 안개영역
             *              ║            ║            ║
             *              ║            ║            ║
             *              ═════════════↓═════════════
             *                          안개 영역
             */
            Plane fogplane = new Plane(fogPlane.x, fogPlane.y, fogPlane.z, fogPlane.w);

            Vertex3f camPosition = camera.Position;
            Plane[] parallelPlanes = CalculateParallelFogOcclusionPlane(fogplane, camPosition, fogDensity);
            Plane perpendicularPlanes = CalculatePerpendicularFogOcclusionPlane(fogplane, camPosition, camera.Forward, fogDensity);
            List<Plane> list = new List<Plane>();

            if (fogplane * camPosition > 0)
            {
                list.AddRange(parallelPlanes);
            }
            else
            {
                list.AddRange(parallelPlanes);
                list.Add(perpendicularPlanes);
            }

            return list.ToArray();
        }

        /// <summary>
        /// BVH 트리를 순회하며 안개 영역에 의한 오클루전 처리를 수행하여 보이지 않는 객체들을 제거하는 메인 함수
        /// </summary>
        /// <param name="camera">시점 카메라</param>
        /// <param name="fogPlane">안개 평면</param>
        /// <param name="fogDensity">안개 밀도</param>
        /// <param name="bvh">경계 볼륨 계층 구조</param>
        public static void OccludeFogHalfSpace(Camera camera, Vertex4f fogPlane, float fogDensity, BVH bvh)
        {
            if (fogPlane == null) return;

            Plane[] fogplanes = PlanesByFogHalfSpace(camera, fogPlane, fogDensity);

            foreach (Plane plane in fogplanes)
            {
                plane.Flip();
            }

            Queue<Node> queue = new Queue<Node>();
            queue.Enqueue(bvh.Root);
            while (queue.Count > 0)
            {
                Node currentNode = queue.Dequeue();
                if (currentNode == null) continue;

                if (currentNode.AABB.Visible(fogplanes))
                {
                    if (currentNode.Left) queue.Enqueue(currentNode.Child1);
                    if (currentNode.Right) queue.Enqueue(currentNode.Child2);
                }
                else
                {
                    currentNode.UnLinkBackCopy();
                }
            }
        }

        /// <summary>
        /// 카메라의 시야 방향에 수직인 안개 오클루전 평면을 계산하여 반환
        /// </summary>
        /// <param name="fogPlane">안개 평면</param>
        /// <param name="cameraPosition">카메라 위치</param>
        /// <param name="viewDirection">시야 방향</param>
        /// <param name="fogDensity">안개 밀도</param>
        /// <param name="maxOpticalDepth">최대 광학 깊이</param>
        /// <returns>계산된 수직 오클루전 평면</returns>
        public static Plane CalculatePerpendicularFogOcclusionPlane(Plane fogPlane, Vertex3f cameraPosition, Vertex3f viewDirection, float fogDensity, float maxOpticalDepth = 12.048f)
        {
            Vertex3f normal = viewDirection.Reject(fogPlane.Normal);
            float n2 = normal.Dot(normal);

            if (n2 > float.MinValue)
            {
                float z0 = fogPlane * cameraPosition;
                float z02 = z0 * z0;
                float z0_inv2 = 1.0f / z02;
                float sigma = 2.0f * maxOpticalDepth / fogDensity;
                float sigma2 = sigma * sigma;
                float m = sigma2 * z0_inv2 * z0_inv2;
                float r = 0.0f;

                if (m < 1.6875f)
                {
                    float u = 1.0f - m * 0.125f;
                    float u2 = u * u;

                    u -= (((u + 2.0f) * u2 - 2.0f) * u + m - 1.0f) /
                        ((u * 4.0f + 6.0f) * u2 - 2.0f);

                    float up1 = u + 1.0f, um1 = u - 1.0f;
                    r = MathF.Sqrt(sigma2 * z0_inv2 / (up1 * up1) - um1 * um1 * z02);
                }

                if (m > 1.0f)
                {
                    r = Math.Max(-z0 * MathF.Sqrt(m - 1.0f), r);
                }

                normal *= 1.0f / MathF.Sqrt(n2);
                return new Plane(normal, -normal.Dot(cameraPosition) - r);
            }

            return new Plane(normal, 0);
        }

        /// <summary>
        /// 안개 평면에 평행한 오클루전 평면들을 계산하여 배열로 반환 (카메라 위치에 따라 1-2개의 평면 생성)
        /// </summary>
        /// <param name="fogPlane">안개 평면</param>
        /// <param name="cameraPosition">카메라 위치</param>
        /// <param name="fogDensity">안개 밀도</param>
        /// <param name="maxOpticalDepth">최대 광학 깊이 (기본값: -log(1/256))</param>
        /// <returns>계산된 평행 오클루전 평면들의 배열</returns>
        public static Plane[] CalculateParallelFogOcclusionPlane(Plane fogPlane, Vertex3f cameraPosition, float fogDensity, float maxOpticalDepth = 2.048f)
        {
            /*
             *                                                ↑
             *                                         ═══════════ zmax  
             *                            ↑
             *                      ═══════════ zmax   ═══════════ zmin
             *   ════════════════════ 안개 평면 ════════════↓════════════
             *        ↑
             *   ═══════════ zmax    ═══════════ zmin 
             *                            ↓
             * 
             *   ═══════════ zmin 
             *        ↓
             */

            List<Plane> list = new List<Plane>();

            float z0 = fogPlane * cameraPosition;
            float z02 = z0 * z0;
            float sigma = 2.0f * maxOpticalDepth / fogDensity;

            if (z0 > 0)
            {
                list.Add(new Plane(-fogPlane.Normal, -MathF.Sqrt(sigma) - fogPlane.W));
                return list.ToArray();
            }

            float zmin = -MathF.Sqrt(z02 + sigma);
            list.Add(new Plane(-fogPlane.Normal, zmin - fogPlane.W));

            if (z02 < sigma) return list.ToArray();

            float zmax = -MathF.Sqrt(z02 - sigma);
            list.Add(new Plane(fogPlane.Normal, fogPlane.W - zmax));

            return list.ToArray();
        }
    }
}