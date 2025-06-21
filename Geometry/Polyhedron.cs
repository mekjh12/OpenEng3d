using OpenGL;
using System;
using System.Collections.Generic;
using ZetaExt;

namespace Geometry
{
    public class Polyhedron
    {
        private const int kCapacity = 2;
        private const int kMaxPolyhedronVertexCount = 28 * kCapacity;
        public const int kMaxPolyhedronFaceCount = 16 * kCapacity;
        private const int kMaxPolyhedronEdgeCount = (kMaxPolyhedronFaceCount - 2) * 3;
        private const int kMaxPolyhedronFaceEdgeCount = kMaxPolyhedronFaceCount - 1;

        private byte _vertexCount;
        private byte _edgeCount;
        private byte _faceCount;

        private Vertex3f[] _vertices;
        private Edge[] _edges;
        private Face[] _faces;
        private Plane[] _planes;

        // variables for render of glsl.
        private uint _vao;
        private uint _verticesCount = 0;
        private Vertex3f _position;
        private Matrix4x4f _bindMatrix;
        private OBB _obb = null;

        #region Properties

        public Vertex3f Center
        {
            get
            {
                Vertex3f.MinMax(_vertices, out Vertex3f min, out Vertex3f max);
                return (min + max) * 0.5f;
            }
        }

        public int FaceCount => _faceCount;

        public int EdgeCount => _edgeCount;

        public OBB OBB => _obb;

        public bool IsOBB => (_obb == null);

        public Vertex3f[] Vertices => _vertices;

        public Plane[] Planes => _planes;

        public Edge[] Edges => _edges;
        
        public Face[] Faces => _faces;

        public Matrix4x4f BindMatrix => _bindMatrix;

        public uint RenderVertexCount => _verticesCount;

        public uint VAO
        {
            get => _vao;
            set => _vao = value;
        }

        public int VertexCount
        {
            get => _vertexCount;
            set => _vertexCount = (byte)value;
        }

        public Vertex3f Position => _position;

        #endregion

        public Polyhedron()
        {
            _vertices = new Vertex3f[kMaxPolyhedronVertexCount];
            _edges = new Edge[kMaxPolyhedronEdgeCount];
            _faces = new Face[kMaxPolyhedronFaceCount];
            _planes = new Plane[kMaxPolyhedronFaceCount];
            _bindMatrix = Matrix4x4f.Identity;
        }

        public void SetVertex(Vertex3f[] vertices)
        {
            _vertexCount = (Byte)vertices.Length;
            _vertices = new Vertex3f[_vertexCount];
            for (int i = 0; i < _vertexCount; i++)
            {
                _vertices[i] = vertices[i];
            }
        }

        public void SetEdge(Edge[] edges)
        {
            _edgeCount = (Byte)edges.Length;
            _edges = new Edge[_edgeCount];
            for (int i = 0; i < _edgeCount; i++)
            {
                _edges[i] = edges[i];
            }
        }

        public void SetFace(Face[] faces)
        {
            _faceCount = (Byte)faces.Length;
            _faces = new Face[_faceCount];
            for (int i = 0; i < _faceCount; i++)
            {
                _faces[i] = faces[i];
            }
        }

        public void SetPlane(Plane[] planes)
        {
            _planes = new Plane[planes.Length];
            for (int i = 0; i < planes.Length; i++)
            {
                _planes[i] = planes[i];
            }
        }

        /// <summary>
        /// ======================================================================<br/>
        /// * Closed Orientable Manifold를 구성한다.<br/>
        /// * 버텍스와 버텍스의 인덱스로 구성되어진 폴리곤으로 닫힌 유향 다양체를 구성한다.<br/>
        /// ======================================================================<br/>
        /// </summary>
        /// <param name="vertexArray"></param>
        /// <param name="polygonArray"></param>
        public void BuildClosedPolyhedron(Vertex3f[] vertexArray, Polygon[] polygonArray)
        {
            _vertices = vertexArray;
            _vertexCount = (byte)vertexArray.Length;

            Polyhedron.BuildEdgeArray(vertexArray.Length, polygonArray, out Edge[] edgeArray);
            BuildFaceArray(polygonArray, edgeArray, out Face[] faceArray, out Plane[] planeArray);

            _edges = edgeArray;
            _edgeCount = (byte)(edgeArray.Length);

            _faces = faceArray;
            _faceCount = (byte)(faceArray.Length);

            _planes = planeArray;
        }

        /// <summary>
        /// 회전하고 x,y,z로 이동시킨다.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="rotdegX"></param>
        /// <param name="rotdegY"></param>
        /// <param name="rotdegZ"></param>
        public void Transform(float x = 0, float y = 0, float z = 0, float rotdegX = 0, float rotdegY = 0, float rotdegZ = 0)
        {
            // 행렬곱 순서는 X->Y->Z->P
            Transform(
                Matrix4x4f.Translated(x, y, z) *
                Matrix4x4f.RotatedZ(rotdegZ) *
                Matrix4x4f.RotatedY(rotdegY) *
                Matrix4x4f.RotatedX(rotdegX)
                );
        }

        /// <summary>
        /// 모델행렬을 이용하여 vertices, planes을 변환한다.
        /// </summary>
        /// <param name="modelMatrix">model matrix</param>
        public void Transform(Matrix4x4f modelMatrix)
        {
            // vertices is transformed by model matrix.
            for (int i = 0; i < _vertexCount; i++)
            {
                Vertex3f point = _vertices[i];
                Vertex4f res = modelMatrix.Multiply(point);
                _vertices[i] = ((Vertex3f)res);
            }

            // planes is transformed by model matrix.
            _position = ((Vertex3f)modelMatrix.Column3);

            // used by site url
            //stackoverflow.com/questions/7685495/transforming-a-3d-plane-using-a-4x4-matrix
            for (int i = 0; i < _planes.Length; i++)
            {
                Vertex4f p = _planes[i].Normal.xyzw();
                p.w = _planes[i].W;
                Vertex4f q = modelMatrix.Inverse.Transposed * p;
                _planes[i] = new Plane(q.x, q.y, q.z, q.w);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="polygonArray"></param>
        /// <param name="edgeArray"></param>
        /// <param name="faceArray"></param>
        /// <param name="planeArray"></param>
        /// <returns></returns>
        public int BuildFaceArray(Polygon[] polygonArray, Edge[] edgeArray, out Face[] faceArray, out Plane[] planeArray)
        {
            Polygon[] polygon = polygonArray;

            // Identify all faces.
            List<Face> faceList = new List<Face>();
            for (int k = 0; k < polygon.Length; k++)
            {
                uint i1 = polygon[k].Index(polygon[k].Count - 1);
                List<ushort> edges = new List<ushort>();
                for (uint v = 0; v < polygon[k].Count; v++)
                {
                    uint i2 = polygon[k].Index(v);
                    for (int i = 0; i < edgeArray.Length; i++)
                    {
                        ushort vertexIndex0 = edgeArray[i].Vertex0;
                        ushort vertexIndex1 = edgeArray[i].Vertex1;
                        if ((vertexIndex0 == i1 && vertexIndex1 == i2) ||
                            (vertexIndex0 == i2 && vertexIndex1 == i1))
                        {
                            edges.Add((ushort)i);
                            break;
                        }
                    }
                    i1 = i2;
                }
                Face face = new Face(edges.ToArray());
                faceList.Add(face);
            }

            // Identify all normals.
            List<Plane> planeList = new List<Plane>();
            for (int k = 0; k < polygon.Length; k++)
            {
                uint i0 = polygon[k].Index(0);
                uint i1 = polygon[k].Index(1);
                uint i2 = polygon[k].Index(2);
                Vertex3f v0 = _vertices[i0];
                Vertex3f v1 = _vertices[i1];
                Vertex3f v2 = _vertices[i2];
                Vertex3f normal = (v2 - v1).Cross(v1 - v0).Normalized;
                planeList.Add(new Plane(-normal, v0));
            }

            faceArray = faceList.ToArray();
            planeArray = planeList.ToArray();

            return faceList.Count;
        }

        /// <summary>
        /// ======================================================================<br/>
        /// * 버텍스와 폴리곤(삼각형)들로 이루어진 polyhedron에서 유향엣지를 구성하여 반환한다.<br/>
        /// * [조건] 반시계방향으로 감겨 있는 폴리곤이어야 한다.<br/>
        /// * [조건] 유향엣지의 구성은 시작점(v0)와 종점(v1)으로 구성하고 <br/>
        /// * [조건] 유향엣지의 f0은 왼쪽방향의 평면 인덱스, f1은 오른쪽 방향의 평면 인덱스로 지정한다. <br/>
        /// ======================================================================<br/>
        /// </summary>
        /// <param name="vertexCount"></param>
        /// <param name="polygonArray"></param>
        /// <param name="edgeArray"></param>
        /// <returns></returns>
        public static int BuildEdgeArray(int vertexCount, Polygon[] polygonArray, out Edge[] edgeArray)
        {
            // Initialize all edge lists to empty.
            ushort[] firstEdge = new ushort[vertexCount];
            ushort[] nextEdge = new ushort[vertexCount + polygonArray.Length * 2]; // v + 2f
            for (int k = 0; k < vertexCount; k++) firstEdge[k] = 0xFFFF;

            int edgeCount = 0;
            List<Edge> edgeList = new List<Edge>();
            Polygon[] polygon = polygonArray;

            // Identify all edges that have increasing vertex indices in CCW direction.
            // 반시계 방향으로 감겨 있는 폴리곤에 대하여 모든 폴리곤을 순회하면서,
            // 폴리곤 마다 반시계 방향으로 순회하여 모든 유향엣지를 구성한다. 
            // 순회하는 과정에서 각 꼭지점마다 연결된 엣지 순회리스트를 구성한다.
            // 구성하는 과정은 스택에 쌓는 모습과 일치하나 해시맵으로 구성된다.
            for (int k = 0; k < polygon.Length; k++)
            {
                uint i1 = polygon[k].Index(polygon[k].Count - 1);
                for (uint v = 0; v < polygon[k].Count; v++)
                {
                    uint i2 = polygon[k].Index(v);
                    if (i1 < i2)
                    {
                        Edge edge = new Edge();
                        edgeList.Add(edge);
                        edge.SetVertexIndex(0, (byte)i1);
                        edge.SetVertexIndex(1, (byte)i2);
                        edge.SetFaceIndex(0, (byte)k);
                        edge.SetFaceIndex(1, (byte)k);

                        // Add the edge to the front of the list for the first vertex.
                        nextEdge[edgeCount] = firstEdge[i1];
                        firstEdge[i1] = (ushort)edgeCount;
                        edgeCount++;
                    }
                    i1 = i2;
                }
            }

            // Match all edges to the triangles for which they are wound clockwise.
            // 각각의 폴리곤을 순회하면서 시계 방향의 엣지를 순회하여
            // 엣지의 끝점을 따라 구성된 해시맵을 따라 엣지를 순회하면서
            // (1) 엣지의 양쪽을 지정하지 않고
            // (2) 엣지의 시작점과 구성된 해시맵의 엣지의 끝점이 일치하는 경우에
            // 엣지의 오른쪽에 face index를 지정하여 엣지를 완성한다.
            for (int k = 0; k < polygon.Length; k++)
            {
                uint i1 = polygon[k].Index(polygon[k].Count - 1);
                for (uint v = 0; v < polygon[k].Count; v++)
                {
                    uint i2 = polygon[k].Index(v);
                    if (i1 > i2)
                    {
                        for (ushort e = firstEdge[i2]; e != 0xFFFF; e = nextEdge[e])
                        {
                            Edge edge = edgeList[e];
                            if ((edge.GetVertexIndex(1) == i1) &&
                                edge.GetFaceIndex(0) == edge.GetFaceIndex(1))
                            {
                                edge.SetFaceIndex(1, (byte)k);
                                break;
                            }
                        }
                    }
                    i1 = i2;
                }
            }

            edgeArray = edgeList.ToArray();
            return edgeCount;
        }

        /// <summary>
        /// 모든 면의 앞, 뒤를 서로 뒤집는다.
        /// </summary>
        public void FlipFrontAndBackFace()
        {
            for (int i = 0; i < _edgeCount; i++)
            {
                Edge edge = _edges[i];
                int a = edge.GetVertexIndex(0);
                int b = edge.GetVertexIndex(1);
                edge.SetVertexIndex(0, (byte)b);
                edge.SetVertexIndex(1, (byte)a);
            }

            for (int i = 0; i < _faceCount; i++)
            {
                _planes[i].Flip();
            }
        }

        /// <summary>
        /// ======================================================================<br/>
        /// * 볼록다각형(같은 평면에 있지 않아도 됨)을 하나의 방향이 있는 평면으로 잘라 잘린 볼록다각형을 반환한다.<br/>
        /// * 잘린 볼록다각형의 꼭짓점의 개수 >= 입력된 볼록다각형의 꼭짓점의 개수<br/>
        /// ======================================================================<br/>
        /// </summary>
        /// <param name="vertices">반시계방향으로 감긴 볼록다각형의 꼭짓점</param>
        /// <param name="plane">방향이 있는 자를 평면</param>
        /// <param name="location">입력된 볼록다각형의 꼭짓점마다 평면과의 부호가 있는 거리</param>
        /// <param name="result">잘린 볼록다각형의 꼭짓점</param>
        /// <returns>잘린 볼록다각형의 꼭짓점의 개수</returns>
        public static int ClipPolygon(Vertex3f[] vertices, Plane plane, out float[] location, out Vertex3f[] result)
        {
            float kPolygonEpsilon = 0.001f;
            int postiveCount = 0, negativeCount = 0;
            int vertexCount = vertices.Length;

            // Calculate the signed distance to plane for all vertices.
            location = new float[vertexCount];

            for (int a = 0; a < vertexCount; a++)
            {
                float d = plane * vertices[a];
                location[a] = d;
                if (d > kPolygonEpsilon) postiveCount++;
                else if (d < -kPolygonEpsilon) negativeCount++;
            }

            if (negativeCount == 0)
            {
                // No vertices on negative side of plane. Copy original polygon to result.
                result = new Vertex3f[vertexCount];
                for (int a = 0; a < vertexCount; a++) result[a] = vertices[a];
                return (vertexCount);
            }
            else if (postiveCount == 0)
            {
                // No vertices on positive side of plane.
                result = null;
                return 0;
            }

            // Loop through all edges, starting with edge from last vertex to first vertex.
            List<Vertex3f> list = new List<Vertex3f>();
            Vertex3f p1 = vertices[vertexCount - 1];
            float d1 = location[vertexCount - 1];
            for (int index = 0; index < vertexCount; index++)
            {
                Vertex3f p2 = vertices[index];
                float d2 = location[index];

                if (d2 < -kPolygonEpsilon)
                {
                    // Current vertex is on negative side of plane.
                    if (d1 > kPolygonEpsilon)
                    {
                        // Preceding vertex is on positive side of plane.
                        float t = d1 / (d1 - d2);
                        list.Add(p1 * (1.0f - t) + p2 * t);
                    }
                }
                else
                {
                    // Current vertex is on positive side of plane or in plane.
                    if ((d2 > kPolygonEpsilon) && (d1 < -kPolygonEpsilon))
                    {
                        // Current vertex on positive side, and preceding vertex on negative side.
                        float t = d2 / (d2 - d1);
                        list.Add(p2 * (1.0f - t) + p1 * t);
                    }

                    list.Add(p2);
                }

                p1 = p2;
                d1 = d2;
            }

            result = list.ToArray();
            int resultCount = list.Count;

            return resultCount;
        }

        /// <summary>
        /// 유향평면으로 폴리곤을 잘라서 양의 방향의 폴리곤만 반환한다.
        /// </summary>
        /// <param name="polyhedron"></param>
        /// <param name="plane"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool ClipPolyhedron(in Polyhedron polyhedron, Plane plane, out Polyhedron result)
        {
            // if polyhedron is null, return a original polyhedron.
            if (polyhedron == null)
            {
                result = polyhedron;
                return false;
            }

            result = new Polyhedron();

            float[] vertexLocation = new float[kMaxPolyhedronVertexCount];
            byte[] vertexCode = new byte[kMaxPolyhedronVertexCount];
            byte[] edgeCode = new byte[kMaxPolyhedronEdgeCount];
            byte[] vertexRemap = new byte[kMaxPolyhedronVertexCount];
            byte[] edgeRemap = new byte[kMaxPolyhedronEdgeCount];
            byte[] faceRemap = new byte[kMaxPolyhedronFaceCount];
            byte[] planeEdgeTable = new byte[kMaxPolyhedronFaceEdgeCount];

            const float kPolygonEpsilon = 0.001f;
            int minCode = 0, maxCode = 0;

            #region (1) Classify vertices, edges, Determine which faces will be in result.

            // Classify vertices.
            int vertexCount = polyhedron.VertexCount;
            for (int a = 0; a < vertexCount; a++)
            {
                vertexRemap[a] = 0xFF;
                float d = plane * polyhedron._vertices[a];
                vertexLocation[a] = d;

                byte code = (byte)(Convert.ToInt16(d > -kPolygonEpsilon) + 2 * Convert.ToInt16(d > kPolygonEpsilon));
                minCode = Math.Min(minCode, code);
                maxCode = Math.Max(maxCode, code);
                vertexCode[a] = code;
            }

            if (minCode != 0)
            {
                result = polyhedron;
                return true;                // No vertices on negative side of clip plane.
            }

            if (maxCode <= 1) return false; // No vertices on positive side of clip plane.

            // Classify edges.
            int edgeCount = polyhedron._edgeCount;
            for (int a = 0; a < edgeCount; a++)
            {
                edgeRemap[a] = 0xFF;
                Edge edge = polyhedron._edges[a];
                edgeCode[a] = (byte)(vertexCode[edge.GetVertexIndex(0)] + vertexCode[edge.GetVertexIndex(1)]);
            }

            // Determine which faces will be in result.
            int resultFaceCount = 0;
            int faceCount = polyhedron._faceCount;
            for (int a = 0; a < faceCount; a++)
            {
                faceRemap[a] = 0xFF;
                Face face = polyhedron._faces[a];
                int faceEdgeCount = face.EdgeCount;
                for (int b = 0; b < faceEdgeCount; b++)
                {
                    if (edgeCode[face.GetEdge(b)] >= 3)
                    {
                        // Face has a vertex on the positive side of the plane.
                        result._planes[resultFaceCount] = polyhedron._planes[a];
                        faceRemap[a] = (byte)resultFaceCount;
                        resultFaceCount++;
                        break;
                    }
                }
            }

            #endregion

            #region (2) Reparied Edges having a classification code of 3 are clipped to produce new vertices. (0 or 1 are eleiminated.)

            // Edges having a classification code of 3 are clipped to produce new vertices.
            // Make a repair edges that is clipped to a plane. 
            // (0 or 1 are eleiminated.)
            byte resultVertexCount = 0;
            byte resultEdgeCount = 0;

            for (int a = 0; a < edgeCount; a++)
            {
                if (edgeCode[a] >= 2)
                {
                    // The edge is not completely clipped away.
                    Edge edge = polyhedron._edges[a];
                    Edge resultEdge = new Edge();
                    result._edges[resultEdgeCount] = resultEdge;
                    edgeRemap[a] = (byte)resultEdgeCount;
                    resultEdgeCount++;

                    resultEdge.SetFaceIndex(0, faceRemap[edge.GetFaceIndex(0)]);
                    resultEdge.SetFaceIndex(1, faceRemap[edge.GetFaceIndex(1)]);

                    // Loop over both vertices of edge.
                    for (int i = 0; i < 2; i++)
                    {
                        ushort vertexIndex = edge.GetVertexIndex(i);
                        if (vertexCode[vertexIndex] != 0)
                        {
                            // This vertex on positive side of plane or in plane.
                            byte remappedVertexIndex = vertexRemap[vertexIndex];
                            // 하나의 버텍스는 계속하여 여러 번 순회하면서 조회하므로ㄴㄴㄴㄴ
                            // 리매핑하지 않았을 경우에만 해당 코드를 실행한다.
                            if (remappedVertexIndex == 0xFF)
                            {
                                remappedVertexIndex = (byte)resultVertexCount;
                                resultVertexCount++;
                                vertexRemap[vertexIndex] = remappedVertexIndex;
                                result._vertices[remappedVertexIndex] = polyhedron._vertices[vertexIndex];
                            }
                            resultEdge.SetVertexIndex(i, remappedVertexIndex);
                        }
                        else
                        {
                            // This vertex on negative side, and other vertex on positive side.
                            ushort otherVertexIndex = edge.GetVertexIndex(1 - i);
                            Vertex3f p1 = polyhedron._vertices[vertexIndex];
                            Vertex3f p2 = polyhedron._vertices[otherVertexIndex];
                            float d1 = vertexLocation[vertexIndex];
                            float d2 = vertexLocation[otherVertexIndex];
                            float t = d2 / (d2 - d1);
                            result._vertices[resultVertexCount] = p2 * (1.0f - t) + p1 * t;
                            resultEdge.SetVertexIndex(i, resultVertexCount);
                            resultVertexCount++;
                        }
                    }
                }
                else
                {
                    // edge of edgecode 0  or 1 throw away.
                }
            }

            #endregion

            #region (3) Assign edges to every faces

            // ***** face make and insert face ******
            // loops over the faces in the original polyhedron and
            // for each face, loops over the edges forming the boundary of that face.
            // When original edges having an odd clssification code are encountered,
            // it means that a new edge needs to be created in the clipping plane to close the original face.
            // The new edge is always wound ccw with respect to the original face.
            int planeEdgeCount = 0;
            for (int a = 0; a < faceCount; a++)
            {
                byte remappedFaceIndex = faceRemap[a];
                if (remappedFaceIndex != 0xFF)
                {
                    byte newEdgeIndex = 0xFF;
                    Edge newEdge = null;

                    // The face is not completely clipped away.
                    Face face = polyhedron._faces[a];
                    uint faceEdgeCount = face.EdgeCount;

                    Face resultFace = new Face();
                    result._faces[remappedFaceIndex] = resultFace;

                    uint resultFaceEdgeCount = 0;

                    // Loop over face's original edges.
                    for (int b = 0; b < faceEdgeCount; b++)
                    {
                        byte edgeIndex = (byte)face.GetEdge(b);
                        int code = edgeCode[edgeIndex];
                        if (code == 1 || code == 3)
                        {
                            // One endpoint on negative side of plane, and other either
                            // on positive side (code == 3) or in plane (code == 1).
                            if (newEdge == null)
                            {
                                // At this point, we know we need a new edge.
                                newEdge = new Edge(0xFF, 0xFF, remappedFaceIndex, 0xFF);
                                newEdgeIndex = resultEdgeCount;
                                result._edges[resultEdgeCount] = newEdge;
                                planeEdgeTable[planeEdgeCount] = (resultEdgeCount);
                                resultEdgeCount++;
                                planeEdgeCount++;
                            }

                            Edge edge = polyhedron._edges[edgeIndex];
                            bool ccw = (edge.GetFaceIndex(0) == a);

                            bool insertEdge = ccw ^ (vertexCode[edge.GetVertexIndex(0)] == 0);

                            if (code == 3)
                            {
                                // Original edge has been clipped.
                                byte remappedEdgeIndex = edgeRemap[edgeIndex];
                                resultFace.SetEdge((int)resultFaceEdgeCount, remappedEdgeIndex);
                                resultFaceEdgeCount++;

                                Edge resultEdge = result._edges[remappedEdgeIndex];

                                if (insertEdge)
                                {
                                    newEdge.SetVertexIndex(0, (byte)resultEdge.GetVertexIndex((!ccw).ToByte()));
                                    resultFace.SetEdge((int)resultFaceEdgeCount, newEdgeIndex);
                                    resultFaceEdgeCount++;
                                }
                                else
                                {
                                    newEdge.SetVertexIndex(1, (byte)resultEdge.GetVertexIndex((ccw).ToByte()));
                                }
                            }
                            else
                            {
                                // Original edge has been delete, code == 1
                                if (insertEdge)
                                {
                                    newEdge.SetVertexIndex(0, vertexRemap[edge.GetVertexIndex((ccw).ToByte())]);
                                    resultFace.SetEdge((int)resultFaceEdgeCount, newEdgeIndex);
                                    resultFaceEdgeCount++;
                                }
                                else
                                {
                                    newEdge.SetVertexIndex(1, vertexRemap[edge.GetVertexIndex((!ccw).ToByte())]);
                                }
                            }

                        }
                        else if (code != 0) // code 2, 4, 6
                        {
                            // Neither endpoint is on the negative side of the clipping plane.
                            byte remappedEdgeIndex = edgeRemap[edgeIndex];
                            resultFace.SetEdge((int)resultFaceEdgeCount, remappedEdgeIndex);
                            resultFaceEdgeCount++;
                            if (code == 2)
                            {
                                planeEdgeTable[planeEdgeCount] = remappedEdgeIndex;
                                planeEdgeCount++;
                            }
                        }
                    }

                    if ((newEdge != null) &&
                        (Math.Max(newEdge.GetVertexIndex(0), newEdge.GetVertexIndex(1)) == 0xFF))
                    {
                        // The input polyhedron was invalid.
                        result = polyhedron;
                        return true;
                    }

                    resultFace.EdgeCount = (byte)resultFaceEdgeCount;
                }
            }

            #endregion

            #region (4) Construct a new face in the clipped plane.

            if (planeEdgeCount > 2)
            {
                // 자르는 평면의 법선벡터의 반대 방향으로 normal을 만들어야 한다.
                result._planes[resultFaceCount] = new Plane(-plane.Normal, plane.W);
                Face resultFace = new Face();
                result._faces[resultFaceCount] = resultFace;
                resultFace.EdgeCount = (byte)planeEdgeCount;

                for (int a = 0; a < planeEdgeCount; a++)
                {
                    byte edgeIndex = planeEdgeTable[a];
                    resultFace.SetEdge(a, edgeIndex);
                    Edge resultEdge = result._edges[edgeIndex];
                    byte k = ((byte)resultEdge.GetFaceIndex(0) == 0xFF).ToByte();
                    resultEdge.SetFaceIndex(k, (byte)resultFaceCount);
                }

                resultFaceCount++;
            }

            #endregion

            result._vertexCount = (byte)resultVertexCount;
            result._edgeCount = (byte)resultEdgeCount;
            result._faceCount = (byte)resultFaceCount;

            return true;
        }


        /// <summary>
        /// ======================================================================<br/>
        /// * 한 광원(무한광, 점광원)으로부터의 다면체의 실루엣 엣지를 찾아 반환한다.<br/>
        /// * 엣지는 gpu에 업로드된 버텍스 정보들로 실루엣을 찾는다.<br/>
        /// ======================================================================<br/>
        /// </summary>
        /// <param name="lightPos">점광원이면 점광원의 위치(x,y,z,1)이고 <br/>
        /// 무한광이면 빛이 무한광으로부터 나가는 방향의 반대방향(dx,dy,dz)라면, (-dx,-dy,-dz,0)으로 설정한다. </param>
        /// <param name="silhouette"></param>
        /// <returns></returns>
        public int FindSilhouetteEdges(Vertex4f lightPos, out Edge[] silhouette)
        {
            //            Vertex4f lightPos = (light is DirectionLight) ?
            //(-(light as DirectionLight).Direction).xyzw(0.0f) : light.Position.xyzw(1.0f);

            Dictionary<ushort, bool> classificationList = new Dictionary<ushort, bool>();
            List<Edge> result = new List<Edge>();

            // 모든 폴리곤에 대하여 광원이 앞면인지 유무를 저장한다.
            for (ushort i = 0; i < _faceCount; i++)
            {
                // 점광원 또는 스팟광원인 경우에 lw=1, 무한광인 경우에는 lw=0이다.
                float distance = _planes[i] * lightPos;
                //Console.WriteLine($"{i} {distance} {_planes[i]}");
                classificationList.Add(i, (distance >= 0.0f));
            }
            //Console.WriteLine("--------------");

            // 각 엣지마다 왼쪽과 오른쪽 폴리곤이 빛과의 유향거리의 부호가 서로 다른 경우를 실루엣 엣지로 판별한다.
            for (int i = 0; i < _edgeCount; i++)
            {
                Edge edge = _edges[i];
                ushort f0 = edge.Face0;
                ushort f1 = edge.Face1;
                // xor
                if (classificationList[f0] ^ classificationList[f1])
                    result.Add(edge);
            }

            silhouette = result.ToArray();
            return silhouette.Length;
        }

        /// <summary>
        /// 폴리곤을 앞, 뒤면이 동일한 양면 다면체로 만든다.
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public static Polyhedron BiPlane(Vertex3f[] vertices, Polygon polygon)
        {
            Polyhedron _polyhedron = new Polyhedron();
            Polygon[] polygons = new Polygon[2];
            polygons[0] = polygon;

            uint n = polygon.Count;
            uint[] reverseIndices = new uint[n];
            for (uint i = 0; i < n; i++)
            {
                reverseIndices[i] = polygon.Index(n - 1 - i);
            }
            polygons[1] = new Polygon(reverseIndices);
            _polyhedron.BuildClosedPolyhedron(vertices, polygons);
            return _polyhedron;
        }

        public void SetVertices(Matrix4x4f view, float maxX, float maxY, float maxZ, float minX, float minY, float minZ)
        {
            Vertex3f[] vertices = new Vertex3f[8];
            Polygon[] quads = new Polygon[6];
            vertices[0] = new Vertex3f(minX, minY, minZ);
            vertices[1] = new Vertex3f(maxX, minY, minZ);
            vertices[2] = new Vertex3f(maxX, minY, maxZ);
            vertices[3] = new Vertex3f(minX, minY, maxZ);
            vertices[4] = new Vertex3f(minX, maxY, minZ);
            vertices[5] = new Vertex3f(maxX, maxY, minZ);
            vertices[6] = new Vertex3f(maxX, maxY, maxZ);
            vertices[7] = new Vertex3f(minX, maxY, maxZ);

            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = (view * vertices[i].xyzw(1.0f)).xyz();

            quads[0] = new Polygon(3, 2, 1, 0);
            quads[1] = new Polygon(5, 4, 0, 1);
            quads[2] = new Polygon(6, 5, 1, 2);
            quads[3] = new Polygon(7, 6, 2, 3);
            quads[4] = new Polygon(4, 7, 3, 0);
            quads[5] = new Polygon(4, 5, 6, 7);

            BuildClosedPolyhedron(vertices, quads);
        }

        /// <summary>
        /// 오브젝트 공간의 큐브를 만들어 view 행렬로 회전, 이동시켜 월드공간으로 이동시킨다.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        /// <param name="maxZ"></param>
        /// <param name="minX"></param>
        /// <param name="minY"></param>
        /// <param name="minZ"></param>
        /// <returns></returns>
        public static Polyhedron Cube(Matrix4x4f model, float maxX, float maxY, float maxZ, float minX, float minY, float minZ)
        {
            Polyhedron _polyhedron = new Polyhedron();

            Vertex3f[] vertices = new Vertex3f[8];
            Polygon[] quads = new Polygon[6];
            vertices[0] = new Vertex3f(minX, minY, minZ);
            vertices[1] = new Vertex3f(maxX, minY, minZ);
            vertices[2] = new Vertex3f(maxX, minY, maxZ);
            vertices[3] = new Vertex3f(minX, minY, maxZ);
            vertices[4] = new Vertex3f(minX, maxY, minZ);
            vertices[5] = new Vertex3f(maxX, maxY, minZ);
            vertices[6] = new Vertex3f(maxX, maxY, maxZ);
            vertices[7] = new Vertex3f(minX, maxY, maxZ);

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = (model * vertices[i].xyzw(1.0f)).xyz();
            }

            // 반시계방향으로 구성
            quads[0] = new Polygon(0, 1, 2, 3);
            quads[1] = new Polygon(0, 4, 5, 1);
            quads[2] = new Polygon(1, 5, 6, 2);
            quads[3] = new Polygon(2, 6, 7, 3);
            quads[4] = new Polygon(3, 7, 4, 0);
            quads[5] = new Polygon(6, 5, 4, 7);

            _polyhedron.BuildClosedPolyhedron(vertices, quads);

            return _polyhedron;
        }

        public static Polyhedron Sphere(uint n, float radius)
        {
            Polyhedron _polyhedron = new Polyhedron();

            uint m = (uint)Math.Ceiling((float)n / 2.0f);

            Vertex3f[] vertices = new Vertex3f[n * (m - 1) + 2];
            Polygon[] quads = new Polygon[n * m];

            float thetaUnit = (360.0f / n).ToRadian();
            float phiUnit = thetaUnit;

            for (uint i = 0; i < m - 1; i++)
            {
                for (uint j = 0; j < n; j++)
                {
                    float theta = j * thetaUnit;
                    float phi = (i + 1) * phiUnit;
                    float px = radius * (float)Math.Sin(phi) * (float)Math.Cos(theta);
                    float py = radius * (float)Math.Cos(phi);
                    float pz = radius * (float)Math.Sin(phi) * (float)Math.Sin(theta);

                    vertices[n * i + j] = new Vertex3f(px, py, pz);

                    if (i < m - 2)
                    {
                        uint a = (j + 0) % n;
                        uint b = (i + 0);
                        uint c = (j + 1) % n;
                        uint d = (i + 1);
                        quads[n * i + j] = new Polygon(b * n + a, b * n + c, d * n + c, d * n + a);
                    }
                }
            }

            uint first = n * (m - 1) + 0;
            uint last = n * (m - 1) + 1;
            vertices[first] = new Vertex3f(0, radius, 0);
            vertices[last] = new Vertex3f(0, -radius, 0);

            for (uint j = 0; j < n; j++)
            {
                uint a = j;
                uint b = (j + 1) % n;
                quads[n * (m - 2) + j] = new Triangle(first, b, a);
                quads[n * (m - 1) + j] = new Triangle(last, n * (m - 2) + a, n * (m - 2) + b);
            }

            _polyhedron.BuildClosedPolyhedron(vertices, quads);

            return _polyhedron;
        }

        public static Polyhedron Prism(uint n, float lowerRadius, float upperRadius, float height)
        {
            Polyhedron _polyhedron = new Polyhedron();

            Vertex3f[] vertices = new Vertex3f[2 * n];
            Polygon[] quads = new Polygon[n + 2];

            float unitRad = (360.0f / n).ToRadian();

            for (uint i = 0; i < n; i++)
            {
                float rad = i * unitRad;
                float px1 = lowerRadius * (float)Math.Cos(rad);
                float pz1 = lowerRadius * (float)Math.Sin(rad);
                float px2 = upperRadius * (float)Math.Cos(rad);
                float pz2 = upperRadius * (float)Math.Sin(rad);
                vertices[i] = new Vertex3f(px1, 0, pz1);
                vertices[n + i] = new Vertex3f(px2, height, pz2);
                uint a = (i + 0) % n;
                uint b = (i + 1) % n;
                quads[i] = new Polygon(a, b, b + n, a + n);
            }

            uint[] lowerIndices = new uint[n];
            uint[] upperIndices = new uint[n];
            for (int i = 0; i < n; i++)
            {
                lowerIndices[i] = (ushort)(n - 1 - i);
                upperIndices[i] = (ushort)(n + i);
            }
            quads[n] = new Polygon(lowerIndices);
            quads[n + 1] = new Polygon(upperIndices);

            _polyhedron.BuildClosedPolyhedron(vertices, quads);

            return _polyhedron;
        }

        public static Polyhedron Pyramid(int n, float radius, float height)
        {
            Polyhedron _polyhedron = new Polyhedron();

            Vertex3f[] vertices = new Vertex3f[n + 1];
            Polygon[] triangles = new Polygon[n + 1];

            float unitRad = (360.0f / n).ToRadian();

            for (int i = 0; i < n; i++)
            {
                float rad = i * unitRad;
                float px = radius * (float)Math.Cos(rad);
                float pz = radius * (float)Math.Sin(rad);
                vertices[i] = new Vertex3f(px, 0, pz);
                triangles[i] = new Triangle(n, (i + 0) % n, (i + 1) % n);
            }
            vertices[n] = new Vertex3f(0, height, 0);

            uint[] lowerIndices = new uint[n];
            for (int i = 0; i < n; i++) lowerIndices[i] = (ushort)(n - 1 - i);
            triangles[n] = new Polygon(lowerIndices);

            _polyhedron.BuildClosedPolyhedron(vertices, triangles);

            return _polyhedron;
        }

    }
}
