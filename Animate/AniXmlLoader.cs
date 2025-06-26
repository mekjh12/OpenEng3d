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
        /// 엉덩이 뼈의 바닥으로부터의 상대적 높이를 반환한다.
        /// </summary>
        /// <param name="ani">이식을 가져올 애니메이션 행렬 모음</param>
        /// <param name="dicBones">이식할 뼈대의 모음</param>
        /// <returns></returns>
        private static float LoadDefaultScaleTransform(Dictionary<string, Dictionary<float, Matrix4x4f>> ani,
            Dictionary<string, Bone> dicBones)
        {
            // 알고리즘 설명: 0초의 엉덩이 뼈를 찾아 상대적 비를 계산한다.
            // 
            //               dstSize       이식할 뼈대의 hip Bone의 pivot의 크기
            //  hipScaled = ---------  = ---------------------------------------
            //               srcSize       이식을 가져올 hip Bone의 pivot의 크기
            //
            if (dicBones == null) return 1.0f;

            float hipScaled = 1.0f;

            // 뼈마다 순회
            foreach (KeyValuePair<string, Dictionary<float, Matrix4x4f>> item in ani)
            {
                string boneName = item.Key;
                Dictionary<float, Matrix4x4f> source = item.Value;

                Bone bone = dicBones.ContainsKey(boneName) ? dicBones[boneName] : null;
                if (bone == null) continue;

                // 뼈의 시간에 따른 순회
                foreach (KeyValuePair<float, Matrix4x4f> subsource in source)
                {
                    float time = subsource.Key;
                    Matrix4x4f mat = subsource.Value;

                    if (bone.IsRootArmature)
                    {
                        float dstSize = bone.PivotPosition.Norm();
                        float srcSize = source[0.0f].Position.Norm();
                        hipScaled = dstSize / srcSize;
                    }
                }
            }

            return hipScaled;
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
            out List<Vertex3f> lstPositions, out List<Vertex2f> lstTexCoord, out List<Vertex3f> lstNormals)
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
                        uint pNum = 0;
                        uint tNum = 0;
                        uint nNum = 0;

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

        public static void LibraryController(XmlDocument xml, out List<string> boneNames, out Dictionary<string, Matrix4x4f> invBindPoses,
            out List<Vertex4i> lstBoneIndex, out List<Vertex4f> lstBoneWeight, out Matrix4x4f bindShapeMatrix)
        {
            bindShapeMatrix = Matrix4x4f.Identity;

            lstBoneIndex = new List<Vertex4i>();
            lstBoneWeight = new List<Vertex4f>();

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
                            // name 가져오기
                            if (input.Attributes["semantic"].Value == "JOINT")
                            {
                                jointsName = input.Attributes["source"].Value;
                            }
                            if (input.Attributes["semantic"].Value == "INV_BIND_MATRIX")
                            {
                                inverseBindMatrixName = input.Attributes["source"].Value;
                            }

                            foreach (XmlNode source in controller["skin"].ChildNodes)
                            {
                                if (source.Name == "source")
                                {
                                    string sourcesId = source.Attributes["id"].Value;

                                    if (source["Name_array"] != null)
                                    {
                                        string[] value = source["Name_array"].InnerText.Split(' ');

                                        // BoneName가져오기
                                        if ("#" + sourcesId == jointsName)
                                        {
                                            boneNames.Clear();
                                            boneNames.AddRange(value);
                                        }
                                    }

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

                        // 정렬하기
                        List<Vertex2f> bwList = new List<Vertex2f>();
                        for (int k = 0; k < boneWeightList.Count; k++)
                        {
                            float w = weightList[boneWeightList[k]];
                            bwList.Add(new Vertex2f(boneIndexList[k], w));
                        }

                        bwList.Sort((a, b) => b.y.CompareTo(a.y));
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

                        lstBoneIndex.Add(jointId);
                        lstBoneWeight.Add(weight);
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
        /// <param name="xml"></param>
        /// <param name="motionName"></param>
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

            Dictionary<string, Dictionary<float, Matrix4x4f>> ani = new Dictionary<string, Dictionary<float, Matrix4x4f>>();
            float maxTimeLength = 0.0f;

            // bone마다 순회
            foreach (XmlNode boneAnimation in libraryAnimations[0].ChildNodes)
            {
                string boneName = boneAnimation.Attributes["id"].Value;
                boneName = boneName.Substring(0, boneName.Length - 5);
                if (boneName == "Armature") continue;

                List<float> sourceInput = new List<float>(); // time interval
                List<Matrix4x4f> sourceOutput = new List<Matrix4x4f>();
                List<string> interpolationInput = new List<string>();

                XmlNode channel = boneAnimation["channel"];
                string channelName = channel.Attributes["source"].Value;

                XmlNode sampler = boneAnimation["sampler"];
                if (channelName != "#" + sampler.Attributes["id"].Value) continue;

                string inputName = "";
                string outputName = "";
                string interpolationName = "";

                // semantic의 Name을 읽어옴.
                foreach (XmlNode input in sampler.ChildNodes)
                {
                    if (input.Attributes["semantic"].Value == "INPUT") inputName = input.Attributes["source"].Value;
                    if (input.Attributes["semantic"].Value == "OUTPUT") outputName = input.Attributes["source"].Value;
                    if (input.Attributes["semantic"].Value == "INTERPOLATION") interpolationName = input.Attributes["source"].Value;
                }

                // bone의 애니메이션 소스를 읽어온다.
                foreach (XmlNode source in boneAnimation.ChildNodes)
                {
                    if (source.Name == "source")
                    {
                        string sourcesId = source.Attributes["id"].Value;
                        if ("#" + sourcesId == inputName)
                        {
                            string[] value = source["float_array"].InnerText.Trim().Replace("\n", " ").Split(' ');
                            float[] items = new float[value.Length];
                            for (int i = 0; i < value.Length; i++)
                            {
                                items[i] = float.Parse(value[i].Trim());
                                maxTimeLength = Math.Max(items[i], maxTimeLength);
                            }
                            sourceInput.AddRange(items);
                        }

                        if ("#" + sourcesId == outputName)
                        {
                            string[] value = source["float_array"].InnerText.Trim().Replace("\n", " ").Split(' ');
                            float[] items = new float[value.Length];
                            for (int i = 0; i < value.Length; i++) items[i] = float.Parse(value[i]);
                            for (int i = 0; i < value.Length; i += 16)
                            {
                                List<float> mat = new List<float>();
                                for (int j = 0; j < 16; j++) mat.Add(items[i + j]);
                                Matrix4x4f matrix = new Matrix4x4f(mat.ToArray());

                                sourceOutput.Add(matrix.Transposed);
                            }
                        }

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

                ani.Add(boneName, keyframe);
            }

            // *** [중요] 바닥으로부터 엉덩이 위치를 맞추기 위하여 hipHeightScale을 구한다.
            // Interpolation Pose만 0초에서 정상적 T-pose를 취하고 있어서 이 부분에서 가져와야 한다.
            if (motionName == "a-T-Pose") //Interpolation Pose
            {
                aniDae.HipHeightScale = LoadDefaultScaleTransform(ani, aniDae.DicBones);
                Console.WriteLine($"XmeDae HipScaled={aniDae.HipHeightScale}");
            }

            Motion motion = new Motion(motionName, maxTimeLength);
            if (maxTimeLength > 0 && aniDae.DicBones != null)
            {
                // 뼈마다 순회
                foreach (KeyValuePair<string, Dictionary<float, Matrix4x4f>> item in ani)
                {
                    string boneName = item.Key;
                    Dictionary<float, Matrix4x4f> source = item.Value;

                    Bone bone = aniDae.GetBoneByName(boneName);
                    if (bone == null) continue;

                    // 뼈의 시간에 따른 순회
                    foreach (KeyValuePair<float, Matrix4x4f> subsource in source)
                    {
                        float time = subsource.Key;
                        Matrix4x4f mat = subsource.Value;
                        motion.AddKeyFrame(time);

                        Vertex3f position = bone.IsRootArmature ?
                            mat.Position * aniDae.HipHeightScale : bone.PivotPosition;

                        ZetaExt.Quaternion q = AniXmlLoader.ToQuaternion(mat);
                        q.Normalize();

                        BonePose bonePose = new BonePose(position, q);
                        motion[time].AddBoneTransform(boneName, bonePose);
                    }
                }
            }

            return motion;
        }

        public static void LibraryAnimations(AniDae xmlDae, XmlDocument xml)
        {
            XmlNodeList libraryAnimations = xml.GetElementsByTagName("library_animations");
            if (libraryAnimations.Count == 0)
            {
                Console.WriteLine($"현재 캐릭터 파일에 library_animations 노드가 없습니다.");
                return;
            }

            Dictionary<string, Dictionary<float, Matrix4x4f>> ani = new Dictionary<string, Dictionary<float, Matrix4x4f>>();
            foreach (XmlNode libraryAnimation in libraryAnimations[0])
            {
                string animationName = libraryAnimation.Attributes["name"].Value;
                float maxTimeLength = 0.0f;
                string motionName = "";

                // bone마다 순회
                foreach (XmlNode boneAnimation in libraryAnimation.ChildNodes)
                {
                    string boneName = boneAnimation.Attributes["id"].Value.Substring(animationName.Length + 1);
                    int fIdx = boneName.IndexOf("_");
                    motionName = (fIdx >= 0) ? boneName.Substring(0, fIdx) : "";
                    boneName = (fIdx >= 0) ? boneName.Substring(fIdx + 1) : boneName;
                    boneName = boneName.Replace("_pose_matrix", "");
                    List<float> sourceInput = new List<float>(); // time interval
                    List<Matrix4x4f> sourceOutput = new List<Matrix4x4f>();
                    List<string> interpolationInput = new List<string>();

                    XmlNode channel = boneAnimation["channel"];
                    string channelName = channel.Attributes["source"].Value;

                    XmlNode sampler = boneAnimation["sampler"];
                    if (channelName != "#" + sampler.Attributes["id"].Value) continue;

                    string inputName = "";
                    string outputName = "";
                    string interpolationName = "";

                    // semantic의 Name을 읽어옴.
                    foreach (XmlNode input in sampler.ChildNodes)
                    {
                        if (input.Attributes["semantic"].Value == "INPUT") inputName = input.Attributes["source"].Value;
                        if (input.Attributes["semantic"].Value == "OUTPUT") outputName = input.Attributes["source"].Value;
                        if (input.Attributes["semantic"].Value == "INTERPOLATION") interpolationName = input.Attributes["source"].Value;
                    }

                    // bone의 애니메이션 소스를 읽어온다.
                    foreach (XmlNode source in boneAnimation.ChildNodes)
                    {
                        if (source.Name == "source")
                        {
                            string sourcesId = source.Attributes["id"].Value;
                            if ("#" + sourcesId == inputName)
                            {
                                string[] value = source["float_array"].InnerText.Split(' ');
                                float[] items = new float[value.Length];
                                for (int i = 0; i < value.Length; i++)
                                {
                                    items[i] = float.Parse(value[i]);
                                    maxTimeLength = Math.Max(items[i], maxTimeLength);
                                }
                                sourceInput.AddRange(items);
                            }

                            if ("#" + sourcesId == outputName)
                            {
                                string[] value = source["float_array"].InnerText.Split(' ');
                                float[] items = new float[value.Length];
                                for (int i = 0; i < value.Length; i++) items[i] = float.Parse(value[i]);
                                for (int i = 0; i < value.Length; i += 16)
                                {
                                    List<float> mat = new List<float>();
                                    for (int j = 0; j < 16; j++) mat.Add(items[i + j]);
                                    Matrix4x4f matrix = new Matrix4x4f(mat.ToArray());
                                    sourceOutput.Add(matrix.Transposed);
                                }
                            }

                            if ("#" + sourcesId == interpolationName)
                            {
                                string[] value = source["Name_array"].InnerText.Split(' ');
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

                    ani.Add(boneName, keyframe);
                }

                Motion motion = new Motion(motionName, maxTimeLength);
                if (maxTimeLength > 0)
                {
                    foreach (KeyValuePair<string, Dictionary<float, Matrix4x4f>> item in ani)
                    {
                        string boneName = item.Key;
                        Dictionary<float, Matrix4x4f> source = item.Value;
                        foreach (KeyValuePair<float, Matrix4x4f> subsource in source)
                        {
                            float time = subsource.Key;
                            Matrix4x4f mat = subsource.Value;
                            motion.AddKeyFrame(time);

                            ZetaExt.Quaternion q = ToQuaternion(mat);
                            q.Normalize();
                            BonePose bonePose = new BonePose();
                            bonePose.Position = new Vertex3f(mat[3, 0], mat[3, 1], mat[3, 2]);
                            bonePose.Rotation = q;
                            motion[time].AddBoneTransform(boneName, bonePose);
                        }
                    }
                }

                xmlDae.Motions.AddMotion(motion);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static Bone LibraryVisualScenes(XmlDocument xml, Dictionary<string, Matrix4x4f> invBindPoses,
            Dictionary<string, int> dicBoneIndex,
            out Dictionary<string, Bone> dicBones)
        {
            XmlNodeList library_visual_scenes = xml.GetElementsByTagName("library_visual_scenes");
            dicBones = new Dictionary<string, Bone>();

            if (library_visual_scenes.Count == 0)
            {
                Console.WriteLine($"[에러] dae파일구조에서 뼈대구조를 읽어올 수 없습니다.");
                return null;
            }

            Stack<XmlNode> nStack = new Stack<XmlNode>();
            Stack<Bone> bStack = new Stack<Bone>();
            XmlNode nodes = library_visual_scenes[0]["visual_scene"];
            XmlNode rootNode = null;

            // Find Root Node
            foreach (XmlNode item in nodes)
                if (item.Attributes["id"].Value == "Armature") rootNode = item;
            if (rootNode == null) return null;

            nStack.Push(rootNode);
            Bone rootBone = new Bone("Armature", 0);
            bStack.Push(rootBone);

            while (nStack.Count > 0)
            {
                XmlNode node = nStack.Pop();
                Bone bone = bStack.Pop();

                // 행렬읽기
                string[] value = node["matrix"].InnerText.Split(' ');
                float[] items = new float[value.Length];
                for (int i = 0; i < value.Length; i++) items[i] = float.Parse(value[i]);
                Matrix4x4f mat = new Matrix4x4f(items).Transposed;

                string boneName = node.Attributes["sid"]?.Value;

                if (boneName != null)
                    dicBones.Add(boneName, bone);

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


        /// <summary>
        /// Quaternions for Computer Graphics by John Vince. p199 참고
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        private static ZetaExt.Quaternion ToQuaternion(Matrix4x4f mat)
        {
            ZetaExt.Quaternion q = ZetaExt.Quaternion.Identity;
            float a11 = mat[0, 0];
            float a12 = mat[1, 0];
            float a13 = mat[2, 0];

            float a21 = mat[0, 1];
            float a22 = mat[1, 1];
            float a23 = mat[2, 1];

            float a31 = mat[0, 2];
            float a32 = mat[1, 2];
            float a33 = mat[2, 2];

            float trace = a11 + a22 + a33;
            if (trace >= -1)
            {
                // I changed M_EPSILON to 0
                float s = 0.5f / (float)Math.Sqrt(trace + 1.0f);
                q.W = 0.25f / s;
                q.X = (a32 - a23) * s;
                q.Y = (a13 - a31) * s;
                q.Z = (a21 - a12) * s;
            }
            else
            {
                if (1 + a11 - a22 - a33 >= 0)
                {
                    float s = 2.0f * (float)Math.Sqrt(1.0f + a11 - a22 - a33);
                    q.X = 0.25f * s;
                    q.Y = (a12 + a21) / s;
                    q.Z = (a13 + a31) / s;
                    q.W = (a32 - a23) / s;
                }
                else if (1 - a11 + a22 - a33 >= 0)
                {
                    float s = 2.0f * (float)Math.Sqrt(1 - a11 + a22 - a33);
                    q.Y = 0.25f * s;
                    q.X = (a12 + a21) / s;
                    q.Z = (a23 + a32) / s;
                    q.W = (a13 - a31) / s;
                }
                else
                {
                    float s = 2.0f * (float)Math.Sqrt(1 - a11 - a22 + a33);
                    q.Z = 0.25f * s;
                    q.X = (a13 + a31) / s;
                    q.Y = (a23 + a32) / s;
                    q.W = (a21 - a12) / s;
                }
            }
            return q;
        }
    }
}
