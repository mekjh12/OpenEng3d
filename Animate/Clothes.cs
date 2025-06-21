using Model3d;
using OpenGL;
using System;
using System.Collections.Generic;
using ZetaExt;

namespace Animate
{
    class Clothes
    {
        public static TexturedModel WearAssignWeightTransfer(TexturedModel skinModel, string clothFileName)
        {
            TexturedModel texturedModel = AniXmlLoader.LoadOnlyGeometryMesh(clothFileName);

            if (!skinModel.IsDrawElement)
            {
                Vertex3f[] vertices = skinModel.Vertices;

                // AABB바운딩 구하기
                float maxValue = float.MinValue;
                float minValue = float.MaxValue;
                for (int i = 0; i < vertices.Length; i++)
                {
                    maxValue = maxValue < vertices[i].z ? vertices[i].z : maxValue;
                    minValue = minValue > vertices[i].z ? vertices[i].z : minValue;
                }
                float epsilon = 0.3f * (maxValue - minValue);

                int numTriangle = vertices.Length / 3;

                // t,u,v구하는 역행렬 구하기 
                Matrix3x3f[] invMat = new Matrix3x3f[numTriangle];
                for (int i = 0; i < numTriangle; i++)
                {
                    Vertex3f v1 = vertices[3 * i + 0];
                    Vertex3f v2 = vertices[3 * i + 1];
                    Vertex3f v3 = vertices[3 * i + 2];
                    Vertex3f e1 = v2 - v1;
                    Vertex3f e2 = v3 - v1;
                    Vertex3f d = e1.Cross(e2);
                    Matrix3x3f m = Matrix3x3f.Identity;
                    m[0, 0] = -d.x; m[0, 1] = -d.y; m[0, 2] = -d.z;
                    m[1, 0] = e1.x; m[1, 1] = e1.y; m[1, 2] = e1.z;
                    m[2, 0] = e2.x; m[2, 1] = e2.y; m[2, 2] = e2.z;
                    invMat[i] = (m * 1000.0f).Inverse * 1000.0f;
                }
                Console.WriteLine("invMat complete.");

                //
                Vertex3f[] clothPoints = texturedModel.Vertices;

                float[] mapValue = new float[texturedModel.Vertices.Length];
                int[] mapIndex = new int[texturedModel.Vertices.Length];
                for (int j = 0; j < clothPoints.Length; j++)
                {
                    mapValue[j] = float.MaxValue;
                    mapIndex[j] = -1;
                }

                List<Vertex4i> lstBoneIndex = new List<Vertex4i>();
                List<Vertex4f> lstBoneWeight = new List<Vertex4f>();
                Vertex3f[] tuv = new Vertex3f[clothPoints.Length];

                for (int j = 0; j < clothPoints.Length; j++)
                {
                    Vertex3f clothPoint = clothPoints[j];
                    float min = float.MaxValue;
                    int minIndex = 0;
                    for (int i = 0; i < numTriangle; i++)
                    {
                        Vertex3f v1 = vertices[3 * i + 0];
                        Vertex3f v2 = vertices[3 * i + 1];
                        Vertex3f v3 = vertices[3 * i + 2];
                        Vertex3f res = invMat[i] * (clothPoint - v1);

                        float t = res.x;
                        float u = res.y;
                        float v = res.z;

                        float distance = Math.Min((clothPoint - v3).Norm(), Math.Min((clothPoint - v1).Norm(), (clothPoint - v2).Norm()));
                        if (distance < min && t > 0)
                        {
                            minIndex = i;
                            min = distance;
                        }

                        if (t < 0 && u + v < 1 && u > 0 && v > 0 && u < 1 && v < 1)
                        {
                            if (mapValue[j] < t)
                            {
                                mapValue[j] = t;
                                mapIndex[j] = i;
                                tuv[j] = new Vertex3f(t, u, v);
                            }
                        }
                    }

                    if (mapIndex[j] == -1)
                    {
                        mapIndex[j] = minIndex;
                    }

                    if (j % 100 == 0) Console.WriteLine($"{j}/{clothPoints.Length}");
                    int bestSkinTriangle = mapIndex[j];

                    lstBoneIndex.Add(skinModel.BoneIndices[3 * bestSkinTriangle]);
                    lstBoneWeight.Add(skinModel.BoneWeights[3 * bestSkinTriangle]);
                }

                texturedModel.Init(boneIndex: lstBoneIndex.ToArray(), boneWeight: lstBoneWeight.ToArray());
                texturedModel.GpuBind();
            }

            return texturedModel;
        }


        /// <summary>
        /// * 폴리곤을 표현하기 위한 삼각형들의 꼭짓점이 분리된 것들을 같은 위치의 버텍스를 같은 버텍스로 만든다.<br/>
        /// * v1-v2-v2 와 v4-v5-v6 삼각형에서 v1=v4, v2=v6이 같으므로 하나의 v1, v2로만 폴리곤을 만든다.<br/>
        /// </summary>
        /// <param name="lstPositions"></param>
        /// <param name="meshTriangles"></param>
        /// <param name="pList"></param>
        /// <param name="normals"></param>
        /// <param name="map"></param>
        /// <param name="expandValue"></param>
        private static void MergeOneTopology(List<Vertex3f> lstPositions, MeshTriangles meshTriangles,
            out List<Vertex3f> pList, out Vertex3f[] normals, out Dictionary<uint, uint> map,
            float expandValue = 0.0001f)
        {
            // 중복되는 점을 찾아 단일화 된 딕셔너리를 만든다.
            map = new Dictionary<uint, uint>();
            pList = new List<Vertex3f>();

            // 단일화 된 점 리스트를 만든다.
            for (uint i = 0; i < lstPositions.Count; i++)
            {
                bool isEqual = false;
                Vertex3f v = lstPositions[(int)i];
                for (uint j = 0; j < pList.Count; j++)
                {
                    Vertex3f p = pList[(int)j];
                    if (p.IsEqual(v, 0.00001f))
                    {
                        isEqual = true;
                        map[i] = j;
                        break;
                    }
                }

                if (!isEqual)
                {
                    map[i] = (uint)pList.Count;
                    pList.Add(v);
                }
            }

            // 삼각형 인덱스 리스트를 만든다.
            List<uint> indices = new List<uint>();
            for (int i = 0; i < meshTriangles.Vertices.Count; i++)
            {
                indices.Add(map[meshTriangles.Vertices[i]]);
            }

            // 단일화된 점의 법선벡터를 평균하여 법선벡터 리스트를 만든다.
            Vertex4f[] nors = new Vertex4f[pList.Count];
            for (int i = 0; i < indices.Count; i += 3)
            {
                int a = (int)indices[i];
                int b = (int)indices[i + 1];
                int c = (int)indices[i + 2];
                Vertex3f va = pList[a];
                Vertex3f vb = pList[b];
                Vertex3f vc = pList[c];
                Vertex3f n = (vb - va).Cross(vc - va).Normalized;

                nors[a].x += n.x;
                nors[a].y += n.y;
                nors[a].z += n.z;
                nors[a].w += 1.0f;

                nors[b].x += n.x;
                nors[b].y += n.y;
                nors[b].z += n.z;
                nors[b].w += 1.0f;

                nors[c].x += n.x;
                nors[c].y += n.y;
                nors[c].z += n.z;
                nors[c].w += 1.0f;
            }

            normals = new Vertex3f[nors.Length];
            for (int i = 0; i < nors.Length; i++)
            {
                float w = nors[i].w;
                normals[i].x = nors[i].x / w;
                normals[i].y = nors[i].y / w;
                normals[i].z = nors[i].z / w;
            }
        }

        /// <summary>
        /// 모델을 원점을 중심으로 팽창, 수축한다. 
        /// </summary>
        /// <param name="lstPositions"></param>
        /// <param name="lstTexCoord"></param>
        /// <param name="lstBoneIndex"></param>
        /// <param name="lstBoneWeight"></param>
        /// <param name="meshTriangles"></param>
        /// <param name="expandValue">팽창이면 양수, 수축이면 음수를 지정한다.</param>
        /// <returns></returns>
        public static RawModel3d Expand(List<Vertex3f> lstPositions, List<Vertex2f> lstTexCoord, 
            List<Vertex4i> lstBoneIndex, List<Vertex4f> lstBoneWeight,
            MeshTriangles meshTriangles, float expandValue = 0.0001f)
        {
            MergeOneTopology(lstPositions, meshTriangles, out List<Vertex3f> pList, out Vertex3f[] normals, out Dictionary<uint, uint> map, expandValue);

            // 단일화 점리스트의 점벡터를 팽창한다.
            for (int i = 0; i < pList.Count; i++)
            {
                pList[i] += new Vertex3f(normals[i].x, normals[i].y, normals[i].z) * expandValue;
            }

            // 단일화된 점 벡터리스트를 원본의 점벡터 리스트에 맵딕셔너리를 이용하여 반영한다.
            Vertex3f[] outNormals = new Vertex3f[lstPositions.Count];

            for (uint i = 0; i < lstPositions.Count; i++)
            {
                lstPositions[(int)i] = pList[(int)map[i]];
                outNormals[(int)i] = normals[(int)map[i]];
            }

            int count = meshTriangles.Vertices.Count;
            List<Vertex3f> vertices = new List<Vertex3f>();
            List<Vertex2f> texcoords = new List<Vertex2f>();
            List<Vertex3f> lstNormals = new List<Vertex3f>();
            List<Vertex4i> boneIndices = new List<Vertex4i>();
            List<Vertex4f> boneWeights = new List<Vertex4f>();

            for (int i = 0; i < count; i++)
            {
                int idx = (int)meshTriangles.Vertices[i];
                int tidx = (int)meshTriangles.Texcoords[i];
                vertices.Add(lstPositions[idx]);
                texcoords.Add(lstTexCoord[tidx]);
                lstNormals.Add(outNormals[idx]);
                boneIndices.Add(lstBoneIndex[idx]);
                boneWeights.Add(lstBoneWeight[idx]);
            }

            RawModel3d _rawModel = new RawModel3d();
            _rawModel.Init(vertices: vertices.ToArray(), texCoords: texcoords.ToArray(), normals: lstNormals.ToArray(),
                boneIndex: boneIndices.ToArray(), boneWeight: boneWeights.ToArray());
            _rawModel.GpuBind();
            return _rawModel;
        }
    }
}
