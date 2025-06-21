using Geometry;
using OpenGL;
using System;
using ZetaExt;
using System.Collections.Generic;


namespace Occlusion
{
    public class Occluder
    {
        public static List<Vertex3f> VertexList = new List<Vertex3f>();

        static byte[] occluderPolygonIndex = new byte[43]
        {
            0x00, 0x80, 0x81, 0, 0x82, 0xC9, 0xC8, 0, 0x83, 0xC7, 0xC6, 0, 0, 0, 0, 0,
            0x84, 0xCF, 0xCE, 0, 0xD1, 0xD9, 0xD8, 0, 0xD0, 0xD7, 0xD6, 0, 0, 0, 0, 0,
            0x85, 0xCB, 0xCA, 0, 0xCD, 0xD5, 0xD4, 0, 0xCC, 0xD3, 0xD2
        };

        static byte[,] occluderVretexIndex = new byte[26, 6]
        {
            {1, 3, 7, 5, 0, 0}, {2, 0, 4, 6, 0, 0}, {3, 2, 6, 7, 0, 0},
            {0, 1, 5, 4, 0, 0}, {4, 5, 7, 6, 0, 0}, {1, 0, 2, 3, 0, 0},
            {2, 0, 1, 5, 4, 6}, {0, 1, 3, 7, 5, 4}, {3, 2, 0, 4, 6, 7}, {1, 3, 2, 6, 7, 5},
            {1, 0, 4, 6, 2, 3}, {5, 1, 0, 2, 3, 7}, {4, 0, 2, 3, 1, 5}, {0, 2, 6, 7, 3, 1},
            {0, 4, 5, 7, 6, 2}, {4, 5, 1, 3, 7, 6}, {1, 5, 7, 6, 4, 0}, {5, 7, 3, 2, 6, 4},
            {3, 1, 5, 4, 6, 2}, {2, 3, 7, 5, 4, 0}, {1, 0, 4, 6, 7, 3}, {0, 2, 6, 7, 5, 1},
            {7, 6, 2, 0, 1, 5}, {6, 4, 0, 1, 3, 7}, {5, 7, 3, 2, 0, 4}, {4, 5, 1, 3, 2, 6},
        };

        static byte[] occluderVretexIndexLength = new byte[26]
        {
            4, 4, 4, 4, 4, 4, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6
        };

        /*
        static float[,] occlusionVertexPosition = new float[8, 3]
        {
            {0.0f, 0.0f, 0.0f}, {1.0f, 0.0f, 0.0f}, {0.0f, 1.0f, 0.0f}, {1.0f, 1.0f, 0.0f},
            {0.0f, 0.0f, 1.0f}, {1.0f, 0.0f, 1.0f}, {0.0f, 1.0f, 1.0f}, {1.0f, 1.0f, 1.0f},
        };
        */

        static Vertex3f[] occlusionVertexPosition = new Vertex3f[8]
        {
            new Vertex3f(0.0f, 0.0f, 0.0f),
            new Vertex3f(1.0f, 0.0f, 0.0f),
            new Vertex3f(0.0f, 1.0f, 0.0f),
            new Vertex3f(1.0f, 1.0f, 0.0f),
            new Vertex3f(0.0f, 0.0f, 1.0f),
            new Vertex3f(1.0f, 0.0f, 1.0f),
            new Vertex3f(0.0f, 1.0f, 1.0f),
            new Vertex3f(1.0f, 1.0f, 1.0f)
        };

        /// <summary>
        /// 오클루더에 의하여 생성되는 오클루전 영역을 만든다.
        /// </summary>
        /// <param name="frustumPlane">표.6.1.에 리스팅된 카메라공간의 4개의 옆면의 평면의 방정식</param>
        /// <param name="obbSize">오클루더의 OBB 상자 사이즈</param>
        /// <param name="Mocc">오클루더의 OBB 상자의 오브젝트공간에서 월드공간으로서 변환행렬</param>
        /// <param name="Mcam">카메라의 카메라 공간에서 월드공간으로의 변환행렬</param>
        /// <param name="occluderPlane"> 앞면과 옆면으로 이루어진 최대 13개의 평면을 반환할 수 있다.<br/>이때 반환된 평면은 오클루전 영역을 감싸고 있는 월드공간의 평면의 방정식이다.</param>
        /// <returns>생성된 평면의 갯수를 반환</returns>
        public static int MakeOcclusionRegion(Plane[] frustumPlane, Vertex3f obbSize, Matrix4x4f Mocc, Matrix4x4f Mcam, out Plane[] occluderPlane)
        {
            List<Plane> planeList = new List<Plane>();
            float[] sizeArray = (float[])obbSize;

            Vertex3f[,] polygonVertex = new Vertex3f[2, 10];
            //float[] vertexLocation = new float[10];
            float kOccluderEpsilon = 0.002f;
            uint occlusionCode = 0;
            //int planeCount = 0;

            // 카메라의 위치를 오클루전 공간의 좌표로 변환한다.
            if (Mocc.Determinant < 0.00001f)
            {
                occluderPlane = null;
                return 0;
            }

            Matrix4x4f m = (Mocc * 1000.0f).Inverse * 1000.0f;
            float[] cameraPosition = (float[])(m * Mcam.GetTranslation());

            // 6비트 오클루전 코드를 계산하고 앞면을 생성한다.
            uint axisCode = 0x01;
            for (uint i = 0; i < 3; i++, axisCode <<= 2)
            {
                Vertex3f iRow = new Vertex3f(m[0, i], m[1, i], m[2, i]);
                if (cameraPosition[i] > sizeArray[i])
                {
                    occlusionCode |= axisCode;
                    planeList.Add(new Plane(-iRow, sizeArray[i] - m[3, i]));
                }
                else if (cameraPosition[i] < 0.0f)
                {
                    occlusionCode |= axisCode << 1;
                    planeList.Add(new Plane(iRow, m[3, i]));
                }
            }

            // 오클루전 코드를 이용하여 실루엣의 꼭짓점 인덱스을 찾아온다.
            uint polygonIndex = occluderPolygonIndex[occlusionCode];
            byte vertCount = occluderVretexIndexLength[polygonIndex & 0x1F];
            byte[] vertexIndex = new byte[vertCount];
            for (int i = 0; i < vertCount; i++)
                vertexIndex[i] = occluderVretexIndex[polygonIndex & 0x1F, i];
            uint vertexCount = polygonIndex >> 5;

            // 실루엣 꼭짓점을 카메라 공간의 점으로 변환한다.
            Matrix4x4f McamInverse = Mcam.Inverse;
            Matrix4x4f t = McamInverse * Mocc; // 순서는 왼쪽에서 오른쪽 (순서를 바꿔주니 잘 작동함)

            for (int i = 0; i < vertexCount; i++)
            {
                Vertex3f p = occlusionVertexPosition[vertexIndex[i]];
                Vertex4f Pocc = new Vertex4f(p.x * sizeArray[0], p.y * sizeArray[1], p.z * sizeArray[2], 1.0f);
                polygonVertex[1, i] = (t * Pocc).xyz();
            }

            // 뷰프러스텀의 옆면으로 실루엣 다각형을 자른다.
            Vertex3f[] inVertices = new Vertex3f[vertexCount];
            for (int i = 0; i < vertexCount; i++)
                inVertices[i] = polygonVertex[1, i];

            for (int k = 0; k < 4; k++)
            {
                Plane clipPlane = frustumPlane[k];
                vertexCount = (uint)Polyhedron.ClipPolygon(inVertices, clipPlane,
                    out float[] vertexLocation, out Vertex3f[] result);
                inVertices = new Vertex3f[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    inVertices[i] = result[i];
                }
            }

            // 오클루더 테스트를 위한 카메라공간의 버텍스 리스트
            Occluder.VertexList.Clear();
            for (int i = 0; i < inVertices.Length; i++)
            {
                Occluder.VertexList.Add(inVertices[i]);
            }

            if (vertexCount < 3)
            {
                occluderPlane = null;
                return 0;
            }

            // Generate occlusion region planes in world space.
            float[,] vertexDistance = new float[2, 4];
            Vertex3f v1 = inVertices[vertexCount - 1];
            for (int k = 0; k < 4; k++)
            {
                vertexDistance[0, k] = frustumPlane[k].Normal.Dot(v1);
            }

            for (int i = 0; i < vertexCount; i++)
            {
                bool cull = false;
                int j = i & 1;

                Vertex3f v2 = inVertices[i];
                Vertex3f planeNormal = -v2.Cross(v1).Normalized;

                for (int k = 0; k < 4; k++)
                {
                    Vertex3f frustumNormal = frustumPlane[k].Normal;
                    float d = frustumNormal.Dot(v2);
                    vertexDistance[j ^ 1, k] = d;

                    // Cull edge lying in frustum plane, but only if its extrusion points inward.
                    if ((Math.Max(d, vertexDistance[j, k]) < kOccluderEpsilon) &&
                        (planeNormal.Dot(frustumNormal) > 0.0f)) cull = true;
                }

                if (!cull) planeList.Add(new Plane(planeNormal, 0.0f) * McamInverse);

                v1 = v2;
            }

            occluderPlane = planeList.ToArray();
            return planeList.Count;
        }

    }
}
