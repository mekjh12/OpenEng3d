using Assimp;
using Model3d;
using OpenGL;
using System.Collections.Generic;
using System.IO;

using System;

/// <summary>
/// Assimp를 사용하여 3D OBJ 파일을 로드하고 OpenGL 렌더링용으로 변환하는 클래스
/// </summary>
public class ObjLoader
{
    // 현재 처리중인 모델의 디렉토리 경로
    static string _directory = "";

    /// <summary>
    /// OBJ 파일을 로드하여 텍스처가 적용된 3D 모델 리스트 반환
    /// </summary>
    public static List<TexturedModel> LoadObj(string filename)
    {
        // 파일 존재 확인
        if (!File.Exists(filename))
        {
            new Exception($"{filename}이 존재하지 않습니다.");
        }

        // Assimp 초기화 및 설정
        Assimp.Scene scene;
        Assimp.AssimpContext importer = new Assimp.AssimpContext();
        // 노말 스무딩 각도 설정
        importer.SetConfig(new Assimp.Configs.NormalSmoothingAngleConfig(66.0f));
        // 삼각형화 및 UV 좌표 뒤집기 적용
        scene = importer.ImportFile(filename, Assimp.PostProcessSteps.Triangulate |
                                                Assimp.PostProcessSteps.FlipUVs);

        if (scene == null || scene.RootNode == null)
        {
            Console.WriteLine("ERROR::ASSIMP::");
            return null;
        }

        _directory = Path.GetDirectoryName(filename);

        // 노드 순회를 위한 초기화
        Stack<Assimp.Node> nodes = new Stack<Assimp.Node>();
        List<TexturedModel> models = new List<TexturedModel>();

        // 깊이 우선 탐색으로 모든 노드 처리
        nodes.Push(scene.RootNode);
        while (nodes.Count > 0)
        {
            Assimp.Node node = nodes.Pop();
            Console.WriteLine(node.Name);

            // 현재 노드의 모든 메시 처리
            for (int i = 0; i < node.MeshCount; i++)
            {
                Assimp.Mesh mesh = scene.Meshes[node.MeshIndices[i]];
                Assimp.Material material = scene.Materials[mesh.MaterialIndex];
                models.AddRange(LoadMeshObj(mesh, scene, material, _directory));
                Console.WriteLine($"- mesh name=[{mesh.Name}] has a material name of [{material.Name}]");
            }

            // 자식 노드들을 스택에 추가
            for (int i = 0; i < node.ChildCount; i++)
                nodes.Push(node.Children[i]);
        }

        return models;
    }

    /// <summary>
    /// 단일 메시를 처리하여 텍스처가 적용된 모델 생성
    /// </summary>
    private static List<TexturedModel> LoadMeshObj(Assimp.Mesh mesh, Assimp.Scene scene,
        Assimp.Material material, string directory)
    {
        // 메시 데이터 버퍼
        float[] positions = null;  // 정점 위치
        float[] normals = null;    // 노말 벡터
        float[] texCoords = null;  // 텍스처 좌표

        Console.WriteLine($"\t\tMesh 정보 {mesh.Name} mesh count={mesh.VertexCount}");

        List<TexturedModel> models = new List<TexturedModel>();

        // 각 데이터 타입별 로드
        Dictionary<TextureType, List<Texture>> textures = LoadMaterials(scene, mesh);

        if (mesh.HasVertices) LoadObjVertices(mesh, ref positions);
        if (mesh.HasNormals) LoadNormals(mesh, ref normals);
        if (mesh.HasTextureCoords(0)) LoadTexCoords(mesh, ref texCoords);

        List<uint> indices = LoadFaceIndices(mesh);

        // OpenGL 버퍼 생성 및 데이터 전송
        RawModel3d rawModel = LoadRawModel3d(positions, texCoords, normals, indices.ToArray());

        // 텍스처 적용
        string textureFileName = directory + "\\" + material.TextureDiffuse.FilePath;
        if (File.Exists(textureFileName))
        {
            models.Add(new TexturedModel(rawModel,
                new Texture(textureFileName, Texture.TextureMapType.Diffuse)));
        }
        else
        {
            models.Add(new TexturedModel(rawModel, null));
        }

        return models;
    }

    /// <summary>
    /// OpenGL 버퍼 객체(VAO, VBO, EBO) 생성 및 데이터 바인딩
    /// </summary>
    private static RawModel3d LoadRawModel3d(float[] positions, float[] textureCoords,
        float[] normals, uint[] indices)
    {
        // VAO 생성 및 바인딩
        uint vaoID = Gl.GenVertexArray();
        Gl.BindVertexArray(vaoID);

        // 정점 속성 설정
        storeDataInAttributeList(0, 3, positions);  // 위치
        if (textureCoords != null)
            storeDataInAttributeList(1, 2, textureCoords);  // UV 좌표
        if (normals != null)
            storeDataInAttributeList(2, 3, normals);  // 노말

        // 인덱스 버퍼(EBO) 설정
        uint ebo = Gl.GenBuffer();
        Gl.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
        Gl.BufferData(BufferTarget.ElementArrayBuffer,
            (uint)(indices.Length * sizeof(int)), indices, BufferUsage.StaticDraw);

        Gl.BindVertexArray(0);

        return new RawModel3d(vaoID, positions);
    }

    /// <summary>
    /// VBO 생성 및 정점 속성 데이터 저장
    /// </summary>
    private static unsafe uint storeDataInAttributeList(uint attributeNumber,
        int coordinateSize, float[] data, BufferUsage usage = BufferUsage.StaticDraw)
    {
        // VBO 생성
        uint vboID = Gl.GenBuffer();

        // GPU 메모리에 데이터 전송
        Gl.BindBuffer(BufferTarget.ArrayBuffer, vboID);
        Gl.BufferData(BufferTarget.ArrayBuffer,
            (uint)(data.Length * sizeof(float)), data, usage);

        // VAO에 정점 속성 설정
        Gl.VertexAttribPointer(attributeNumber, coordinateSize,
            VertexAttribType.Float, false, 0, IntPtr.Zero);

        // 바인딩 해제
        Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);

        return vboID;
    }

    /// <summary>
    /// 정점 위치 데이터를 배열로 변환
    /// </summary>
    private static void LoadObjVertices(Assimp.Mesh mesh, ref float[] positions)
    {
        Vector3D[] vectors = mesh.Vertices.ToArray();
        positions = new float[vectors.Length * 3];
        // XYZ 좌표값 복사
        for (int i = 0; i < vectors.Length; i++)
        {
            positions[3 * i + 0] = vectors[i].X;
            positions[3 * i + 1] = vectors[i].Y;
            positions[3 * i + 2] = vectors[i].Z;
        }
    }

    /// <summary>
    /// 노말 벡터 데이터를 배열로 변환
    /// </summary>
    private static void LoadNormals(Assimp.Mesh mesh, ref float[] normals)
    {
        Vector3D[] normalList = mesh.Normals.ToArray();
        normals = new float[normalList.Length * 3];
        // XYZ 법선 벡터값 복사
        for (int i = 0; i < normalList.Length; i++)
        {
            normals[3 * i + 0] = normalList[i].X;
            normals[3 * i + 1] = normalList[i].Y;
            normals[3 * i + 2] = normalList[i].Z;
        }
    }

    /// <summary>
    /// 텍스처 좌표 데이터를 배열로 변환
    /// </summary>
    private static void LoadTexCoords(Assimp.Mesh mesh, ref float[] texCoords)
    {
        Vector3D[] texCoordList = mesh.TextureCoordinateChannels[0].ToArray();
        texCoords = new float[texCoordList.Length * 2];
        // UV 좌표값 복사
        for (int i = 0; i < texCoordList.Length; i++)
        {
            texCoords[2 * i + 0] = texCoordList[i].X;
            texCoords[2 * i + 1] = texCoordList[i].Y;
        }
    }

    /// <summary>
    /// 면(Face) 인덱스 데이터 추출
    /// </summary>
    private static List<uint> LoadFaceIndices(Assimp.Mesh mesh)
    {
        List<uint> indices = new List<uint>();
        // 모든 면의 인덱스 수집
        for (int i = 0; i < mesh.FaceCount; i++)
        {
            foreach (uint item in mesh.Faces[i].Indices)
            {
                indices.Add(item);
            }
        }
        return indices;
    }

    /// <summary>
    /// 메시의 모든 텍스처 정보 로드
    /// </summary>
    private static Dictionary<TextureType, List<Texture>> LoadMaterials(
        Assimp.Scene scene, Assimp.Mesh mesh)
    {
        Dictionary<TextureType, List<Texture>> textures =
            new Dictionary<TextureType, List<Texture>>();
        Assimp.Material material = scene.Materials[mesh.MaterialIndex];

        // 텍스처 타입별 로드
        textures[TextureType.Diffuse] = LoadMaterialTextures(material, TextureType.Diffuse);
        textures[TextureType.Specular] = LoadMaterialTextures(material, TextureType.Specular);
        textures[TextureType.Normals] = LoadMaterialTextures(material, TextureType.Normals);

        return textures;
    }

    /// <summary>
    /// 지정된 타입의 텍스처 파일 로드
    /// </summary>
    private static List<Texture> LoadMaterialTextures(
        Assimp.Material mat, Assimp.TextureType typeName)
    {
        List<Texture> textures = new List<Texture>();

        // 해당 타입의 모든 텍스처 처리
        for (int i = 0; i < mat.GetMaterialTextureCount(typeName); i++)
        {
            TextureSlot str;
            mat.GetMaterialTexture(typeName, i, out str);
            string filename = _directory + "\\" + str.FilePath;
            TextureStorage.Add(filename);
        }

        Console.WriteLine(mat.GetMaterialTextureCount(typeName) + "=>mc");
        return textures;
    }
}