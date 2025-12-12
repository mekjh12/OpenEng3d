using Assimp;
using Common.Abstractions;
using OpenGL;
using System;
using System.Collections.Generic;
using System.IO;

namespace Model3d
{
    public class ObjLoaderEx
    {
        static string _directory = "";

        /// <summary>
        /// OBJ 파일을 하나의 통합 메시로 로드
        /// </summary>
        public static UnifiedModel LoadObjUnified(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new Exception($"{filename}이 존재하지 않습니다.");
            }

            // Assimp 초기화
            Assimp.AssimpContext importer = new Assimp.AssimpContext();
            importer.SetConfig(new Assimp.Configs.NormalSmoothingAngleConfig(66.0f));

            Assimp.Scene scene = importer.ImportFile(filename,
                Assimp.PostProcessSteps.Triangulate |
                Assimp.PostProcessSteps.FlipUVs);

            if (scene == null || scene.RootNode == null)
            {
                Console.WriteLine("ERROR::ASSIMP:: Scene is null");
                return null;
            }

            _directory = Path.GetDirectoryName(filename);

            // 1단계: 모든 메시 데이터 수집
            List<MeshData> meshDataList = CollectMeshData(scene);

            // 2단계: 텍스처 수집 및 인덱싱
            Dictionary<string, int> textureIndexMap = new Dictionary<string, int>();
            List<string> textureFiles = new List<string>();

            foreach (var meshData in meshDataList)
            {
                if (!string.IsNullOrEmpty(meshData.TextureFile))
                {
                    if (!textureIndexMap.ContainsKey(meshData.TextureFile))
                    {
                        textureIndexMap[meshData.TextureFile] = textureFiles.Count;
                        textureFiles.Add(meshData.TextureFile);
                    }
                    meshData.MaterialID = textureIndexMap[meshData.TextureFile];
                }
                else
                {
                    meshData.MaterialID = -1; // 텍스처 없음
                }
            }

            // 3단계: 하나의 메시로 통합
            UnifiedModel model = MergeIntoSingleMesh(meshDataList, textureFiles);

            Console.WriteLine($"통합 완료: {model.VertexCount}개 정점, " +
                             $"{model.IndexCount}개 인덱스, {model.Textures.Count}개 텍스처");

            return model;
        }

        /// <summary>
        /// Scene에서 모든 메시 데이터 수집
        /// </summary>
        private static List<MeshData> CollectMeshData(Assimp.Scene scene)
        {
            List<MeshData> meshDataList = new List<MeshData>();
            Stack<Assimp.Node> nodes = new Stack<Assimp.Node>();
            nodes.Push(scene.RootNode);

            while (nodes.Count > 0)
            {
                Assimp.Node node = nodes.Pop();

                for (int i = 0; i < node.MeshCount; i++)
                {
                    Assimp.Mesh mesh = scene.Meshes[node.MeshIndices[i]];
                    Assimp.Material material = scene.Materials[mesh.MaterialIndex];

                    MeshData data = ExtractMeshData(mesh, material);
                    meshDataList.Add(data);

                    Console.WriteLine($"메시 수집: {mesh.Name}, 정점={mesh.VertexCount}, 재질={material.Name}");
                }

                for (int i = 0; i < node.ChildCount; i++)
                    nodes.Push(node.Children[i]);
            }

            return meshDataList;
        }

        /// <summary>
        /// 개별 메시의 데이터 추출
        /// </summary>
        private class MeshData
        {
            public float[] Positions;
            public float[] Normals;
            public float[] TexCoords;
            public uint[] Indices;
            public string TextureFile;
            public int MaterialID;  // Texture2DArray의 레이어 인덱스
        }

        private static MeshData ExtractMeshData(Assimp.Mesh mesh, Assimp.Material material)
        {
            MeshData data = new MeshData();

            // 정점 데이터 추출
            if (mesh.HasVertices)
            {
                Vector3D[] vectors = mesh.Vertices.ToArray();
                data.Positions = new float[vectors.Length * 3];
                for (int i = 0; i < vectors.Length; i++)
                {
                    data.Positions[3 * i + 0] = vectors[i].X;
                    data.Positions[3 * i + 1] = vectors[i].Y;
                    data.Positions[3 * i + 2] = vectors[i].Z;
                }
            }

            // 노말 데이터 추출
            if (mesh.HasNormals)
            {
                Vector3D[] normals = mesh.Normals.ToArray();
                data.Normals = new float[normals.Length * 3];
                for (int i = 0; i < normals.Length; i++)
                {
                    data.Normals[3 * i + 0] = normals[i].X;
                    data.Normals[3 * i + 1] = normals[i].Y;
                    data.Normals[3 * i + 2] = normals[i].Z;
                }
            }

            // UV 좌표 추출
            if (mesh.HasTextureCoords(0))
            {
                Vector3D[] texCoords = mesh.TextureCoordinateChannels[0].ToArray();
                data.TexCoords = new float[texCoords.Length * 2];
                for (int i = 0; i < texCoords.Length; i++)
                {
                    data.TexCoords[2 * i + 0] = texCoords[i].X;
                    data.TexCoords[2 * i + 1] = texCoords[i].Y;
                }
            }

            // 인덱스 추출
            List<uint> indices = new List<uint>();
            for (int i = 0; i < mesh.FaceCount; i++)
            {
                foreach (uint idx in mesh.Faces[i].Indices)
                    indices.Add(idx);
            }
            data.Indices = indices.ToArray();

            // 텍스처 파일 경로
            string textureFile = _directory + "\\" + material.TextureDiffuse.FilePath;
            if (File.Exists(textureFile))
            {
                data.TextureFile = textureFile;
            }

            return data;
        }

        /// <summary>
        /// 여러 메시를 하나로 병합
        /// </summary>
        private static UnifiedModel MergeIntoSingleMesh(List<MeshData> meshDataList, List<string> textureFiles)
        {
            // 전체 크기 계산
            int totalVertexCount = 0;
            int totalIndexCount = 0;

            foreach (var meshData in meshDataList)
            {
                totalVertexCount += meshData.Positions.Length / 3;
                totalIndexCount += meshData.Indices.Length;
            }

            // 통합 버퍼 생성
            float[] allPositions = new float[totalVertexCount * 3];
            float[] allNormals = new float[totalVertexCount * 3];
            float[] allTexCoords = new float[totalVertexCount * 2];
            float[] allMaterialIDs = new float[totalVertexCount];  // Material ID 버퍼
            uint[] allIndices = new uint[totalIndexCount];

            int vertexOffset = 0;
            int indexOffset = 0;

            // 각 메시 데이터를 통합 버퍼에 복사
            foreach (var meshData in meshDataList)
            {
                int vertexCount = meshData.Positions.Length / 3;
                int indexCount = meshData.Indices.Length;

                // 정점 위치 복사
                Array.Copy(meshData.Positions, 0, allPositions, vertexOffset * 3, meshData.Positions.Length);

                // 노말 복사
                if (meshData.Normals != null)
                    Array.Copy(meshData.Normals, 0, allNormals, vertexOffset * 3, meshData.Normals.Length);

                // UV 좌표 복사
                if (meshData.TexCoords != null)
                    Array.Copy(meshData.TexCoords, 0, allTexCoords, vertexOffset * 2, meshData.TexCoords.Length);

                // Material ID 설정 (모든 정점에 동일한 Material ID)
                for (int i = 0; i < vertexCount; i++)
                {
                    allMaterialIDs[vertexOffset + i] = meshData.MaterialID;
                }

                // 인덱스 복사 (버텍스 오프셋 적용)
                for (int i = 0; i < indexCount; i++)
                {
                    allIndices[indexOffset + i] = (uint)(meshData.Indices[i] + vertexOffset);
                }

                vertexOffset += vertexCount;
                indexOffset += indexCount;
            }

            // VAO 생성
            uint vaoID = CreateVAO(allPositions, allNormals, allTexCoords, allMaterialIDs, allIndices);

            // 텍스처 로드
            List<Texture> textures = new List<Texture>();
            foreach (string texFile in textureFiles)
            {
                textures.Add(new Texture(texFile, Texture.TextureMapType.Diffuse));
            }

            UnifiedModel model = new UnifiedModel
            {
                VaoID = vaoID,
                VertexCount = totalVertexCount,
                IndexCount = totalIndexCount,
                Textures = textures
            };

            return model;
        }

        /// <summary>
        /// VAO 생성 및 데이터 바인딩
        /// </summary>
        private static uint CreateVAO(float[] positions, float[] normals,
            float[] texCoords, float[] materialIDs, uint[] indices)
        {
            uint vaoID = Gl.GenVertexArray();
            Gl.BindVertexArray(vaoID);

            // 정점 속성 바인딩
            StoreAttribute(0, 3, positions);      // location 0: position
            StoreAttribute(1, 2, texCoords);      // location 1: texCoord
            StoreAttribute(2, 3, normals);        // location 2: normal
            StoreAttribute(3, 1, materialIDs);    // location 3: materialID

            // 인덱스 버퍼
            uint ebo = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            Gl.BufferData(BufferTarget.ElementArrayBuffer,
                (uint)(indices.Length * sizeof(uint)), indices, BufferUsage.StaticDraw);

            Gl.BindVertexArray(0);

            Console.WriteLine($"VAO 생성: ID={vaoID}");

            return vaoID;
        }

        /// <summary>
        /// VBO 생성 및 정점 속성 설정
        /// </summary>
        private static void StoreAttribute(uint location, int size, float[] data)
        {
            uint vbo = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            Gl.BufferData(BufferTarget.ArrayBuffer,
                (uint)(data.Length * sizeof(float)), data, BufferUsage.StaticDraw);

            Gl.VertexAttribPointer(location, size, VertexAttribType.Float, false, 0, IntPtr.Zero);
            Gl.EnableVertexAttribArray(location);

            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }
    }
}
