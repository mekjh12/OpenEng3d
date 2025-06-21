using Common.Abstractions;
using Geometry;
using OpenGL;
using System;
using System.Collections.Generic;
using ZetaExt;

namespace Occlusion
{
    /// <summary>
    /// 3D 공간에서의 뷰 프러스텀(View Frustum)을 정의하고 관리하는 클래스
    /// </summary>
    public class ViewFrustum
    {
        /// <summary>
        /// 뷰 프러스텀을 구성하는 6개 평면의 식별자
        /// </summary>
        [Flags]
        public enum PlaneName
        {
            Near = 1,    // 근평면
            Far = 2,     // 원평면
            Left = 4,    // 좌평면
            Right = 8,   // 우평면
            Top = 16,    // 상평면
            Bottom = 32  // 하평면
        };

        /// <summary>
        /// 카메라 위치와 상향 벡터를 이용하여 상단 평면을 생성한다
        /// </summary>
        /// <param name="cameraPos">카메라 위치</param>
        /// <param name="cameraUp">카메라 상향 벡터</param>
        /// <returns>생성된 상단 평면</returns>
        public static Plane UpperPlane(Vertex3f cameraPos, Vertex3f cameraUp)
        {
            return new Plane(cameraUp, cameraPos);
        }

        /// <summary>
        /// 카메라 공간에서의 뷰 프러스텀 평면들을 생성한다
        /// </summary>
        /// <param name="g">초점거리 (g = 1.0f / tan(FOV * 0.5f))</param>
        /// <param name="s">화면 비율 (width / height)</param>
        /// <param name="n">근평면 거리</param>
        /// <param name="f">원평면 거리</param>
        /// <param name="flags">생성할 평면 지정 플래그</param>
        /// <returns>지정된 평면들의 배열</returns>
        public static Plane[] BuildFrustumPlane(float g, float s, float n, float f, PlaneName flags)
        {
            List<Plane> list = new List<Plane>();
            // 정규화를 위한 계수 계산
            float g2s2 = 1.0f / (float)Math.Sqrt(g * g + s * s);
            float g21 = 1.0f / (float)Math.Sqrt(g * g + 1);

            // 카메라 공간 기준: z=forward, x=right, y=bottom
            if (flags.HasFlag(PlaneName.Near)) list.Add(new Plane(0, 0, 1, -n));
            if (flags.HasFlag(PlaneName.Far)) list.Add(new Plane(0, 0, -1, f));
            if (flags.HasFlag(PlaneName.Left)) list.Add(new Plane(g2s2 * g, 0, g2s2 * s, 0));
            if (flags.HasFlag(PlaneName.Right)) list.Add(new Plane(g2s2 * -g, 0, g2s2 * s, 0));
            if (flags.HasFlag(PlaneName.Top)) list.Add(new Plane(0, g21 * g, g21 * 1, 0));
            if (flags.HasFlag(PlaneName.Bottom)) list.Add(new Plane(0, g21 * -g, g21 * 1, 0));

            return list.ToArray();
        }

        public static Polyhedron BuildFrustumPolyhedron(Camera camera)
        {
            float g = 1.0f / (float)Math.Tan((camera.FOV * 0.5f).ToRadian());
            return ViewFrustum.BuildFrustumPolyhedron(
                    camera.Position,
                    camera.Forward,
                    camera.Up,
                    camera.Right,
                    g,
                    camera.AspectRatio,
                    camera.NEAR,
                    camera.FAR);
        }

        /// <summary>
        /// 카메라 파라미터를 기반으로 완전한 뷰 프러스텀 다면체를 생성한다
        /// </summary>
        /// <param name="cameraPos">카메라 위치</param>
        /// <param name="cameraForward">카메라 전방 벡터</param>
        /// <param name="cameraUp">카메라 상향 벡터</param>
        /// <param name="cameraRight">카메라 우측 벡터</param>
        /// <param name="g">초점거리</param>
        /// <param name="s">화면 비율</param>
        /// <param name="n">근평면 거리</param>
        /// <param name="f">원평면 거리</param>
        /// <returns>생성된 뷰 프러스텀 다면체</returns>
        public static Polyhedron BuildFrustumPolyhedron(
        Vertex3f cameraPos, Vertex3f cameraForward, Vertex3f cameraUp, Vertex3f cameraRight,
        float g, float s, float n, float f)
        {
            Polyhedron polyhedron = new Polyhedron();

            // 뷰 프러스텀의 8개 꼭지점을 저장할 배열
            Vertex3f[] vertices = new Vertex3f[8];

            // 카메라 변환 행렬 구성
            Matrix4x4f cameraView = Matrix4x4f.Identity;
            cameraView[0, 0] = cameraRight.x; cameraView[0, 1] = cameraRight.y; cameraView[0, 2] = cameraRight.z;
            cameraView[1, 0] = cameraUp.x; cameraView[1, 1] = cameraUp.y; cameraView[1, 2] = cameraUp.z;
            cameraView[2, 0] = cameraForward.x; cameraView[2, 1] = cameraForward.y; cameraView[2, 2] = cameraForward.z;
            cameraView[3, 0] = cameraPos.x; cameraView[3, 1] = cameraPos.y; cameraView[3, 2] = cameraPos.z;

            // 근평면의 4개 꼭지점 생성
            float y = n / g, x = y * s;
            vertices[0] = (cameraView * new Vertex4f(x, y, n, 1)).xyz();    // 우상단
            vertices[1] = (cameraView * new Vertex4f(x, -y, n, 1)).xyz();   // 우하단
            vertices[2] = (cameraView * new Vertex4f(-x, -y, n, 1)).xyz();  // 좌하단
            vertices[3] = (cameraView * new Vertex4f(-x, y, n, 1)).xyz();   // 좌상단

            // 원평면의 4개 꼭지점 생성
            y = f / g; x = y * s;
            vertices[4] = (cameraView * new Vertex4f(x, y, f, 1)).xyz();    // 우상단
            vertices[5] = (cameraView * new Vertex4f(x, -y, f, 1)).xyz();   // 우하단
            vertices[6] = (cameraView * new Vertex4f(-x, -y, f, 1)).xyz();  // 좌하단
            vertices[7] = (cameraView * new Vertex4f(-x, y, f, 1)).xyz();   // 좌상단
            polyhedron.SetVertex(vertices);

            // 6개의 평면 생성 (4개의 측면 + 근평면 + 원평면)
            Matrix4x4f inverse = cameraView.Transposed;
            Plane[] planes = new Plane[6];

            // 4개의 측면 평면 생성
            planes[0] = new Plane((vertices[4] - vertices[0]).Cross(vertices[1] - vertices[0]).Normalized, vertices[0]); // 우측면
            planes[1] = new Plane((vertices[5] - vertices[1]).Cross(vertices[2] - vertices[1]).Normalized, vertices[1]); // 하단면
            planes[2] = new Plane((vertices[6] - vertices[2]).Cross(vertices[3] - vertices[2]).Normalized, vertices[2]); // 좌측면
            planes[3] = new Plane((vertices[7] - vertices[3]).Cross(vertices[4] - vertices[3]).Normalized, vertices[3]); // 상단면

            // 근평면과 원평면 생성
            Vertex3f col2 = cameraView.Column2.xyz();
            Vertex3f col3 = cameraView.Column3.xyz();
            float d = col2.Dot(col3);
            planes[4] = new Plane(col2, -(d + n));         // 근평면
            planes[5] = new Plane(-col2, col3 + col2 * f); // 원평면

            polyhedron.SetPlane(planes);

            // 12개의 모서리와 6개의 면 초기화
            Edge[] edges = new Edge[12];
            Face[] faces = new Face[6];

            for (int i = 0; i < edges.Length; i++) edges[i] = new Edge();
            for (int i = 0; i < faces.Length; i++) faces[i] = new Face();

            // 모서리와 면의 위상 구조 설정
            for (int i = 0; i < 4; i++)
            {
                // 수직 모서리 설정
                edges[i + 0].SetVertexIndex(0, (byte)i);
                edges[i + 0].SetVertexIndex(1, (byte)(i + 4));
                edges[i + 0].SetFaceIndex(0, (byte)i);
                edges[i + 0].SetFaceIndex(1, (byte)((i - 1) & 3));

                // 근평면 모서리 설정
                edges[i + 4].SetVertexIndex(0, (byte)i);
                edges[i + 4].SetVertexIndex(1, (byte)((i + 1) & 3));
                edges[i + 4].SetFaceIndex(0, 4);
                edges[i + 4].SetFaceIndex(1, (byte)i);

                // 원평면 모서리 설정
                edges[i + 8].SetVertexIndex(0, (byte)(((i + 1) & 3) + 4));
                edges[i + 8].SetVertexIndex(1, (byte)((i + 4)));
                edges[i + 8].SetFaceIndex(0, 5);
                edges[i + 8].SetFaceIndex(1, (byte)(i));

                // 측면 면 설정
                faces[i].EdgeCount = 4;
                faces[i].SetEdge(0, (ushort)(i));
                faces[i].SetEdge(1, (ushort)((i + 1) & 3));
                faces[i].SetEdge(2, (ushort)(i + 4));
                faces[i].SetEdge(3, (ushort)(i + 8));
            }

            // 근평면과 원평면 설정
            faces[4].EdgeCount = 4; faces[5].EdgeCount = 4;
            faces[4].SetEdge(0, 4); faces[5].SetEdge(0, 8);
            faces[4].SetEdge(1, 5); faces[5].SetEdge(1, 9);
            faces[4].SetEdge(2, 6); faces[5].SetEdge(2, 10);
            faces[4].SetEdge(3, 7); faces[5].SetEdge(3, 11);

            polyhedron.SetEdge(edges);
            polyhedron.SetFace(faces);

            return polyhedron;
        }

        public static Plane[] BuildFrustumPlane(Camera camera)
        {
            // Frustum의 4개의 기본 옆면을 만든다.
            Plane[] frustumPlane = ViewFrustum.BuildFrustumPlane(camera.FocusDistance, camera.AspectRatio, camera.NEAR, camera.FAR,
            ViewFrustum.PlaneName.Left | ViewFrustum.PlaneName.Right | ViewFrustum.PlaneName.Top | ViewFrustum.PlaneName.Bottom);

            return frustumPlane;
        }
    }
}
