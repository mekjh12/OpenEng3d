using OpenGL;
using System;
using System.Collections.Generic;
using ZetaExt;

namespace Model3d
{
    /// <summary>
    /// 기본적인 도형을 GPU에 올려 원형으로 사용한다.
    /// </summary>
    public class Loader3d
    {
        public static RawModel3d LoadAxis(float size)
        {
            float[] positions = new float[]
            {
                0, 0, 0, size, 0, 0,
                0, 0, 0, 0, size, 0,
                0, 0, 0, 0, 0, size,
            };

            float[] colors = new float[]
            {
                1, 0, 0, 1, 0, 0,
                0, 1, 0, 0, 1, 0,
                0, 0, 1, 0, 0, 1,
            };

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            uint vbo;
            vbo = StoreDataInAttributeList(0, 3, positions);
            vbo = StoreDataInAttributeList(2, 3, colors);
            Gl.BindVertexArray(0);

            RawModel3d rawModel = new RawModel3d(vao, positions);
            return rawModel;
        }

        public static float[] SkyboxVertices
        {
            get
            {
                return new float[36 * 3]
                {
                // positions          
                -1.0f, -1.0f, -1.0f, // bottom
                -1.0f,  1.0f, -1.0f,
                 1.0f,  1.0f, -1.0f,
                 1.0f,  1.0f, -1.0f,
                 1.0f, -1.0f, -1.0f,
                -1.0f, -1.0f, -1.0f,

                -1.0f,  1.0f,  1.0f, //left
                -1.0f,  1.0f, -1.0f,
                -1.0f, -1.0f, -1.0f,
                -1.0f, -1.0f, -1.0f,
                -1.0f, -1.0f,  1.0f,
                -1.0f,  1.0f,  1.0f,

                 1.0f, -1.0f,  1.0f, // right
                 1.0f, -1.0f, -1.0f,
                 1.0f,  1.0f, -1.0f,
                 1.0f,  1.0f, -1.0f,
                 1.0f,  1.0f,  1.0f,
                 1.0f, -1.0f,  1.0f,

                -1.0f,  1.0f,  1.0f, // top
                -1.0f, -1.0f,  1.0f,
                 1.0f, -1.0f,  1.0f,
                 1.0f, -1.0f,  1.0f,
                 1.0f,  1.0f,  1.0f,
                -1.0f,  1.0f,  1.0f,

                -1.0f, -1.0f,  1.0f, // front
                -1.0f, -1.0f, -1.0f,
                 1.0f, -1.0f, -1.0f,
                 1.0f, -1.0f, -1.0f,
                 1.0f, -1.0f,  1.0f,
                -1.0f, -1.0f,  1.0f,

                 1.0f,  1.0f,  1.0f, // back
                 1.0f,  1.0f, -1.0f,
                -1.0f,  1.0f, -1.0f,
                -1.0f,  1.0f, -1.0f,
                -1.0f,  1.0f,  1.0f,
                 1.0f,  1.0f,  1.0f
                };
            }
        }

        /// <summary>
        /// 반시계 방향으로 회전하여 적용한다.
        /// </summary>
        /// <param name="rotDeg">degree</param>
        /// <returns></returns>
        public static RawModel3d LoadPlaneLine()
        {
            float[] positions =
            {
                -1.0f, -1.0f, 0.0f,
                1.0f, -1.0f, 0.0f,
                1.0f, -1.0f, 0.0f,
                1.0f, 1.0f, 0.0f,
                1.0f, 1.0f, 0.0f,
                -1.0f, 1.0f, 0.0f,
                -1.0f, 1.0f, 0.0f,
                -1.0f, -1.0f, 0.0f,
            };

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            uint vbo = StoreDataInAttributeList(0, 3, positions);
            GPUBuffer.Add(vao, vbo);
            Gl.BindVertexArray(0);

            RawModel3d rawModel = new RawModel3d(vao, positions);
            return rawModel;
        }

        /// <summary>
        /// 점을 만든다.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static RawModel3d LoadPoint(float x, float y, float z)
        {
            float[] positions = new float[] { x, y, z };

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            uint vbo;
            vbo = StoreDataInAttributeList(0, 3, positions);
            GPUBuffer.Add(vao, vbo);

            Gl.BindVertexArray(0);

            RawModel3d rawModel = new RawModel3d(vao, positions);
            return rawModel;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static RawModel3d LoadLines(Vertex3f[] points)
        {
            if (points.Length <= 1) return null;
            List<float> positions = new List<float>();
            for (int i = 1; i < points.Length; i++)
            {
                positions.Add(points[i - 1].x);
                positions.Add(points[i - 1].y);
                positions.Add(points[i - 1].z);
                positions.Add(points[i].x);
                positions.Add(points[i].y);
                positions.Add(points[i].z);
            }

            float[] vertices = positions.ToArray();

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            uint vbo;
            vbo = StoreDataInAttributeList(0, 3, vertices);
            GPUBuffer.Add(vao, vbo);

            Gl.BindVertexArray(0);

            RawModel3d rawModel = new RawModel3d(vao, vertices);
            return rawModel;
        }

        public static RawModel3d LoadPolygon(Vertex3f[] positions)
        {
            List<float> list = new List<float>();
            for (int i = 1; i < positions.Length - 1; i++)
            {
                list.Add(positions[0].x);
                list.Add(positions[0].y);
                list.Add(positions[0].z);
                list.Add(positions[i].x);
                list.Add(positions[i].y);
                list.Add(positions[i].z);
                list.Add(positions[i + 1].x);
                list.Add(positions[i + 1].y);
                list.Add(positions[i + 1].z);
            }
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            uint vbo;
            vbo = StoreDataInAttributeList(0, 3, list.ToArray());
            GPUBuffer.Add(vao, vbo);

            Gl.BindVertexArray(0);
            RawModel3d rawModel = new RawModel3d(vao, list.ToArray());
            return rawModel;
        }


        /// <summary>
        /// 점들을 만든다.
        /// </summary>
        /// <param name="positions"></param>
        /// <returns></returns>
        public static RawModel3d LoadPoints(float[] positions)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            uint vbo;
            vbo = StoreDataInAttributeList(0, 3, positions);
            GPUBuffer.Add(vao, vbo);

            Gl.BindVertexArray(0);

            RawModel3d rawModel = new RawModel3d(vao, positions);
            return rawModel;
        }

        /// <summary>
        /// SkyBox를 만든다.
        /// </summary>
        /// <returns></returns>
        public static RawModel3d LoadSkyBox()
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            uint vbo;
            vbo = StoreDataInAttributeList(0, 3, Loader3d.SkyboxVertices);
            GPUBuffer.Add(vao, vbo);
            Gl.BindVertexArray(0);

            RawModel3d rawModel = new RawModel3d(vao, Loader3d.SkyboxVertices);
            return rawModel;
        }

        /// <summary>
        /// 다각뿔을 만든다. (바닥이 다각형이고 높이가z인 뿔이다.)
        /// </summary>
        /// <param name="n">밑면의 원의 꼭짓점의 갯수</param>
        /// <param name="radius">밑면의 원의 반지름</param>
        /// <param name="height">다각뿔의 높이</param>
        /// <returns></returns>
        public static RawModel3d LoadCone(int n, float radius, float height, bool isOuter = true)
        {
            List<float> positionList = new List<float>();
            List<float> normalList = new List<float>();
            List<float> textureList = new List<float>();

            Vertex3f[] vertices = new Vertex3f[n + 2];
            Vertex2f[] texCoords = new Vertex2f[n + 1];

            float unitRad = (360.0f / n).ToRadian();
            for (int i = 0; i < n; i++)
            {
                float rad = i * unitRad;
                float px = radius * MathF.Cos(rad);
                float py = radius * MathF.Sin(rad);
                vertices[i] = new Vertex3f(px, py, 0);
                float tu = 0.5f * MathF.Cos(rad) + 0.5f;
                float tv = 0.5f * MathF.Sin(rad) + 0.5f;
                texCoords[i] = new Vertex2f(tu, tv);
            }
            vertices[n] = new Vertex3f(0, 0, 0);
            vertices[n + 1] = new Vertex3f(0, 0, height);
            texCoords[n] = new Vertex2f(0.5f, 0.5f);

            int a, b, c;

            // 옆면
            for (int i = 0; i < n; i++)
            {
                if (isOuter)
                {
                    a = (i + 1) % n;
                    b = (i + 0) % n;
                    c = n + 1;
                }
                else
                {
                    a = (i + 0) % n;
                    b = (i + 1) % n;
                    c = n + 1;
                }
                positionList.Add(vertices[a].x);
                positionList.Add(vertices[a].y);
                positionList.Add(vertices[a].z);
                positionList.Add(vertices[b].x);
                positionList.Add(vertices[b].y);
                positionList.Add(vertices[b].z);
                positionList.Add(vertices[c].x);
                positionList.Add(vertices[c].y);
                positionList.Add(vertices[c].z);

                textureList.Add(texCoords[a].x);
                textureList.Add(texCoords[a].y);
                textureList.Add(texCoords[b].x);
                textureList.Add(texCoords[b].y);
                textureList.Add(texCoords[n].x);
                textureList.Add(texCoords[n].y);

                Vertex3f normalA = vertices[a] - vertices[c];
                normalA.z = -(normalA.x * normalA.x + normalA.y * normalA.y) / normalA.z;
                Vertex3f normalB = vertices[b] - vertices[c];
                normalB.z = -(normalB.x * normalB.x + normalB.y * normalB.y) / normalB.z;

                normalList.Add(normalA.x); normalList.Add(normalA.y); normalList.Add(normalA.z);
                normalList.Add(normalB.x); normalList.Add(normalB.y); normalList.Add(normalB.z);
                normalList.Add(0); normalList.Add(0); normalList.Add(1);
            }

            // 아랫면
            for (int i = 0; i < n; i++)
            {
                if (isOuter)
                {
                    a = (i + 0) % n;
                    b = (i + 1) % n;
                    c = n;
                }
                else
                {
                    a = (i + 1) % n;
                    b = (i + 0) % n;
                    c = n;
                }
                positionList.Add(vertices[a].x);
                positionList.Add(vertices[a].y);
                positionList.Add(vertices[a].z);
                positionList.Add(vertices[b].x);
                positionList.Add(vertices[b].y);
                positionList.Add(vertices[b].z);
                positionList.Add(vertices[c].x);
                positionList.Add(vertices[c].y);
                positionList.Add(vertices[c].z);
                textureList.Add(texCoords[a].x); textureList.Add(texCoords[a].y);
                textureList.Add(texCoords[b].x); textureList.Add(texCoords[b].y);
                textureList.Add(texCoords[c].x); textureList.Add(texCoords[c].y);
                normalList.Add(0); normalList.Add(0); normalList.Add(-1);
                normalList.Add(0); normalList.Add(0); normalList.Add(-1);
                normalList.Add(0); normalList.Add(0); normalList.Add(-1);
            }

            float[] positions = positionList.ToArray();
            float[] textures = textureList.ToArray();
            float[] normals = normalList.ToArray();

            TangentSpace.CalculateTangents(positions, textures, normals, out float[] tangents, out float[] bitangents);

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            uint vbo;
            vbo = StoreDataInAttributeList(0, 3, positions);
            GPUBuffer.Add(vao, vbo);
            vbo = StoreDataInAttributeList(1, 2, textures);
            GPUBuffer.Add(vao, vbo);
            vbo = StoreDataInAttributeList(2, 3, normals);
            GPUBuffer.Add(vao, vbo);
            vbo = StoreDataInAttributeList(3, 4, tangents);
            GPUBuffer.Add(vao, vbo);
            vbo = StoreDataInAttributeList(4, 4, bitangents);
            GPUBuffer.Add(vao, vbo);


            Gl.BindVertexArray(0);

            RawModel3d rawModel = new RawModel3d(vao, positions);
            return rawModel;
        }

        /// <summary>
        /// 구면을 만든다.
        /// </summary>
        /// <param name="r">구의 반지름</param>
        /// <param name="piece">위도0도부터 90도까지 n등분을 한다. 이것은 360도를 4n등분한다.</param>
        /// <param name="outer">외부 또는 내부</param>
        /// <returns></returns>
        public static RawModel3d LoadSphere(float r = 1.0f, int piece = 3, float Tu = 1.0f, float Tv = 1.0f, bool outer = true)
        {
            float unitAngle = (float)(Math.PI / 2.0f) / piece;
            float deltaTheta = 360.ToRadian() / (4.0f * piece);

            List<float> vertices = new List<float>();
            List<float> textures = new List<float>();

            // <image url="$(ProjectDir)_PictureComment\SphereCoordinate.png" scale="0.8" />
            // 반시계 방향으로 면을 생성한다.
            for (int i = -piece; i < piece; i++) // phi
            {
                for (int j = 0; j < piece * 4; j++) // theta
                {
                    float theta1 = j * unitAngle;
                    float theta2 = (j + 1) * unitAngle;
                    float phi1 = i * unitAngle;
                    float phi2 = (i + 1) * unitAngle;
                    float tu1 = Tu * (deltaTheta * (j + 0)) / 360.ToRadian();
                    float tu2 = Tu * deltaTheta * (j + 1) / 360.ToRadian();
                    float tv1 = Tv * i * unitAngle / 90.ToRadian();
                    float tv2 = Tv * (i + 1) * unitAngle / 90.ToRadian();

                    if (outer)
                    {
                        if (i == (piece - 1))
                        {
                            vertices.AddRange(SphereCoordination(r, theta2, phi1));
                            vertices.AddRange(SphereCoordination(r, theta2, phi2));
                            vertices.AddRange(SphereCoordination(r, theta1, phi1));
                            textures.AddRange(TextureCoordination(tu2, tv1));
                            textures.AddRange(TextureCoordination(tu2, tv2));
                            textures.AddRange(TextureCoordination(tu1, tv1));
                        }
                        else if (i == -piece)
                        {
                            vertices.AddRange(SphereCoordination(r, theta2, phi2));
                            vertices.AddRange(SphereCoordination(r, theta1, phi2));
                            vertices.AddRange(SphereCoordination(r, theta1, phi1));
                            textures.AddRange(TextureCoordination(tu2, tv2));
                            textures.AddRange(TextureCoordination(tu1, tv2));
                            textures.AddRange(TextureCoordination(tu1, tv1));
                        }
                        else
                        {
                            vertices.AddRange(SphereCoordination(r, theta2, phi2));
                            vertices.AddRange(SphereCoordination(r, theta1, phi2));
                            vertices.AddRange(SphereCoordination(r, theta1, phi1));
                            vertices.AddRange(SphereCoordination(r, theta2, phi1));
                            vertices.AddRange(SphereCoordination(r, theta2, phi2));
                            vertices.AddRange(SphereCoordination(r, theta1, phi1));
                            textures.AddRange(TextureCoordination(tu2, tv2));
                            textures.AddRange(TextureCoordination(tu1, tv2));
                            textures.AddRange(TextureCoordination(tu1, tv1));
                            textures.AddRange(TextureCoordination(tu2, tv1));
                            textures.AddRange(TextureCoordination(tu2, tv2));
                            textures.AddRange(TextureCoordination(tu1, tv1));
                        }
                    }
                    else
                    {
                        if (i == (piece - 1))
                        {
                            vertices.AddRange(SphereCoordination(r, theta2, phi1));
                            vertices.AddRange(SphereCoordination(r, theta1, phi1));
                            vertices.AddRange(SphereCoordination(r, theta2, phi2));
                            textures.AddRange(TextureCoordination(tu2, tv1));
                            textures.AddRange(TextureCoordination(tu1, tv1));
                            textures.AddRange(TextureCoordination(tu2, tv2));
                        }
                        else if (i == -piece)
                        {
                            vertices.AddRange(SphereCoordination(r, theta2, phi2));
                            vertices.AddRange(SphereCoordination(r, theta1, phi1));
                            vertices.AddRange(SphereCoordination(r, theta1, phi2));
                            textures.AddRange(TextureCoordination(tu2, tv2));
                            textures.AddRange(TextureCoordination(tu1, tv1));
                            textures.AddRange(TextureCoordination(tu1, tv2));
                        }
                        else
                        {
                            vertices.AddRange(SphereCoordination(r, theta2, phi2));
                            vertices.AddRange(SphereCoordination(r, theta1, phi1));
                            vertices.AddRange(SphereCoordination(r, theta1, phi2));
                            vertices.AddRange(SphereCoordination(r, theta2, phi1));
                            vertices.AddRange(SphereCoordination(r, theta1, phi1));
                            vertices.AddRange(SphereCoordination(r, theta2, phi2));
                            textures.AddRange(TextureCoordination(tu2, tv2));
                            textures.AddRange(TextureCoordination(tu1, tv1));
                            textures.AddRange(TextureCoordination(tu1, tv2));
                            textures.AddRange(TextureCoordination(tu2, tv1));
                            textures.AddRange(TextureCoordination(tu1, tv1));
                            textures.AddRange(TextureCoordination(tu2, tv2));
                        }
                    }
                }
            }

            float[] positions = vertices.ToArray();
            float[] texCoords = textures.ToArray();

            TangentSpace.CalculateTangents(positions, texCoords, positions, out float[] tangents, out float[] bitangents);

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            uint vbo;
            vbo = StoreDataInAttributeList(0, 3, positions);
            GPUBuffer.Add(vao, vbo);
            vbo = StoreDataInAttributeList(1, 2, texCoords);
            GPUBuffer.Add(vao, vbo);
            vbo = StoreDataInAttributeList(2, 3, positions);
            GPUBuffer.Add(vao, vbo);
            vbo = StoreDataInAttributeList(3, 4, tangents);
            GPUBuffer.Add(vao, vbo);
            vbo = StoreDataInAttributeList(4, 4, bitangents);
            GPUBuffer.Add(vao, vbo);

            Gl.BindVertexArray(0);

            RawModel3d rawModel = new RawModel3d(vao, positions);
            return rawModel;

            float[] TextureCoordination(float tu, float tv)
            {
                float[] verts = new float[2];
                verts[0] = tu;
                verts[1] = tv;
                return verts;
            }

            float[] SphereCoordination(float radius, float theta, float phi)
            {
                float[] verts = new float[3];
                float z = radius * (float)Math.Sin(phi);
                float R = radius * (float)Math.Cos(phi);
                float x = R * (float)Math.Cos(theta);
                float y = R * (float)Math.Sin(theta);
                verts[0] = x;
                verts[1] = y;
                verts[2] = z;
                return verts;
            }
        }


        /// <summary>
        /// 구면을 만든다.
        /// </summary>
        /// <param name="r">구의 반지름</param>
        /// <param name="piece">위도0도부터 90도까지 n등분을 한다. 이것은 360도를 4n등분한다.</param>
        /// <param name="outer">외부 또는 내부</param>
        /// <returns></returns>
        public static RawModel3d LoadHalfUppperSphere(float r = 1.0f, int horzPicesCount = 3, int piece = 3, int startpicesIndex = -3, int endpicesIndex = 3, float Tu = 1.0f, float Tv = 1.0f, bool outer = true)
        {
            float unitAngle = (float)(Math.PI / 2.0f) / piece;
            float deltaTheta = 360.ToRadian() / (4.0f * horzPicesCount);

            List<float> vertices = new List<float>();
            List<float> textures = new List<float>();

            for (int i = startpicesIndex; i < endpicesIndex; i++) // phi
            {
                for (int j = 0; j < horzPicesCount * 4; j++) // theta
                {
                    float theta1 = j * deltaTheta;
                    float theta2 = (j + 1) * deltaTheta;
                    float phi1 = i * unitAngle;
                    float phi2 = (i + 1) * unitAngle;
                    float tu1 = Tu * (deltaTheta * (j + 0)) / 360.ToRadian();
                    float tu2 = Tu * deltaTheta * (j + 1) / 360.ToRadian();
                    float tv1 = Tv * i * unitAngle / 90.ToRadian();
                    float tv2 = Tv * (i + 1) * unitAngle / 90.ToRadian();

                    if (outer)
                    {
                        if (i == (piece - 1))
                        {
                            vertices.AddRange(SphereCoordination(r, theta2, phi1));
                            vertices.AddRange(SphereCoordination(r, theta2, phi2));
                            vertices.AddRange(SphereCoordination(r, theta1, phi1));
                            textures.AddRange(TextureCoordination(tu2, tv1));
                            textures.AddRange(TextureCoordination(tu2, tv2));
                            textures.AddRange(TextureCoordination(tu1, tv1));
                        }
                        else if (i == -piece)
                        {
                            vertices.AddRange(SphereCoordination(r, theta2, phi2));
                            vertices.AddRange(SphereCoordination(r, theta1, phi2));
                            vertices.AddRange(SphereCoordination(r, theta1, phi1));
                            textures.AddRange(TextureCoordination(tu2, tv2));
                            textures.AddRange(TextureCoordination(tu1, tv2));
                            textures.AddRange(TextureCoordination(tu1, tv1));
                        }
                        else
                        {
                            vertices.AddRange(SphereCoordination(r, theta2, phi2));
                            vertices.AddRange(SphereCoordination(r, theta1, phi2));
                            vertices.AddRange(SphereCoordination(r, theta1, phi1));
                            vertices.AddRange(SphereCoordination(r, theta2, phi1));
                            vertices.AddRange(SphereCoordination(r, theta2, phi2));
                            vertices.AddRange(SphereCoordination(r, theta1, phi1));
                            textures.AddRange(TextureCoordination(tu2, tv2));
                            textures.AddRange(TextureCoordination(tu1, tv2));
                            textures.AddRange(TextureCoordination(tu1, tv1));
                            textures.AddRange(TextureCoordination(tu2, tv1));
                            textures.AddRange(TextureCoordination(tu2, tv2));
                            textures.AddRange(TextureCoordination(tu1, tv1));
                        }
                    }
                    else
                    {
                        if (i == (piece - 1))
                        {
                            vertices.AddRange(SphereCoordination(r, theta2, phi1));
                            vertices.AddRange(SphereCoordination(r, theta1, phi1));
                            vertices.AddRange(SphereCoordination(r, theta2, phi2));
                            textures.AddRange(TextureCoordination(tu2, tv1));
                            textures.AddRange(TextureCoordination(tu1, tv1));
                            textures.AddRange(TextureCoordination(tu2, tv2));
                        }
                        else if (i == -piece)
                        {
                            vertices.AddRange(SphereCoordination(r, theta2, phi2));
                            vertices.AddRange(SphereCoordination(r, theta1, phi1));
                            vertices.AddRange(SphereCoordination(r, theta1, phi2));
                            textures.AddRange(TextureCoordination(tu2, tv2));
                            textures.AddRange(TextureCoordination(tu1, tv1));
                            textures.AddRange(TextureCoordination(tu1, tv2));
                        }
                        else
                        {
                            vertices.AddRange(SphereCoordination(r, theta2, phi2));
                            vertices.AddRange(SphereCoordination(r, theta1, phi1));
                            vertices.AddRange(SphereCoordination(r, theta1, phi2));
                            vertices.AddRange(SphereCoordination(r, theta2, phi1));
                            vertices.AddRange(SphereCoordination(r, theta1, phi1));
                            vertices.AddRange(SphereCoordination(r, theta2, phi2));
                            textures.AddRange(TextureCoordination(tu2, tv2));
                            textures.AddRange(TextureCoordination(tu1, tv1));
                            textures.AddRange(TextureCoordination(tu1, tv2));
                            textures.AddRange(TextureCoordination(tu2, tv1));
                            textures.AddRange(TextureCoordination(tu1, tv1));
                            textures.AddRange(TextureCoordination(tu2, tv2));
                        }
                    }
                }
            }

            float[] positions = vertices.ToArray();
            float[] texCoords = textures.ToArray();

            TangentSpace.CalculateTangents(positions, texCoords, positions, out float[] tangents, out float[] bitangents);

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            uint vbo;
            vbo = StoreDataInAttributeList(0, 3, positions);
            GPUBuffer.Add(vao, vbo);
            vbo = StoreDataInAttributeList(1, 2, texCoords);
            GPUBuffer.Add(vao, vbo);
            vbo = StoreDataInAttributeList(2, 3, positions);
            GPUBuffer.Add(vao, vbo);
            vbo = StoreDataInAttributeList(3, 4, tangents);
            GPUBuffer.Add(vao, vbo);
            vbo = StoreDataInAttributeList(4, 4, bitangents);
            GPUBuffer.Add(vao, vbo);

            Gl.BindVertexArray(0);

            RawModel3d rawModel = new RawModel3d(vao, positions);
            return rawModel;

            float[] TextureCoordination(float tu, float tv)
            {
                float[] verts = new float[2];
                verts[0] = tu;
                verts[1] = tv;
                return verts;
            }

            float[] SphereCoordination(float radius, float theta, float phi)
            {
                float[] verts = new float[3];
                float z = radius * (float)Math.Sin(phi);
                float R = radius * (float)Math.Cos(phi);
                float x = R * (float)Math.Cos(theta);
                float y = R * (float)Math.Sin(theta);
                verts[0] = x;
                verts[1] = y;
                verts[2] = z;
                return verts;
            }
        }

        /// <summary>
        /// 각기둥을 만든다.
        /// </summary>
        /// <param name="n"></param>
        /// <param name="lowerRadius"></param>
        /// <param name="upperRadius"></param>
        /// <param name="height"></param>
        /// <param name="model"></param>
        /// <param name="Tu"></param>
        /// <param name="Tv"></param>
        /// <param name="upperTu"></param>
        /// <param name="upperTv"></param>
        /// <param name="rotDegreeUpperTexture"></param>
        /// <param name="isOuter"></param>
        /// <returns></returns>
        public static RawModel3d LoadPrism(int n, float lowerRadius, float upperRadius, float height, Matrix4x4f model,
            float Tu = 1.0f, float Tv = 1.0f, float upperTu = 1.0f, float upperTv = 1.0f, float rotDegreeUpperTexture = 0, bool isOuter = true)
        {
            List<float> positionList = new List<float>();
            List<float> normalList = new List<float>();
            List<float> textureList = new List<float>();

            Vertex3f[] vertices = new Vertex3f[n + n];
            Vertex2f[] texCoords = new Vertex2f[n + n];

            float unitRad = (360.0f / n).ToRadian();
            for (int i = 0; i < n; i++)
            {
                float rad = i * unitRad;
                float px = lowerRadius * MathF.Cos(rad);
                float py = lowerRadius * MathF.Sin(rad);
                float ux = upperRadius * MathF.Cos(rad);
                float uy = upperRadius * MathF.Sin(rad);
                vertices[i] = new Vertex3f(px, py, -height);
                vertices[n + i] = new Vertex3f(ux, uy, height);

                float tu = 0.5f * Tu * (0.5f * MathF.Cos(rad) + 0.5f);
                texCoords[i] = new Vertex2f(tu, 0);
                texCoords[n + i] = new Vertex2f(tu, Tv);
            }

            // 45도 회전을 통하여 축에 정렬
            model = Matrix4x4f.RotatedZ(45) * model;

            // 행렬 변환으로 초기 변환행렬 설정
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = (model * vertices[i].xyzw(1)).xyz();
            }

            int a, b, c, d;

            // 옆면
            for (int i = 0; i < n; i++)
            {
                if (isOuter)
                {
                    a = (i + 0) % n;
                    b = (i + 1) % n;
                    c = n + a;
                    d = n + b;
                }
                else
                {
                    a = (i + 1) % n;
                    b = (i + 0) % n;
                    c = n + b;
                    d = n + a;
                }
                positionList.Add(vertices[a].x); positionList.Add(vertices[a].y); positionList.Add(vertices[a].z);
                positionList.Add(vertices[d].x); positionList.Add(vertices[d].y); positionList.Add(vertices[d].z);
                positionList.Add(vertices[b].x); positionList.Add(vertices[b].y); positionList.Add(vertices[b].z);
                positionList.Add(vertices[d].x); positionList.Add(vertices[d].y); positionList.Add(vertices[d].z);
                positionList.Add(vertices[a].x); positionList.Add(vertices[a].y); positionList.Add(vertices[a].z);
                positionList.Add(vertices[c].x); positionList.Add(vertices[c].y); positionList.Add(vertices[c].z);

                textureList.Add(texCoords[a].x); textureList.Add(texCoords[a].y);
                textureList.Add(texCoords[d].x); textureList.Add(texCoords[d].y);
                textureList.Add(texCoords[b].x); textureList.Add(texCoords[b].y);
                textureList.Add(texCoords[d].x); textureList.Add(texCoords[d].y);
                textureList.Add(texCoords[a].x); textureList.Add(texCoords[a].y);
                textureList.Add(texCoords[c].x); textureList.Add(texCoords[c].y);

                Vertex3f normalA = vertices[a] - vertices[c];
                normalA.z = -(normalA.x * normalA.x + normalA.y * normalA.y) / normalA.z;
                Vertex3f normalB = vertices[b] - vertices[d];
                normalB.z = -(normalB.x * normalB.x + normalB.y * normalB.y) / normalB.z;

                if (!isOuter)
                {
                    normalA = -normalA;
                    normalB = -normalB;
                }

                normalList.Add(normalA.x); normalList.Add(normalA.y); normalList.Add(normalA.z);
                normalList.Add(normalB.x); normalList.Add(normalB.y); normalList.Add(normalB.z);
                normalList.Add(normalB.x); normalList.Add(normalB.y); normalList.Add(normalB.z);
                normalList.Add(normalB.x); normalList.Add(normalB.y); normalList.Add(normalB.z);
                normalList.Add(normalA.x); normalList.Add(normalA.y); normalList.Add(normalA.z);
                normalList.Add(normalA.x); normalList.Add(normalA.y); normalList.Add(normalA.z);
            }

            // 윗면 + 아랫면
            for (int k = 0; k < 2; k++)
            {
                for (int i = 1; i < n; i++)
                {
                    int mod = (k == 0) ? n : 0;
                    if (isOuter)
                    {
                        a = mod + 0;
                        b = mod + (i + 0) % n;
                        c = mod + (i + 1) % n;
                    }
                    else
                    {
                        a = mod + 0;
                        b = mod + (i + 1) % n;
                        c = mod + (i + 0) % n;
                    }

                    Vertex3f normalA = Vertex3f.UnitZ;
                    Vertex3f normalB = -Vertex3f.UnitZ;

                    if (k == 1)
                    {
                        int t = a;
                        a = b;
                        b = t;
                        normalA = -Vertex3f.UnitZ;
                        normalB = Vertex3f.UnitZ;
                    }

                    positionList.Add(vertices[a].x); positionList.Add(vertices[a].y); positionList.Add(vertices[a].z);
                    positionList.Add(vertices[c].x); positionList.Add(vertices[c].y); positionList.Add(vertices[c].z);
                    positionList.Add(vertices[b].x); positionList.Add(vertices[b].y); positionList.Add(vertices[b].z);

                    float radius = MathF.Sqrt(2) * upperRadius;
                    Vertex3f[] texCoord = new Vertex3f[3];
                    texCoord[0] = vertices[a] * radius;
                    texCoord[2] = vertices[c] * radius;
                    texCoord[1] = vertices[b] * radius;

                    Vertex3f scale = new Vertex3f(upperTu, upperTv, 0);
                    for (int v = 0; v < 3; v++)
                    {
                        Vertex3f tex = Vertex3f.Zero;
                        float theta = rotDegreeUpperTexture.ToRadian();
                        float cos = MathF.Cos(theta);
                        float sin = MathF.Sin(theta);
                        tex.x = cos * texCoord[v].x - sin * texCoord[v].y;
                        tex.y = sin * texCoord[v].x + cos * texCoord[v].y;

                        tex = scale.ComponentProduct(tex * 0.5f + Vertex3f.One * 0.5f);
                        textureList.Add(tex.x); textureList.Add(tex.y);
                        normalList.Add(normalA.x); normalList.Add(normalA.y); normalList.Add(normalA.z);
                    }
                }
            }

            float[] positions = positionList.ToArray();
            float[] textures = textureList.ToArray();
            float[] normals = normalList.ToArray();

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            uint vbo;
            vbo = StoreDataInAttributeList(0, 3, positions);
            GPUBuffer.Add(vao, vbo);
            vbo = StoreDataInAttributeList(1, 2, textures);
            GPUBuffer.Add(vao, vbo);
            vbo = StoreDataInAttributeList(2, 3, normals);
            GPUBuffer.Add(vao, vbo);

            Gl.BindVertexArray(0);

            RawModel3d rawModel = new RawModel3d(vao, positions);
            return rawModel;
        }

        /// <summary>
        /// 정육면체를 만든다.
        /// </summary>
        /// <param name="tu"></param>
        /// <param name="tv"></param>
        /// <param name="outer"></param>
        /// <returns></returns>
        public static RawModel3d LoadCube(float tu = 1.0f, float tv = 1.0f, float h = 1, bool outer = true)
        {
            // vertices 8 points.
            Vertex3f[] Points = new Vertex3f[8]
            {
                new Vertex3f(-1, -1, -h),
                new Vertex3f(1, -1, -h),
                new Vertex3f(1, 1, -h),
                new Vertex3f(-1, 1, -h),
                new Vertex3f(-1, -1, h),
                new Vertex3f(1, -1, h),
                new Vertex3f(1, 1, h),
                new Vertex3f(-1, 1, h)
            };

            Vertex3f[] Normals = new Vertex3f[6]
            {
                Vertex3f.UnitX,
                -Vertex3f.UnitX,
                Vertex3f.UnitY,
                -Vertex3f.UnitY,
                Vertex3f.UnitZ,
                -Vertex3f.UnitZ
            };

            Vertex2f[] texCoords = new Vertex2f[4]
            {
                new Vertex2f(0, 0),
                new Vertex2f(tu, 0),
                new Vertex2f(tu, tv),
                new Vertex2f(0, tv)
            };

            //         7------6                        
            //         |      |                
            //         |  +z  |   counter-clockwise
            //         |      |               
            //  7------4------5------6------7          
            //  |      |      |      |      |  
            //  |  -x  |  -y  |  +x  |  +y  |
            //  |      |      |      |      | 
            //  3------0------1------2------3           
            //         |      |                 
            //         |  -z  |                
            //         |      |                
            //         3------2                         
            List<float> positionList = new List<float>();
            List<float> normalList = new List<float>();
            List<float> textureList = new List<float>();

            attachQuad3(positionList, Points, 1, 2, 6, 5, outer); // +x
            attachQuad3(positionList, Points, 3, 0, 4, 7, outer); // -x
            attachQuad3(positionList, Points, 2, 3, 7, 6, outer); // +y
            attachQuad3(positionList, Points, 0, 1, 5, 4, outer); // -y
            attachQuad3(positionList, Points, 4, 5, 6, 7, outer); // +z
            attachQuad3(positionList, Points, 3, 2, 1, 0, outer); // -z

            attachQuad3N(normalList, Normals, 0, 1, outer); // +x
            attachQuad3N(normalList, Normals, 1, 0, outer); // -x
            attachQuad3N(normalList, Normals, 2, 3, outer); // +y
            attachQuad3N(normalList, Normals, 3, 2, outer); // -y
            attachQuad3N(normalList, Normals, 4, 5, outer); // +z
            attachQuad3N(normalList, Normals, 5, 4, outer); // -z

            attachQuad2(textureList, texCoords, 0, 1, 2, 3, outer); // +x
            attachQuad2(textureList, texCoords, 0, 1, 2, 3, outer); // -x
            attachQuad2(textureList, texCoords, 0, 1, 2, 3, outer); // +y
            attachQuad2(textureList, texCoords, 0, 1, 2, 3, outer); // -y
            attachQuad2(textureList, texCoords, 0, 1, 2, 3, outer); // +z
            attachQuad2(textureList, texCoords, 0, 1, 2, 3, outer); // -z

            // gen vertext array.
            float[] positions = positionList.ToArray();
            float[] textures = textureList.ToArray();
            float[] normals = normalList.ToArray();

            TangentSpace.CalculateTangents(positions, textures, normals, out float[] tangents, out float[] bitangents);

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            uint vbo;
            vbo = StoreDataInAttributeList(0, 3, positions);
            GPUBuffer.Add(vao, vbo);
            vbo = StoreDataInAttributeList(1, 2, textures);
            GPUBuffer.Add(vao, vbo);
            vbo = StoreDataInAttributeList(2, 3, normals);
            GPUBuffer.Add(vao, vbo);
            vbo = StoreDataInAttributeList(3, 4, tangents);
            GPUBuffer.Add(vao, vbo);
            vbo = StoreDataInAttributeList(4, 4, bitangents);
            GPUBuffer.Add(vao, vbo);

            Gl.BindVertexArray(0);

            void attachQuad3(List<float> list, Vertex3f[] points, int a, int b, int c, int d, bool isOuter)
            {
                if (isOuter)
                {
                    list.AddRange(attachVertices3f(points, a, b, c));
                    list.AddRange(attachVertices3f(points, a, c, d));
                }
                else
                {
                    list.AddRange(attachVertices3f(points, a, c, b));
                    list.AddRange(attachVertices3f(points, a, d, c));
                }
            }

            void attachQuad3N(List<float> list, Vertex3f[] points, int a, int b, bool isOuter)
            {
                if (isOuter)
                {
                    list.AddRange(attachVertices3f(points, a, a, a));
                    list.AddRange(attachVertices3f(points, a, a, a));
                }
                else
                {
                    list.AddRange(attachVertices3f(points, b, b, b));
                    list.AddRange(attachVertices3f(points, b, b, b));
                }
            }

            void attachQuad2(List<float> list, Vertex2f[] points, int a, int b, int c, int d, bool isOuter)
            {
                if (isOuter)
                {
                    list.AddRange(attachVertices2f(points, a, b, c));
                    list.AddRange(attachVertices2f(points, a, c, d));
                }
                else
                {
                    list.AddRange(attachVertices2f(points, a, c, b));
                    list.AddRange(attachVertices2f(points, a, d, c));
                }
            }

            float[] attachVertices3f(Vertex3f[] points, int a, int b, int c)
            {
                float[] vertices = new float[9];
                vertices[0] = points[a].x; vertices[1] = points[a].y; vertices[2] = points[a].z;
                vertices[3] = points[b].x; vertices[4] = points[b].y; vertices[5] = points[b].z;
                vertices[6] = points[c].x; vertices[7] = points[c].y; vertices[8] = points[c].z;
                return vertices;
            }

            float[] attachVertices2f(Vertex2f[] points, int a, int b, int c)
            {
                float[] vertices = new float[6];
                vertices[0] = points[a].x; vertices[1] = points[a].y;
                vertices[2] = points[b].x; vertices[3] = points[b].y;
                vertices[4] = points[c].x; vertices[5] = points[c].y;
                return vertices;
            }

            return new RawModel3d(vao, positions);
        }

        /// <summary>
        /// 두점을 이은 선분을 만든다.
        /// </summary>
        /// <param name="sx"></param>
        /// <param name="sy"></param>
        /// <param name="sz"></param>
        /// <param name="ex"></param>
        /// <param name="ey"></param>
        /// <param name="ez"></param>
        /// <returns></returns>
        public static RawModel3d LoadLine(float sx, float sy, float sz, float ex, float ey, float ez)
        {
            float[] positions = new float[] { sx, sy, sz, ex, ey, ez };

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            uint vbo;
            vbo = StoreDataInAttributeList(0, 3, positions);
            GPUBuffer.Add(vao, vbo);

            Gl.BindVertexArray(0);

            RawModel3d rawModel = new RawModel3d(vao, positions);
            return rawModel;
        }

        /// <summary>
        /// 2Nx2N개의 사각형으로 이루어진 평면을 가져온다. 
        /// </summary>
        /// <param name="n"></param>
        /// <param name="unitSize"></param>
        /// <returns></returns>
        public static RawModel3d LoadPlaneNxN(int n = 10, float unitSize = 1.0f)
        {
            List<float> pos = new List<float>();
            List<float> tex = new List<float>();
            List<float> nor = new List<float>();

            List<uint> indices = new List<uint>();

            int idx = 0;
            int N = 2 * n + 1;
            float unit = 1.0f / (float)(N - 1);
            for (int y = -n; y <= n; y++)
            {
                for (int x = -n; x <= n; x++)
                {
                    pos.Add(x * unitSize);
                    pos.Add(y * unitSize);
                    pos.Add(0.0f);

                    tex.Add(x / (2.0f * (float)n) + 0.5f);
                    tex.Add(y / (2.0f * (float)n) + 0.5f);

                    nor.Add(0.0f);
                    nor.Add(0.0f);
                    nor.Add(1.0f);

                    if (x < n && y < n) // 맨 오른쪽과 맨 위쪽의 한 줄씩은 제외한다.
                    {
                        indices.Add((uint)(idx));
                        indices.Add((uint)(idx + 1));
                        indices.Add((uint)(idx + N));
                        indices.Add((uint)(idx + 1 + N));
                    }

                    idx++;
                }
            }

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            StoreDataInAttributeList(0, 3, pos.ToArray());
            StoreDataInAttributeList(1, 2, tex.ToArray());
            StoreDataInAttributeList(2, 3, nor.ToArray());
            uint ibo = StoreDataInAttributeList(indices.ToArray());
            Gl.BindVertexArray(0);

            RawModel3d rawModel = new RawModel3d(vao, pos.ToArray());
            rawModel.IBO = ibo;
            rawModel.VertexCount = indices.Count;
            rawModel.IsDrawElement = true;
            return rawModel;
        }

        /// <summary>
        /// 반시계 방향으로 회전하여 적용한다.
        /// </summary>
        /// <param name="rotDeg">degree</param>
        /// <returns></returns>
        public static RawModel3d LoadPlane(float rotDeg = 0)
        {
            float Cos(float radian) => (float)Math.Cos(radian);
            float Sin(float radian) => (float)Math.Sin(radian);

            float[] positions =
            {
                -1.0f, -1.0f, 0.0f,
                1.0f, -1.0f, 0.0f,
                1.0f, 1.0f,  0.0f,
                1.0f, 1.0f,  0.0f,
                -1.0f, 1.0f,  0.0f,
                -1.0f, -1.0f,  0.0f
            };

            float[] normals =
            {
                0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 1.0f
            };

            float[] textures =
            {
                0.0f, 0.0f,
                1.0f, 0.0f,
                1.0f, 1.0f,
                1.0f, 1.0f,
                0.0f, 1.0f,
                0.0f, 0.0f
            };

            TangentSpace.CalculateTangents(positions, textures, normals, out float[] tangents, out float[] bitangents);

            // (tu,tv)를 회전하여 텍스처링한다.
            for (int i = 0; i < textures.Length; i += 2)
            {
                float tu = textures[i + 0];
                float tv = textures[i + 1];
                float deg = rotDeg.ToRadian();
                textures[i + 0] = Cos(deg) * tu - Sin(deg) * tv;
                textures[i + 1] = Sin(deg) * tu + Cos(deg) * tv;
            }

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            uint vbo;
            vbo = StoreDataInAttributeList(0, 3, positions);
            vbo = StoreDataInAttributeList(1, 2, textures);
            vbo = StoreDataInAttributeList(2, 3, normals);
            vbo = StoreDataInAttributeList(3, 4, tangents);
            vbo = StoreDataInAttributeList(4, 4, bitangents);
            Gl.BindVertexArray(0);

            RawModel3d rawModel = new RawModel3d(vao, positions);
            return rawModel;
        }


        public static RawModel3d LoadQuadPatch()
        {
            float[] positions =
            {
                -1.0f, -1.0f,
                1.0f, -1.0f,
                1.0f, 1.0f,
                -1.0f, 1.0f,
            };
            uint[] indices = { 0, 1, 3, 2};

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            uint vbo;
            vbo = StoreDataInAttributeList(0, 2, positions);

            uint ibo = StoreDataInAttributeList(indices);

            Gl.BindVertexArray(0);

            RawModel3d rawModel = new RawModel3d(vao, positions);
            rawModel.IBO = ibo;
            return rawModel;
        }

        public static RawModel3d LoadQuad()
        {
            float[] positions =
            {
                -1.0f, -1.0f,
                1.0f, -1.0f, 
                1.0f, 1.0f,
                1.0f, 1.0f,
                -1.0f, 1.0f, 
                -1.0f, -1.0f,
            };

            float[] textures =
            {
                0.0f, 0.0f,
                1.0f, 0.0f,
                1.0f, 1.0f,
                1.0f, 1.0f,
                0.0f, 1.0f,
                0.0f, 0.0f
            };

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            uint vbo;
            vbo = StoreDataInAttributeList(0, 2, positions);
            vbo = StoreDataInAttributeList(1, 2, textures);
            Gl.BindVertexArray(0);

            RawModel3d rawModel = new RawModel3d(vao, positions);
            return rawModel;
        }

        /// <summary>
        /// * data를 gpu에 올리고 vbo를 반환한다.<br/>
        /// * vao는 함수 호출 전에 바인딩하여야 한다.<br/>
        /// </summary>
        /// <param name="attributeNumber">attributeNumber 슬롯 번호</param>
        /// <param name="coordinateSize">자료의 벡터 성분의 개수 (예) vertex3f는 3이다.</param>
        /// <param name="data"></param>
        /// <param name="usage"></param>
        /// <returns>vbo를 반환</returns>
        public static unsafe uint StoreDataInAttributeList(uint attributeNumber, int coordinateSize, float[] data, BufferUsage usage = BufferUsage.StaticDraw)
        {
            // VBO 생성
            uint vboID = Gl.GenBuffer();

            // VBO의 데이터를 CPU로부터 GPU에 복사할 때 사용하는 BindBuffer를 다음과 같이 사용
            Gl.BindBuffer(BufferTarget.ArrayBuffer, vboID);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)(data.Length * sizeof(float)), data, usage);

            // 이전에 BindVertexArray한 VAO에 현재 Bind된 VBO를 attributeNumber 슬롯에 설정
            Gl.VertexAttribPointer(attributeNumber, coordinateSize, VertexAttribType.Float, false, 0, IntPtr.Zero);
            //Gl.VertexArrayVertexBuffer(glVertexArrayVertexBuffer, vboID, )

            // GPU 메모리 조작이 필요 없다면 다음과 같이 바인딩 해제
            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);

            return vboID;
        }

        public static unsafe uint StoreDataInAttributeList(uint[] data, BufferUsage usage = BufferUsage.StaticDraw)
        {
            // ibo 생성
            uint ibo = Gl.GenBuffer();

            // ibo 데이터를 CPU로부터 GPU에 복사할 때 사용하는 BindBuffer를 다음과 같이 사용
            Gl.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
            Gl.BufferData(BufferTarget.ElementArrayBuffer, (uint)(data.Length * sizeof(uint)), data, usage);

            // GPU 메모리 조작이 필요 없다면 다음과 같이 바인딩 해제
            Gl.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            return ibo;
        }
    }
}
