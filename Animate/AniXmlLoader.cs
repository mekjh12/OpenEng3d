using Assimp;
using Model3d;
using OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using ZetaExt;

namespace Animate
{
    public class AniXmlLoader
    {
        public static Matrix4x4f LibraryVisualScenesRootTransform(XmlDocument xml)
        {
            XmlNodeList library_visual_scenes = xml.GetElementsByTagName("library_visual_scenes");
            XmlNode nodes = library_visual_scenes[0];// ["visual_scene"];
            string[] value = nodes.InnerText.Split(' ');
            float[] items = new float[value.Length];
            for (int i = 0; i < value.Length; i++) items[i] = float.Parse(value[i]);
            return new Matrix4x4f(items).Transposed;
        }

        public static TexturedModel LoadOnlyGeometryMesh(string filename)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(filename);

            // (1) library_images = textures
            Dictionary<string, Texture> textures = AniXmlLoader.LibraryImages(filename, xml);
            Dictionary<string, string> materialToEffect = AniXmlLoader.LoadMaterials(xml);
            Dictionary<string, string> effectToImage = AniXmlLoader.LoadEffect(xml);

            // (2) library_geometries = position, normal, texcoord, color
            List<MeshTriangles> meshes = AniXmlLoader.LibraryGeometris(xml, out List<Vertex3f> lstPositions, out List<Vertex2f> lstTexCoord, out List<Vertex3f> lstNormals);

            //
            Matrix4x4f transform = AniXmlLoader.LibraryVisualScenesRootTransform(xml);
            //transform = Matrix4x4f.Identity;

            // 읽어온 정보의 인덱스를 이용하여 GPU에 데이터를 전송한다.
            List<TexturedModel> texturedModels = new List<TexturedModel>();
            foreach (MeshTriangles meshTriangles in meshes)
            {
                int count = meshTriangles.Vertices.Count;

                List<Vertex3f> lstVertices = new List<Vertex3f>();
                List<Vertex2f> lstTexs = new List<Vertex2f>();
                List<Vertex3f> lstNors = new List<Vertex3f>();

                for (int i = 0; i < count; i++)
                {
                    int idx = (int)meshTriangles.Vertices[i];
                    int tidx = (int)meshTriangles.Texcoords[i];
                    int nidx = (int)meshTriangles.Normals[i];
                    lstVertices.Add(transform.Multiply(lstPositions[idx]));
                    lstTexs.Add(lstTexCoord[tidx]);
                    lstNors.Add(transform.Multiply(lstNormals[nidx]));
                }

                RawModel3d _rawModel = new RawModel3d();
                _rawModel.Init(vertices: lstVertices.ToArray(), texCoords: lstTexs.ToArray(), normals: lstNors.ToArray());
                _rawModel.GpuBind();

                if (meshTriangles.Material == "")
                {
                    TexturedModel texturedModel = new TexturedModel(_rawModel, null);
                    texturedModel.IsDrawElement = false;
                    texturedModels.Add(texturedModel);
                }
                else
                {
                    string effect = materialToEffect[meshTriangles.Material].Replace("#", "");
                    string imageName = (effectToImage[effect]);
                    Console.WriteLine($"load texture-image {imageName}");
                    if (textures.ContainsKey(imageName))
                    {
                        TexturedModel texturedModel = new TexturedModel(_rawModel, textures[imageName]);
                        texturedModel.IsDrawElement = false;
                        texturedModels.Add(texturedModel);
                    }
                }
            }

            return texturedModels[0];
        }

        /// <summary>
        /// 대입한 모델의 엉덩이 뼈를 기준으로 엉덩이 뼈의 바닥으로부터의 상대적 높이를 반환한다.
        /// </summary>
        /// <param name="animationData">이식을 가져올 애니메이션 행렬 모음</param>
        /// <param name="dicBones">이식할 뼈대의 모음</param>
        /// <returns></returns>
        private static float CalculateHipScaleRatio(Dictionary<string, Dictionary<float, Matrix4x4f>> animationData, Dictionary<string, Bone> dicBones)
        {
            // 알고리즘 설명: 0초의 엉덩이 뼈를 찾아 상대적 비를 계산한다.
            // 
            //               dstSize       이식할 뼈대의 hip Bone의 pivot의 크기
            //  hipScaled = ---------  = ---------------------------------------
            //               srcSize       이식을 가져올 hip Bone의 pivot의 크기
            //
            if (dicBones == null) return 1.0f;

            // 딕셔너리 정보는 뼈이름, <시간, 행렬>로 구성되어 있다.
            foreach (KeyValuePair<string, Dictionary<float, Matrix4x4f>> item in animationData)
            {
                string boneName = item.Key;
                Dictionary<float, Matrix4x4f> timeFrames = item.Value;

                if (!dicBones.ContainsKey(boneName)) continue;

                Bone bone = dicBones[boneName];
                if (bone.IsRootArmature && timeFrames.ContainsKey(0.0f))
                {
                    float dstSize = bone.PivotPosition.Norm();
                    float srcSize = timeFrames[0.0f].Position.Norm();
                    return dstSize / srcSize; // 찾으면 즉시 반환
                }
            }

            return 1.0f; // 루트 본을 찾지 못한 경우 기본값
        }

        /// <summary>
        /// * TextureStorage에 텍스처를 로딩한다. <br/>
        /// - 딕셔너리의 키는 전체파일명으로 한다.<br/>
        /// </summary>
        /// <param name="xml"></param>
        public static Dictionary<string, Texture> LibraryImages(string filenName, XmlDocument xml)
        {
            Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
            string _diffuseFileName;

            XmlNodeList libraryImagesNode = xml.GetElementsByTagName("library_images");
            if (libraryImagesNode.Count > 0)
            {
                foreach (XmlNode imageNode in libraryImagesNode[0].ChildNodes)
                {
                    _diffuseFileName = Path.GetDirectoryName(filenName) + "\\" + imageNode["init_from"].InnerText;
                    _diffuseFileName = _diffuseFileName.Replace("%20", " ");

                    if (!File.Exists(_diffuseFileName))
                    {
                        Console.WriteLine($"[로딩에러] library_image가 존재하지 않습니다. {_diffuseFileName}");
                    }

                    string iamgeId = imageNode.Attributes["id"].Value;
                    Dictionary<TextureType, Texture> _textures = new Dictionary<TextureType, Texture>();
                    Texture texture;
                    if (TextureStorage.TexturesLoaded.ContainsKey(_diffuseFileName)) // 로드한 적이 있음
                    {
                        _textures[TextureType.Diffuse] = TextureStorage.TexturesLoaded[_diffuseFileName];
                        texture = _textures[TextureType.Diffuse];
                    }
                    else  // 로드한 적이 없음
                    {
                        texture = new Texture(_diffuseFileName);
                        _textures.Add(TextureType.Diffuse, texture);
                        TextureStorage.TexturesLoaded.Add(_diffuseFileName, texture);
                    }
                    textures.Add(iamgeId, texture);
                }
            }

            return textures;
        }

        public static Dictionary<string, string> LoadMaterials(XmlDocument xml)
        {
            Dictionary<string, string> materials = new Dictionary<string, string>();
            XmlNodeList libraryMaterials = xml.GetElementsByTagName("library_materials");

            if (libraryMaterials.Count > 0)
            {
                foreach (XmlNode material in libraryMaterials[0].ChildNodes)
                {
                    string key = material.Attributes["id"].Value;
                    string value = material["instance_effect"].Attributes["url"].Value;
                    materials.Add(key, value);
                }
            }

            return materials;
        }

        public static Dictionary<string, string> LoadEffect(XmlDocument xml)
        {
            Dictionary<string, string> effects = new Dictionary<string, string>();
            XmlNodeList libraryEffects = xml.GetElementsByTagName("library_effects");

            if (libraryEffects.Count > 0)
            {
                foreach (XmlNode effect in libraryEffects[0].ChildNodes)
                {
                    string key = effect.Attributes["id"].Value;
                    string value = effect["profile_COMMON"]["newparam"].InnerText;
                    effects.Add(key, value);
                }
            }

            return effects;
        }

        public static List<MeshTriangles> LibraryGeometris(XmlDocument xml,
            out List<Vertex3f> lstPositions, 
            out List<Vertex2f> lstTexCoord, 
            out List<Vertex3f> lstNormals)
        {
            List<MeshTriangles> meshTriangles = new List<MeshTriangles>();

            List<uint> lstVertexIndices = new List<uint>();
            List<uint> texcoordIndices = new List<uint>();
            List<uint> lstNormalIndices = new List<uint>();
            lstPositions = new List<Vertex3f>();
            lstTexCoord = new List<Vertex2f>();
            lstNormals = new List<Vertex3f>();

            uint pOffset = 0;
            uint tOffset = 0;
            uint nOffset = 0;

            XmlNodeList libraryGeometries = xml.GetElementsByTagName("library_geometries");

            if (libraryGeometries.Count > 0)
            {
                XmlNode library_geometries = libraryGeometries[0];

                foreach (XmlNode geometry in library_geometries.ChildNodes)
                {
                    XmlNode vertices = geometry["mesh"]["vertices"];
                    string positionName = vertices["input"].Attributes["source"].Value;
                    string vertexName = positionName.Replace("-positions", "-vertices");
                    string normalName = positionName.Replace("-positions", "-normals");
                    string texcoordName = positionName.Replace("-positions", "-map-0");
                    string colorName = positionName.Replace("-positions", "-colors-Col");

                    foreach (XmlNode node in geometry["mesh"])
                    {
                        uint pNum = 0; // position 개수
                        uint tNum = 0; // texcoord 개수
                        uint nNum = 0; // normal 개수

                        // 기본 데이터 source를 읽어옴.
                        if (node.Name == "source")
                        {
                            // 소스텍스트로부터 실수 배열을 만든다.
                            string sourcesId = node.Attributes["id"].Value.Replace(" ", "");
                            string[] value = node["float_array"].InnerText.Split(' ');
                            float[] items = new float[value.Length];
                            for (int i = 0; i < value.Length; i++)
                                items[i] = float.Parse(value[i]);

                            if ("#" + sourcesId == positionName)
                            {
                                for (int i = 0; i < items.Length; i += 3)
                                {
                                    lstPositions.Add(new Vertex3f(items[i], items[i + 1], items[i + 2]));
                                }
                                pNum = (uint)(items.Length / 3);
                            }
                            else if ("#" + sourcesId == texcoordName)
                            {
                                for (int i = 0; i < items.Length; i += 2)
                                {
                                    lstTexCoord.Add(new Vertex2f(items[i], 1.0f - items[i + 1]));
                                }
                                tNum = (uint)(items.Length / 2);
                            }
                            else if ("#" + sourcesId == normalName)
                            {
                                for (int i = 0; i < items.Length; i += 3)
                                {
                                    lstNormals.Add(new Vertex3f(items[i], items[i + 1], items[i + 2]));
                                }
                                nNum = (uint)(items.Length / 3);
                            }
                            else if ("#" + sourcesId == colorName)
                            {
                            }
                        }

                        // triangles만 처리
                        if (node.Name == "triangles")
                        {
                            XmlNode triangles = node;

                            int vertexOffset = -1;
                            int normalOffset = -1;
                            int texcoordOffset = -1;
                            int colorOffset = -1;

                            // offset 읽어온다. pos, tex, nor, color                        
                            foreach (XmlNode input in triangles.ChildNodes)
                            {
                                if (input.Name == "input")
                                {
                                    if (input.Attributes["semantic"].Value == "VERTEX")
                                    {
                                        vertexName = input.Attributes["source"].Value;
                                        vertexOffset = int.Parse(input.Attributes["offset"].Value);
                                    }
                                    if (input.Attributes["semantic"].Value == "NORMAL")
                                    {
                                        normalName = input.Attributes["source"].Value;
                                        normalOffset = int.Parse(input.Attributes["offset"].Value);
                                    }
                                    if (input.Attributes["semantic"].Value == "TEXCOORD")
                                    {
                                        texcoordName = input.Attributes["source"].Value;
                                        texcoordOffset = int.Parse(input.Attributes["offset"].Value);
                                    }
                                    if (input.Attributes["semantic"].Value == "COLOR")
                                    {
                                        colorName = input.Attributes["source"].Value;
                                        colorOffset = int.Parse(input.Attributes["offset"].Value);
                                    }
                                }
                            }

                            XmlNode p = node["p"];
                            string[] values = p.InnerText.Split(new char[] { ' ' });
                            int total = (vertexOffset >= 0 ? 1 : 0) + (normalOffset >= 0 ? 1 : 0)
                                + (texcoordOffset >= 0 ? 1 : 0) + (colorOffset >= 0 ? 1 : 0);

                            for (int i = 0; i < values.Length; i += total)
                            {
                                if (vertexOffset >= 0) lstVertexIndices.Add(pOffset + uint.Parse(values[i + vertexOffset]));
                                if (texcoordOffset >= 0) texcoordIndices.Add(tOffset + uint.Parse(values[i + texcoordOffset]));
                                if (normalOffset >= 0) lstNormalIndices.Add(nOffset + uint.Parse(values[i + normalOffset]));
                                //if (colorOffset >= 0) colorIndices.Add(uint.Parse(values[i + colorOffset]));
                            }

                            pOffset += pNum;
                            tOffset += tNum;
                            nOffset += nNum;

                            string materialName = node.Attributes["material"] != null ? node.Attributes["material"].Value : "";

                            // 버텍스, 텍스쳐좌표, 법선벡터의 인덱스를 모두 모아서 MeshTriangles로 만든다.
                            MeshTriangles triMesh = new MeshTriangles();
                            triMesh.Material = materialName;
                            triMesh.AddVertices(lstVertexIndices.ToArray());
                            triMesh.AddTexCoords(texcoordIndices.ToArray());
                            triMesh.AddNormals(lstNormalIndices.ToArray());
                            meshTriangles.Add(triMesh);
                        }
                    }
                }
            }

            return meshTriangles;
        }

        /// <summary>
        /// COLLADA XML 파일에서 라이브러리 컨트롤러를 파싱하여 반환한다.
        /// <remarks>
        /// <para>뼈대 애니메이션을 위한 본 이름, 역바인드 포즈, 본 인덱스 및 가중치 정보를 추출합니다.</para>
        /// <para>본 인덱스 및 가중치 정보는 읽어온 지오메트리의 정점의 갯수만큼 생성됩니다.</para>
        /// </remarks>
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="boneNames"></param>
        /// <param name="invBindPoses"></param>
        /// <param name="lstBoneIndex"></param>
        /// <param name="lstBoneWeight"></param>
        /// <param name="bindShapeMatrix"></param>
        public static void LibraryController(XmlDocument xml, 
            out List<string> boneNames, 
            out Dictionary<string, Matrix4x4f> invBindPoses,
            out List<VertexBoneData> vertexBoneData,
            out Matrix4x4f bindShapeMatrix)
        {
            bindShapeMatrix = Matrix4x4f.Identity;

            // 초기화
            vertexBoneData = new List<VertexBoneData>();

            string jointsName = "";
            string inverseBindMatrixName = "";
            string weightName = "";
            int jointsOffset = -1;
            int weightOffset = -1;
            invBindPoses = new Dictionary<string, Matrix4x4f>();
            boneNames = new List<string>();

            List<float> weightList = new List<float>();

            XmlNodeList libraryControllers = xml.GetElementsByTagName("library_controllers");
            if (libraryControllers.Count > 0)
            {
                XmlNode libraryController = libraryControllers[0];

                foreach (XmlNode controller in libraryController.ChildNodes)
                {
                    XmlNode joints = controller["skin"]["joints"];
                    XmlNode vertex_weights = controller["skin"]["vertex_weights"];

                    // bind_shape_matrix 읽어옴.
                    string[] eles = controller["skin"]["bind_shape_matrix"].InnerText.Split(' ');
                    float[] eleValues = new float[eles.Length];
                    for (int i = 0; i < eles.Length; i++)
                        eleValues[i] = float.Parse(eles[i]);
                    bindShapeMatrix = new Matrix4x4f(eleValues).Transposed;

                    // joints 읽어옴.
                    foreach (XmlNode input in joints.ChildNodes)
                    {
                        if (input.Name == "input")
                        {
                            // joint와 inv_bind_mat의 semantic을 읽어온다.
                            switch (input.Attributes["semantic"].Value)
                            {
                                case "JOINT":
                                    jointsName = input.Attributes["source"].Value;
                                    break;
                                case "INV_BIND_MATRIX":
                                    inverseBindMatrixName = input.Attributes["source"].Value;
                                    break;
                            }

                            // source 읽어오기
                            foreach (XmlNode source in controller["skin"].ChildNodes)
                            {
                                if (source.Name == "source")
                                {
                                    string sourcesId = source.Attributes["id"].Value;

                                    // BoneName 읽어오기
                                    if (source["Name_array"] != null)
                                    {
                                        string[] value = source["Name_array"].InnerText.Split(' ');
                                        if ("#" + sourcesId == jointsName)
                                        {
                                            boneNames.Clear();
                                            boneNames.AddRange(value);
                                        }
                                    }

                                    // INV_BIND_MATRIX 읽어오기
                                    if (source["float_array"] != null)
                                    {
                                        string[] value = source["float_array"].InnerText.Split(' ');
                                        float[] items = new float[value.Length];
                                        for (int i = 0; i < value.Length; i++)
                                            items[i] = float.Parse(value[i]);

                                        // INV_BIND_MATRIX
                                        if ("#" + sourcesId == inverseBindMatrixName)
                                        {
                                            for (int i = 0; i < items.Length; i += 16)
                                            {
                                                List<float> mat = new List<float>();
                                                for (int j = 0; j < 16; j++) mat.Add(items[i + j]);
                                                Matrix4x4f bindpose = new Matrix4x4f(mat.ToArray());
                                                if (!invBindPoses.ContainsKey(boneNames[i / 16]))
                                                {
                                                    invBindPoses.Add(boneNames[i / 16], bindpose.Transposed);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // vertex_weights 읽어옴.
                    foreach (XmlNode input in vertex_weights.ChildNodes)
                    {
                        if (input.Name == "input")
                        {
                            // name 가져오기
                            if (input.Attributes["semantic"].Value == "WEIGHT") weightName = input.Attributes["source"].Value;
                            foreach (XmlNode source in controller["skin"].ChildNodes)
                            {
                                if (source.Name == "source")
                                {
                                    string sourcesId = source.Attributes["id"].Value;
                                    if (source["float_array"] != null)
                                    {
                                        string[] value = source["float_array"].InnerText.Split(' ');
                                        float[] items = new float[value.Length];
                                        for (int i = 0; i < value.Length; i++)
                                            items[i] = float.Parse(value[i]);

                                        // WEIGHT
                                        if ("#" + sourcesId == weightName) weightList.AddRange(items);
                                    }
                                }
                            }
                        }
                    }

                    // vertex_weights - vcount, v 읽어옴.
                    XmlNode vcount = controller["skin"]["vertex_weights"]["vcount"];
                    XmlNode v = controller["skin"]["vertex_weights"]["v"];
                    string[] vcountArray = vcount.InnerText.Trim().Split(' ');
                    int[] vcountIntArray = new int[vcountArray.Length];
                    uint total = 0;
                    for (int i = 0; i < vcountArray.Length; i++)
                    {
                        vcountIntArray[i] = int.Parse(vcountArray[i].Trim());
                        total += (uint)vcountIntArray[i];
                    }

                    foreach (XmlNode input in controller["skin"]["vertex_weights"].ChildNodes)
                    {
                        if (input.Name == "input")
                        {
                            if (input.Attributes["semantic"].Value == "JOINT") jointsOffset = int.Parse(input.Attributes["offset"].Value);
                            if (input.Attributes["semantic"].Value == "WEIGHT") weightOffset = int.Parse(input.Attributes["offset"].Value);
                        }
                    }

                    string[] vArray = v.InnerText.Split(' ');
                    int sum = 0;
                    for (int i = 0; i < vcountIntArray.Length; i++)
                    {
                        int vertexCount = vcountIntArray[i];
                        List<int> boneIndexList = new List<int>();
                        List<int> boneWeightList = new List<int>();

                        for (int j = 0; j < vertexCount; j++)
                        {
                            if (jointsOffset >= 0)
                                boneIndexList.Add(int.Parse(vArray[sum + 2 * j + jointsOffset].Trim()));
                            if (weightOffset >= 0)
                                boneWeightList.Add(int.Parse(vArray[sum + 2 * j + weightOffset].Trim()));
                        }

                        // 가중치가 높은 순서로 정렬한다.
                        List<Vertex2f> bwList = new List<Vertex2f>();
                        for (int k = 0; k < boneWeightList.Count; k++)
                        {
                            float w = weightList[boneWeightList[k]];
                            bwList.Add(new Vertex2f(boneIndexList[k], w));
                        }

                        // 가중치가 높은 순서로 정렬한다.
                        bwList.Sort((a, b) => b.y.CompareTo(a.y));

                        // 4개까지만 남기고 나머지는 합산한다.
                        if (bwList.Count > 4)
                        {
                            for (int k = 4; k < bwList.Count; k++)
                            {
                                bwList[0] = new Vertex2f(bwList[0].x, bwList[0].y + bwList[k].y);
                            }
                            bwList.RemoveRange(4, bwList.Count - 4);
                        }

                        // 정렬된 가중치 정보를 리스트에 담는다.
                        Vertex4i jointId = Vertex4i.Zero;
                        Vertex4f weight = Vertex4f.Zero;

                        for (int k = 0; k < bwList.Count; k++)
                        {
                            if (k == 0) jointId.x = (int)bwList[k].x;
                            if (k == 1) jointId.y = (int)bwList[k].x;
                            if (k == 2) jointId.z = (int)bwList[k].x;
                            if (k == 3) jointId.w = (int)bwList[k].x;
                            if (k == 0) weight.x = bwList[k].y;
                            if (k == 1) weight.y = bwList[k].y;
                            if (k == 2) weight.z = bwList[k].y;
                            if (k == 3) weight.w = bwList[k].y;
                        }

                        var boneData = new VertexBoneData(jointId, weight);
                        vertexBoneData.Add(boneData);

                        sum += 2 * vertexCount;
                    }
                }
            }
        }

        /// <summary>
        /// AniDae클래스에 Motion를 추가한다.
        /// </summary>
        /// <param name="aniDae"></param>
        /// <param name="motionFileName"></param>
        public static void AttachMotion(AniDae aniDae, string motionFileName)
        {
            Motion motion = LoadMixamoMotion(aniDae, motionFileName);
            aniDae.Motions.AddMotion(motion);
        }

        /// <summary>
        /// * Mixamo에서 Export한 Dae파일을 그대로 읽어온다. <br/>
        /// - Without Skin, Only Armature <br/>
        /// - "3D Mesh Processing and Character Animation", p.183 Animation Retargeting
        /// </summary>
        /// <param name="aniDae"></param>
        /// <param name="motionFileName"></param>
        public static Motion LoadMixamoMotion(AniDae aniDae, string motionFileName)
        {
            // Dae 파일을 읽어온다.
            XmlDocument xml = new XmlDocument();
            xml.Load(motionFileName);
            string motionName = Path.GetFileNameWithoutExtension(motionFileName);

            // dae 파일 구조에서 애니메이션 구조를 읽어온다.
            XmlNodeList libraryAnimations = xml.GetElementsByTagName("library_animations");
            if (libraryAnimations.Count == 0)
            {
                Console.WriteLine($"{motionName} dae 파일 구조에서 애니메이션 구조를 읽어올 수 없습니다.");
                return null;
            }

            // 애니메이션 정보를 담을 딕셔너리 생성 (boneName, Dictionary<time, Matrix4x4f>)
            Dictionary<string, Dictionary<float, Matrix4x4f>> animationData = new Dictionary<string, Dictionary<float, Matrix4x4f>>();
            float maxTimeLength = 0.0f;

            // 각 뼈의 애니메이션 소스를 읽어온다.
            // 행렬 정보는 4x4 행렬로 되어있고, 시간은 float로 되어있다.
            // 행렬은 캐릭터의 발 밑 가운데를 원점으로 하는 캐릭터 공간 변환행렬이다.
            foreach (XmlNode boneAnimation in libraryAnimations[0].ChildNodes)
            {
                // boneAnimation은 <animation> 태그로 되어있다.
                string boneName = boneAnimation.Attributes["id"].Value;
                boneName = boneName.Substring(0, boneName.Length - 5);
                if (boneName == "Armature") continue;

                // 애니메이션 소스의 시간과 행렬을 담을 리스트를 생성한다.
                List<float> sourceInput = new List<float>(); 
                List<Matrix4x4f> sourceOutput = new List<Matrix4x4f>();
                List<string> interpolationInput = new List<string>();

                // 채널과 샘플러를 가져온다.
                XmlNode channel = boneAnimation["channel"];
                string channelName = channel.Attributes["source"].Value;
                XmlNode sampler = boneAnimation["sampler"];
                if (channelName != "#" + sampler.Attributes["id"].Value) continue;

                // sampler의 INPUT, OUTPUT, INTERPOLATION을 읽어온다.
                string inputName = "";
                string outputName = "";
                string interpolationName = "";
                foreach (XmlNode input in sampler.ChildNodes)
                {
                    if (input.Attributes["semantic"].Value == "INPUT") inputName = input.Attributes["source"].Value;
                    if (input.Attributes["semantic"].Value == "OUTPUT") outputName = input.Attributes["source"].Value;
                    if (input.Attributes["semantic"].Value == "INTERPOLATION") interpolationName = input.Attributes["source"].Value;
                }

                // 각 뼈마다 시간과 행렬을 가져온다.
                foreach (XmlNode source in boneAnimation.ChildNodes)
                {
                    if (source.Name == "source")
                    {
                        // source의 id를 읽어온다.
                        string sourcesId = source.Attributes["id"].Value;
                        if ("#" + sourcesId == inputName)
                        {
                            // 시간 배열을 가져오고 최대시간을 얻는다.
                            string[] value = source["float_array"].InnerText.Trim().Replace("\n", " ").Split(' ');
                            float[] items = new float[value.Length];
                            for (int i = 0; i < value.Length; i++)
                            {
                                items[i] = float.Parse(value[i].Trim());
                                maxTimeLength = Math.Max(items[i], maxTimeLength);
                            }
                            sourceInput.AddRange(items);
                        }

                        // source의 행렬을 읽어온다. 
                        // 행렬은 각 본의 로컬 공간 변환행렬 (부모 본에 대한 상대적 변환)
                        if ("#" + sourcesId == outputName)
                        {
                            string[] value = source["float_array"].InnerText.Trim().Replace("\n", " ").Split(' ');
                            float[] items = new float[value.Length];
                            for (int i = 0; i < value.Length; i++) items[i] = float.Parse(value[i]);
                            for (int i = 0; i < value.Length; i += 16)
                            {
                                List<float> mat = new List<float>();
                                for (int j = 0; j < 16; j++) mat.Add(items[i + j]);

                                // Mixamo에서 Export한 Dae파일은 행렬이 Transposed되어 있다.
                                Matrix4x4f matrix = new Matrix4x4f(mat.ToArray());

                                // 0열부터 2열은 회전정보이고, 3열은 위치정보이다.
                                sourceOutput.Add(matrix.Transposed);
                            }
                        }

                        // source의 INTERPOLATION을 읽어온다. (예) LINEAR, BEZIER 등
                        if ("#" + sourcesId == interpolationName)
                        {
                            string[] value = source["Name_array"].InnerText.Trim().Replace("\n", " ").Split(' ');
                            interpolationInput.AddRange(value);
                        }
                    }
                }

                // 가져온 소스로 키프레임을 만든다.
                Dictionary<float, Matrix4x4f> keyframe = new Dictionary<float, Matrix4x4f>();
                for (int i = 0; i < sourceInput.Count; i++)
                {
                    keyframe.Add(sourceInput[i], sourceOutput[i]);
                }

                animationData.Add(boneName, keyframe);
            }

            // *** [중요] 바닥으로부터 엉덩이 위치를 맞추기 위하여 hipHeightScale을 구한다.
            // Interpolation Pose만 0초에서 정상적 T-pose를 취하고 있어서 이 부분에서 가져와야 한다.
            if (motionName == "a-T-Pose") //Interpolation Pose
            {
                aniDae.HipHeightScale = CalculateHipScaleRatio(animationData, aniDae.DicBones);
                Console.WriteLine($"XmeDae HipScaled={aniDae.HipHeightScale}");
            }

            // 애니메이션을 생성한다.
            Motion motion = new Motion(motionName, maxTimeLength);
            if (maxTimeLength > 0 && aniDae.DicBones != null)
            {
                // 뼈마다 순회 (뼈, 시간, 로컬변환행렬)
                foreach (KeyValuePair<string, Dictionary<float, Matrix4x4f>> item in animationData)
                {
                    string boneName = item.Key;
                    Dictionary<float, Matrix4x4f> source = item.Value;

                    Bone bone = aniDae.GetBoneByName(boneName);
                    if (bone == null) continue;

                    // 시간마다 순회 (시간, 로컬변환행렬)
                    foreach (KeyValuePair<float, Matrix4x4f> subsource in source)
                    {
                        float time = subsource.Key;
                        Matrix4x4f mat = subsource.Value;

                        // 키프레임을 추가한다.
                        motion.AddKeyFrame(time);

                        // 본포즈를 설정한다.
                        Vertex3f position = bone.IsRootArmature ?
                            mat.Position * aniDae.HipHeightScale : bone.PivotPosition * 0.001f;
                        ZetaExt.Quaternion q = mat.ToQuaternion();
                        q.Normalize();
                        BoneTransform boneTransform  = new BoneTransform(position, q);

                        // 시간에 본포즈를 추가한다.
                        motion[time].AddBoneTransform(boneName, boneTransform);
                    }
                }
            }

            return motion;
        }

        /// <summary>
        /// COLLADA XML 파일에서 애니메이션 라이브러리를 파싱하여 AniDae 객체에 모션 데이터를 추가하는 메서드
        /// </summary>
        /// <param name="aniDae">애니메이션 데이터를 저장할 AniDae 객체</param>
        /// <param name="xml">파싱할 COLLADA XML 문서</param>
        public static void LibraryAnimations(AniDae aniDae, XmlDocument xml)
        {
            // XML에서 library_animations 노드를 찾음
            XmlNodeList libraryAnimations = xml.GetElementsByTagName("library_animations");
            if (libraryAnimations.Count == 0)
            {
                Console.WriteLine($"현재 캐릭터 파일에 library_animations 노드가 없습니다.");
                return;
            }

            // 애니메이션 데이터를 저장할 딕셔너리 (본 이름 -> 시간별 변환 매트릭스)
            Dictionary<string, Dictionary<float, Matrix4x4f>> bonenameKeyframeDic = new Dictionary<string, Dictionary<float, Matrix4x4f>>();

            // 각 애니메이션을 순회
            foreach (XmlNode libraryAnimation in libraryAnimations[0])
            {
                // 애니메이션 이름 추출
                string animationName = libraryAnimation.Attributes["name"].Value;
                float maxTimeLength = 0.0f;  // 애니메이션의 최대 시간 길이
                string motionName = "";      // 모션 이름

                // 각 본(bone)의 애니메이션 데이터를 순회
                foreach (XmlNode boneAnimation in libraryAnimation.ChildNodes)
                {
                    // 본 이름 추출 및 정리
                    string boneName = boneAnimation.Attributes["id"].Value.Substring(animationName.Length + 1);
                    int fIdx = boneName.IndexOf("_");
                    motionName = (fIdx >= 0) ? boneName.Substring(0, fIdx) : "";
                    boneName = (fIdx >= 0) ? boneName.Substring(fIdx + 1) : boneName;
                    boneName = boneName.Replace("_pose_matrix", "");

                    // 애니메이션 데이터 저장용 리스트들
                    List<float> sourceInput = new List<float>();           // 시간 간격 데이터
                    List<Matrix4x4f> sourceOutput = new List<Matrix4x4f>(); // 변환 매트릭스 데이터
                    List<string> interpolationInput = new List<string>();   // 보간 방식 데이터

                    // 채널 정보 가져오기
                    XmlNode channel = boneAnimation["channel"];
                    string channelName = channel.Attributes["source"].Value;

                    // 샘플러 정보 가져오기
                    XmlNode sampler = boneAnimation["sampler"];
                    if (channelName != "#" + sampler.Attributes["id"].Value) continue;

                    // 입력, 출력, 보간 소스의 이름 저장용 변수들
                    string inputName = "";
                    string outputName = "";
                    string interpolationName = "";

                    // 샘플러의 입력들을 순회하여 각 semantic(의미)의 소스 이름을 읽어옴
                    foreach (XmlNode input in sampler.ChildNodes)
                    {
                        if (input.Attributes["semantic"].Value == "INPUT") inputName = input.Attributes["source"].Value;
                        if (input.Attributes["semantic"].Value == "OUTPUT") outputName = input.Attributes["source"].Value;
                        if (input.Attributes["semantic"].Value == "INTERPOLATION") interpolationName = input.Attributes["source"].Value;
                    }

                    // 본의 애니메이션 소스 데이터를 읽어옴
                    foreach (XmlNode source in boneAnimation.ChildNodes)
                    {
                        if (source.Name == "source")
                        {
                            string sourcesId = source.Attributes["id"].Value;

                            // INPUT 소스 처리 (시간 데이터)
                            if ("#" + sourcesId == inputName)
                            {
                                string[] value = source["float_array"].InnerText.Split(' ');
                                float[] items = new float[value.Length];
                                for (int i = 0; i < value.Length; i++)
                                {
                                    items[i] = float.Parse(value[i]);
                                    maxTimeLength = Math.Max(items[i], maxTimeLength); // 최대 시간 길이 업데이트
                                }
                                sourceInput.AddRange(items);
                            }

                            // OUTPUT 소스 처리 (변환 매트릭스 데이터)
                            if ("#" + sourcesId == outputName)
                            {
                                string[] value = source["float_array"].InnerText.Split(' ');
                                float[] items = new float[value.Length];
                                for (int i = 0; i < value.Length; i++) items[i] = float.Parse(value[i]);

                                // 16개 float 값으로 4x4 매트릭스를 생성 (매트릭스는 16개 요소로 구성)
                                for (int i = 0; i < value.Length; i += 16)
                                {
                                    List<float> mat = new List<float>();
                                    for (int j = 0; j < 16; j++) mat.Add(items[i + j]);
                                    Matrix4x4f matrix = new Matrix4x4f(mat.ToArray());
                                    sourceOutput.Add(matrix.Transposed); // 전치 매트릭스로 저장
                                }
                            }

                            // INTERPOLATION 소스 처리 (보간 방식 데이터)
                            if ("#" + sourcesId == interpolationName)
                            {
                                string[] value = source["Name_array"].InnerText.Split(' ');
                                interpolationInput.AddRange(value);
                            }
                        }
                    }

                    // 가져온 소스 데이터로 키프레임 딕셔너리를 생성
                    Dictionary<float, Matrix4x4f> keyframe = new Dictionary<float, Matrix4x4f>();
                    for (int i = 0; i < sourceInput.Count; i++)
                    {
                        keyframe.Add(sourceInput[i], sourceOutput[i]); // 시간을 키로, 매트릭스를 값으로 저장
                    }

                    // 본 이름을 키로 하여 키프레임 데이터를 애니메이션 딕셔너리에 추가
                    bonenameKeyframeDic.Add(boneName, keyframe);
                }

                // 모션 객체 생성
                Motion motion = new Motion(motionName, maxTimeLength);

                // 애니메이션 시간이 0보다 클 때만 키프레임 데이터를 모션에 추가
                if (maxTimeLength > 0)
                {
                    // 각 본의 애니메이션 데이터를 순회
                    foreach (KeyValuePair<string, Dictionary<float, Matrix4x4f>> item in bonenameKeyframeDic)
                    {
                        string boneName = item.Key;
                        Dictionary<float, Matrix4x4f> source = item.Value;

                        // 각 시간대별 변환 데이터를 순회
                        foreach (KeyValuePair<float, Matrix4x4f> subsource in source)
                        {
                            float time = subsource.Key;          // 키프레임 시간
                            Matrix4x4f mat = subsource.Value;    // 변환 매트릭스

                            // 모션에 키프레임 시간 추가
                            motion.AddKeyFrame(time);

                            // 매트릭스에서 쿼터니언 회전값 추출 및 정규화
                            ZetaExt.Quaternion q = mat.ToQuaternion();
                            q.Normalize();

                            // 본 포즈 객체 생성 및 위치/회전 설정
                            BoneTransform boneTransform = new BoneTransform();
                            boneTransform.Position = new Vertex3f(mat[3, 0], mat[3, 1], mat[3, 2]); // 매트릭스에서 위치 추출
                            boneTransform.Rotation = q; // 회전값 설정

                            // 해당 시간의 키프레임에 본 변환 데이터 추가
                            motion[time].AddBoneTransform(boneName, boneTransform);
                        }
                    }
                }

                // 완성된 모션을 AniDae 객체의 모션 컬렉션에 추가
                aniDae.Motions.AddMotion(motion);
            }
        }

        /// <summary>
        /// COLLADA XML 파일에서 뼈대 구조를 파싱하여 루트 본을 반환한다.
        /// </summary>
        /// <param name="xml">XML</param>
        /// <param name="invBindPoses">역바인드 포즈 딕셔너리</param>
        /// <param name="dicBoneIndex">본익덱스 딕셔너리</param>
        /// <param name="dicBones">생성한 본딕셔너리</param>
        /// <returns>루트본을 반환</returns>
        public static Bone LibraryVisualScenes(XmlDocument xml, 
            Dictionary<string, Matrix4x4f> invBindPoses,
            Dictionary<string, int> dicBoneIndex,
            out Dictionary<string, Bone> dicBones)
        {
            // 뼈대 구조를 읽기 위하여 준비한다.
            XmlNodeList library_visual_scenes = xml.GetElementsByTagName("library_visual_scenes");
            dicBones = new Dictionary<string, Bone>();
            if (library_visual_scenes.Count == 0)
            {
                Console.WriteLine($"[에러] dae파일구조에서 뼈대구조를 읽어올 수 없습니다.");
                return null;
            }

            // 뼈대 구조를 읽기 위해 스택을 준비한다.
            Stack<XmlNode> nStack = new Stack<XmlNode>();
            Stack<Bone> bStack = new Stack<Bone>();
            XmlNode nodes = library_visual_scenes[0]["visual_scene"];
            XmlNode rootNode = null;

            // Armature 노드를 찾는다.
            foreach (XmlNode item in nodes)
                if (item.Attributes["id"].Value == "Armature") rootNode = item;
            if (rootNode == null) return null; // Armature 노드가 없으면 null 반환

            nStack.Push(rootNode);
            Bone rootBone = new Bone("Armature", 0);
            bStack.Push(rootBone);

            while (nStack.Count > 0)
            {
                XmlNode node = nStack.Pop();
                Bone bone = bStack.Pop();

                // 노드의 변환 행렬을 읽어온다.
                string[] value = node["matrix"].InnerText.Split(' ');
                float[] items = new float[value.Length];
                for (int i = 0; i < value.Length; i++) items[i] = float.Parse(value[i]);
                Matrix4x4f mat = new Matrix4x4f(items).Transposed;

                // 본 이름을 읽어온다.
                string boneName = node.Attributes["sid"]?.Value;

                // 본을 생성하고 딕셔너리에 추가한다.
                if (boneName != null)
                    dicBones.Add(boneName, bone);

                // 본의 이름과 변환 행렬을 설정한다.
                if (boneName == null)
                {
                    if (node.Attributes["name"].Value == "Armature")
                    {
                        bone.Name = "Armature";
                        bone.LocalBindTransform = mat;
                        bone.Index = 0;
                    }
                }
                else
                {
                    bone.Name = boneName;
                    bone.LocalBindTransform = mat;
                    bone.Index = dicBoneIndex.ContainsKey(boneName) ? dicBoneIndex[boneName] : -1;
                }

                bone.PivotPosition = mat.Column3.Vertex3f();

                // 역바인드 포즈를 설정한다.
                if (invBindPoses.ContainsKey(bone.Name))
                {
                    bone.InverseBindTransform = invBindPoses[bone.Name];
                }

                // 하위 노드를 순회한다.
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (child.Name != "node") continue;
                    nStack.Push(child);
                    Bone childBone = new Bone("", 0);
                    childBone.Parent = bone;
                    bone.AddChild(childBone);
                    bStack.Push(childBone);
                }
            }

            return rootBone;
        }


    }
}
