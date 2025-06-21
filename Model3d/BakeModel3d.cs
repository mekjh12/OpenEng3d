using Geometry;
using OpenGL;
using System;
using System.Collections.Generic;

namespace Model3d
{
    public class BakeModel3d
    {
        /// <summary>
        /// 다면체를 이용하여 렌더링 모델(Entity)을 만든다. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="polyhedron"></param>
        /// <param name="ambient"></param>
        /// <returns></returns>
        public static Entity MakeEntity(string name, Polyhedron polyhedron, Vertex4f ambient)
        {
            RawModel3d rawModel = MakeRawModel3d(polyhedron);
            Entity entity = new Entity(name, "polyhedron_geometry", rawModel)
            {
                Material = new Material() { Ambient = ambient }
            };
            return entity;
        }

        /// <summary>
        /// Face별 삼각형으로 쪼게어 glsl를 위하여 GPU에 데이터를 올린다.
        /// </summary>
        /// <returns></returns>
        private static RawModel3d MakeRawModel3d(Polyhedron polyhedron)
        {
            // release a vao in gpu.
            if (polyhedron.VAO > 0) GPUBuffer.CleanAt(polyhedron.VAO);

            List<Vertex3f> vertexlist = new List<Vertex3f>();
            List<Vertex3f> normalList = new List<Vertex3f>();

            for (int f = 0; f < polyhedron.FaceCount; f++)
            {
                Face face = polyhedron.Faces[f];
                int faceEdgeCount = face.EdgeCount;
                if (face.EdgeCount < 3) continue;

                Edge edge0 = polyhedron.Edges[face.GetEdge(0)];
                Vertex3f point0 = (edge0.GetFaceIndex(0) == f) ?
                    polyhedron.Vertices[edge0.GetVertexIndex(0)] : polyhedron.Vertices[edge0.GetVertexIndex(1)];

                for (int j = 1; j < faceEdgeCount; j++)
                {
                    Edge edge = polyhedron.Edges[face.GetEdge(j)];
                    Vertex3f point1 = Vertex3f.Zero;
                    Vertex3f point2 = Vertex3f.Zero;

                    if (edge.GetFaceIndex(0) == f)
                    {
                        point1 = polyhedron.Vertices[edge.GetVertexIndex(0)];
                        point2 = polyhedron.Vertices[edge.GetVertexIndex(1)];
                    }
                    else
                    {
                        point1 = polyhedron.Vertices[edge.GetVertexIndex(1)];
                        point2 = polyhedron.Vertices[edge.GetVertexIndex(0)];
                    }

                    vertexlist.Add(point0);
                    vertexlist.Add(point1);
                    vertexlist.Add(point2);

                    normalList.Add(polyhedron.Planes[f].Normal);
                    normalList.Add(polyhedron.Planes[f].Normal);
                    normalList.Add(polyhedron.Planes[f].Normal);
                }

            }

            polyhedron.VertexCount = vertexlist.Count;
            float[] positions = new float[polyhedron.VertexCount * 3];
            float[] normals = new float[polyhedron.VertexCount * 3];

            for (int i = 0; i < polyhedron.VertexCount; i++)
            {
                positions[3 * i + 0] = vertexlist[i].x;
                positions[3 * i + 1] = vertexlist[i].y;
                positions[3 * i + 2] = vertexlist[i].z;

                normals[3 * i + 0] = normalList[i].x;
                normals[3 * i + 1] = normalList[i].y;
                normals[3 * i + 2] = normalList[i].z;
            }

            polyhedron.VAO = Gl.GenVertexArray();
            Gl.BindVertexArray(polyhedron.VAO);
            uint vbo;
            vbo = StoreDataInAttributeList(0, 3, positions);
            GPUBuffer.Add(polyhedron.VAO, vbo);
            vbo = StoreDataInAttributeList(2, 3, normals);
            GPUBuffer.Add(polyhedron.VAO, vbo);

            Gl.BindVertexArray(0);

            //RawModel3d result = new RawModel3d(polyhedron.VAO, vertexlist.ToArray());
            //result.Polyhedron = this;
            return null;
            //return result;
        }

        /// <summary>
        /// 다면체에서 선택한 모서리로 렌더링 모델(Entity)을 만든다. 
        /// </summary>
        /// <param name="polyhedron"></param>
        /// <param name="edges"></param>
        /// <returns></returns>
        public static RawModel3d MakeEdgeRawModel(Polyhedron polyhedron, Edge[] edges)
        {
            List<float> vertexlist = new List<float>();
            foreach (Edge edge in edges)
            {
                Vertex4f v0 = polyhedron.Vertices[edge.Vertex0];
                Vertex4f v1 = polyhedron.Vertices[edge.Vertex1];

                vertexlist.Add(v0.x);
                vertexlist.Add(v0.y);
                vertexlist.Add(v0.z);
                vertexlist.Add(v1.x);
                vertexlist.Add(v1.y);
                vertexlist.Add(v1.z);
            }

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            uint vbo;
            vbo = StoreDataInAttributeList(0, 3, vertexlist.ToArray());
            GPUBuffer.Add(vao, vbo);
            Gl.BindVertexArray(0);

            //RawModel3d result = new RawModel3d(vao, vertexlist.ToArray());
            return null;
            //return result;
        }

        /// <summary>
        /// (-width, -depth, -height)--(width, depth, height)의 cube를 만든다.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="depth"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Polyhedron Cube(float width, float depth, float height)
        {
            Polyhedron _polyhedron = new Polyhedron();

            Vertex3f[] vertices = new Vertex3f[8];
            Polygon[] quads = new Polygon[6];
            vertices[0] = new Vertex3f(-width, -height, -depth);
            vertices[1] = new Vertex3f(width, -height, -depth);
            vertices[2] = new Vertex3f(width, -height, depth);
            vertices[3] = new Vertex3f(-width, -height, depth);
            vertices[4] = new Vertex3f(-width, height, -depth);
            vertices[5] = new Vertex3f(width, height, -depth);
            vertices[6] = new Vertex3f(width, height, depth);
            vertices[7] = new Vertex3f(-width, height, depth);
            quads[0] = new Polygon(3, 2, 1, 0);
            quads[1] = new Polygon(5, 4, 0, 1);
            quads[2] = new Polygon(6, 5, 1, 2);
            quads[3] = new Polygon(7, 6, 2, 3);
            quads[4] = new Polygon(4, 7, 3, 0);
            quads[5] = new Polygon(4, 5, 6, 7);

            _polyhedron.BuildClosedPolyhedron(vertices, quads);

            return _polyhedron;
        }

        public static Entity MakeEntityOBB(OBB obb, string name)
        {
            Vertex3f center = obb.Center;
            Vertex3f size = obb.Size;
            Vertex3f[] axis = obb.Axis;

            Polyhedron cube = BakeModel3d.Cube(1, 1, 1);
            Matrix4x4f scale = Matrix4x4f.Identity;
            scale[0, 0] = size.x;
            scale[1, 1] = size.y;
            scale[2, 2] = size.z;

            Matrix4x4f rot = Matrix4x4f.Identity;
            rot[0, 0] = axis[0].x;
            rot[0, 1] = axis[0].y;
            rot[0, 2] = axis[0].z;
            rot[1, 0] = axis[1].x;
            rot[1, 1] = axis[1].y;
            rot[1, 2] = axis[1].z;
            rot[2, 0] = axis[2].x;
            rot[2, 1] = axis[2].y;
            rot[2, 2] = axis[2].z;

            Matrix4x4f trans = Matrix4x4f.Identity;
            trans[3, 0] = center.x;
            trans[3, 1] = center.y;
            trans[3, 2] = center.z;
            cube.Transform(scale * rot * trans);

            RawModel3d cubeRawModel = MakeRawModel3d(cube);
            Entity entity = new Entity(name, "cube", cubeRawModel)
            {
                Material = new Material() { Ambient = new Vertex4f(1, 0, 0, 0.3f) }
            };

            return entity;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        public static OBB MakeOBB(Vertex3f[] vertices)
        {
            OBB obb = null;
            OBBUtility.CalculateOBB(vertices, out Vertex3f center, out Vertex3f size, out Vertex3f[] axis);
            bool chked = OBBUtility.CheckCalculateOBB(vertices, center, size, axis);
            if (chked)
            {
                obb = new OBB(center, size, axis);
            }
            return obb;
        }

        private static unsafe uint StoreDataInAttributeList(uint attributeNumber, int coordinateSize, float[] data, BufferUsage usage = BufferUsage.StaticDraw)
        {
            uint vboID = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, vboID);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)(data.Length * sizeof(float)), data, usage);
            Gl.VertexAttribPointer(attributeNumber, coordinateSize, VertexAttribType.Float, false, 0, IntPtr.Zero);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);
            return vboID;
        }
    }
}
