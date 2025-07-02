using Assimp;
using Model3d;
using OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Animate
{
    public static class ObjLoader
    {
        static string _directory = "";

        public static List<TexturedModel> LoadObj(string filename)
        {
            Assimp.Scene scene;
            Assimp.AssimpContext importer = new Assimp.AssimpContext();
            importer.SetConfig(new Assimp.Configs.NormalSmoothingAngleConfig(66.0f));
            scene = importer.ImportFile(filename, Assimp.PostProcessSteps.Triangulate |
                                                    Assimp.PostProcessSteps.FlipUVs);

            if (scene == null || scene.RootNode == null)
            {
                Console.WriteLine("ERROR::ASSIMP::");
                return null;
            }

            _directory = Path.GetDirectoryName(filename);

            // 모델을 로드한다.
            Stack<Assimp.Node> nodes = new Stack<Assimp.Node>();
            List<TexturedModel> models = new List<TexturedModel>();

            nodes.Push(scene.RootNode);
            while (nodes.Count > 0)
            {
                Assimp.Node node = nodes.Pop();
                Console.WriteLine(node.Name);

                // process all the node's meshes (if any)
                for (int i = 0; i < node.MeshCount; i++)
                {
                    Assimp.Mesh mesh = scene.Meshes[node.MeshIndices[i]];
                    models.Add(LoadMeshObj(mesh, scene));
                }

                // 자식노드 재귀호출
                for (int i = 0; i < node.ChildCount; i++)
                    nodes.Push(node.Children[i]);
            }

            return models;
        }

        private static TexturedModel LoadMeshObj(Assimp.Mesh mesh, Assimp.Scene scene)
        {
            float[] positions = null;
            float[] normals = null;
            float[] texCoords = null;

            Console.WriteLine($"\t\tMesh 정보 {mesh.Name} mesh count={mesh.VertexCount}");

            // 모델의 텍스쳐를 읽어온다.
            Dictionary<TextureType, List<Texture>> textures = LoadMaterials(scene, mesh);

            // Vertices를 읽는다.
            if (mesh.HasVertices)
                LoadObjVertices(mesh, ref positions);

            // Normal을 읽는다.
            if (mesh.HasNormals)
                LoadNormals(mesh, ref normals);

            // TextureCoords를 읽는다.
            if (mesh.HasTextureCoords(0))
                LoadTexCoords(mesh, ref texCoords);

            // Faces Index를 읽는다.
            List<uint> indices = LoadFaceIndices(mesh);

            // VAO, VBO 모델을 만든다.
            Loader3d loader = new Loader3d();
            RawModel3d rawModel = LoadRawModel3d(positions, texCoords, normals, indices.ToArray());

            return new TexturedModel(rawModel, textures[TextureType.Diffuse][0]);
        }

        private static RawModel3d LoadRawModel3d(float[] positions, float[] textureCoords, float[] normals, uint[] indices)
        {
            // VAO의 경우
            uint vaoID = Gl.GenVertexArray();
            Gl.BindVertexArray(vaoID);

            storeDataInAttributeList(0, 3, positions);
            if (textureCoords != null) storeDataInAttributeList(1, 2, textureCoords);
            if (normals != null) storeDataInAttributeList(2, 3, normals);

            uint ebo = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            Gl.BufferData(BufferTarget.ElementArrayBuffer, (uint)(indices.Length * sizeof(int)), indices, BufferUsage.StaticDraw);

            Gl.BindVertexArray(0);

            return null;// new RawModel3d(vaoID, positions, null, null, null);
        }

        private static unsafe uint storeDataInAttributeList(uint attributeNumber, int coordinateSize, float[] data, BufferUsage usage = BufferUsage.StaticDraw)
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

        private static void LoadObjVertices(Assimp.Mesh mesh, ref float[] positions)
        {
            Vector3D[] vectors = mesh.Vertices.ToArray();
            positions = new float[vectors.Length * 3];
            for (int i = 0; i < vectors.Length; i++)
            {
                positions[3 * i + 0] = vectors[i].X;
                positions[3 * i + 1] = vectors[i].Y;
                positions[3 * i + 2] = vectors[i].Z;
            }
        }

        private static void LoadNormals(Assimp.Mesh mesh, ref float[] normals)
        {
            Vector3D[] normalList = mesh.Normals.ToArray();
            normals = new float[normalList.Length * 3];
            for (int i = 0; i < normalList.Length; i++)
            {
                normals[3 * i + 0] = normalList[i].X;
                normals[3 * i + 1] = normalList[i].Y;
                normals[3 * i + 2] = normalList[i].Z;
            }
        }

        private static void LoadTexCoords(Assimp.Mesh mesh, ref float[] texCoords)
        {
            Vector3D[] texCoordList = mesh.TextureCoordinateChannels[0].ToArray();
            texCoords = new float[texCoordList.Length * 2];
            for (int i = 0; i < texCoordList.Length; i++)
            {
                texCoords[2 * i + 0] = texCoordList[i].X;
                texCoords[2 * i + 1] = texCoordList[i].Y;
            }
        }

        private static List<uint> LoadFaceIndices(Assimp.Mesh mesh)
        {
            List<uint> indices = new List<uint>();
            for (int i = 0; i < mesh.FaceCount; i++)
            {
                foreach (uint item in mesh.Faces[i].Indices)
                {
                    indices.Add(item);
                }
            }
            return indices;
        }

        private static Dictionary<TextureType, List<Texture>> LoadMaterials(Assimp.Scene scene, Assimp.Mesh mesh)
        {
            // 텍스처를 읽어온다.
            Dictionary<TextureType, List<Texture>> textures = new Dictionary<TextureType, List<Texture>>();
            Assimp.Material material = scene.Materials[mesh.MaterialIndex];

            List<Texture> diffuseMaps = LoadMaterialTextures(material, TextureType.Diffuse);
            textures[TextureType.Diffuse] = diffuseMaps;

            List<Texture> specularMaps = LoadMaterialTextures(material, TextureType.Specular);
            textures[TextureType.Specular] = specularMaps;

            List<Texture> normalMaps = LoadMaterialTextures(material, TextureType.Normals);
            textures[TextureType.Normals] = normalMaps;

            return textures;
        }

        private static List<Texture> LoadMaterialTextures(Assimp.Material mat, Assimp.TextureType typeName)
        {
            List<Texture> textures = new List<Texture>();

            for (int i = 0; i < mat.GetMaterialTextureCount(typeName); i++)
            {
                TextureSlot str;
                mat.GetMaterialTexture(typeName, i, out str);
                string filename = _directory + "\\" + str.FilePath;

                if (TextureStorage.TexturesLoaded.ContainsKey(filename))
                {
                    // 로드한 적이 있음
                    textures.Add(TextureStorage.TexturesLoaded[filename]);
                }
                else
                {
                    // 로드한 적이 없음
                    if (File.Exists(filename))
                    {
                        Bitmap bitmap = (Bitmap)Bitmap.FromFile(filename);
                        Texture texture = new Texture(bitmap);
                        textures.Add(texture);
                        TextureStorage.TexturesLoaded.Add(filename, texture);
                    }
                }
            }

            return textures;
        }
    }
}