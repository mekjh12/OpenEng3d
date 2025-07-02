using Assimp;
using Model3d;
using OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Animate
{
    class ModelDae
    {
        private RawModel3d _modelTextured;
        private string _directory;
        private Bone _rootJoint;
        private Matrix4x4f _rootBindPoseMatrix;

        public Matrix4x4f RootBindPoseMatrix => _rootBindPoseMatrix;

        /// <summary>
        /// BoneIndex -> 뼈공간의 정점을 캐릭터공간의 정점으로 변환하는 행렬
        /// </summary>
        Dictionary<int, Matrix4x4f> _boneOffsetMatrices = new Dictionary<int, Matrix4x4f>();

        /// <summary>
        /// BoneName -> BoneIndex
        /// </summary>
        Dictionary<string, int>_jointIds = new Dictionary<string, int>();

        /// <summary>
        /// ChildIndex -> ParentIndex
        /// </summary>
        Dictionary<int, int> _skeletonHierarchy = new Dictionary<int, int>();
        Motion _animation;
        Matrix4x4f _rootMatrix4x4;

        public Matrix4x4f RootMatrix4x4 => _rootMatrix4x4;

        struct BoneWeight
        {
            public int JointId;
            public float Weight;
        }

        public Bone RootBone => _rootJoint;

        public Motion Animation => _animation;

        public int BoneCount => _boneOffsetMatrices.Count;

        public RawModel3d Model => _modelTextured;

        public ModelDae(string filename)
        {
            string extension = Path.GetExtension(filename);

            if (extension == ".dae")
            {
                LoadModel(filename);
            }
            else if (extension == ".obj")
            {
                LoadModelObj(filename);
            }
        }

        private void LoadModelObj(string filename)
        {
            Assimp.Scene scene;
            Assimp.AssimpContext importer = new Assimp.AssimpContext();
            importer.SetConfig(new Assimp.Configs.NormalSmoothingAngleConfig(66.0f));
            scene = importer.ImportFile(filename, Assimp.PostProcessSteps.Triangulate |
                                                    Assimp.PostProcessSteps.FlipUVs);

            if (scene == null || scene.RootNode == null)
            {
                Console.WriteLine("ERROR::ASSIMP::");
                return;
            }

            _directory = Path.GetDirectoryName(filename);

            // 모델을 로드한다. stack을 변환요망~~~~~~~~~~
            processNodeObj(scene.RootNode, scene);
        }

        private void LoadModel(string filename)
        {
            Assimp.Scene scene;
            Assimp.AssimpContext importer = new Assimp.AssimpContext();
            importer.SetConfig(new Assimp.Configs.NormalSmoothingAngleConfig(66.0f));
            scene = importer.ImportFile(filename, PostProcessSteps.Triangulate | PostProcessSteps.FlipUVs);

            if (scene == null || scene.RootNode == null)
            {
                Console.WriteLine("ERROR::ASSIMP::");
                return;
            }

            // bind_shape_matrix
            Matrix4x4f matrix = Matrix4x4f.Identity;
            XmlDocument xml = new XmlDocument();
            xml.Load(filename);
            XmlNodeList xmlNodeList = xml.GetElementsByTagName("bind_shape_matrix");
            foreach (XmlNode item in xmlNodeList)
            {
                string[] cols = item.InnerText.Split(new char[] { ' ' });
                float[] m = new float[16];
                for (int i = 0; i < cols.Length; i++)
                {
                    m[i] = float.Parse(cols[i]);
                }
                matrix = new Matrix4x4f();
                matrix[0, 0] = m[0];
                matrix[1, 0] = m[1];
                matrix[2, 0] = m[2];
                matrix[3, 0] = m[3];
                matrix[0, 1] = m[4];
                matrix[1, 1] = m[5];
                matrix[2, 1] = m[6];
                matrix[3, 1] = m[7];
                matrix[0, 2] = m[8];
                matrix[1, 2] = m[9];
                matrix[2, 2] = m[10];
                matrix[3, 2] = m[11];
                matrix[0, 3] = m[12];
                matrix[1, 3] = m[13];
                matrix[2, 3] = m[14];
                matrix[3, 3] = m[15];
            }
            _rootBindPoseMatrix = matrix;

            _directory = Path.GetDirectoryName(filename);

            // (1) 모델을 로드한다.
            //   1-1. position, texcoord, normal, boneIndex, boneWeight
            //   1-2. Faces Index
            //   1-3. boneOffsetMatrices을 로드한다.
            //   1-4. jointIds를 로드한다.
            Stack<Node> stack = new Stack<Node>();
            stack.Push(scene.RootNode);
            _rootMatrix4x4 = scene.RootNode.Transform.ToMatrix4x4f();
            _rootMatrix4x4 = matrix;

            while (stack.Count > 0)
            {
                Node node = stack.Pop();
                for (int i = 0; i < node.MeshCount; i++)
                {
                    Mesh mesh = scene.Meshes[node.MeshIndices[i]];
                    _modelTextured = LoadMesh(mesh, scene);
                }

                for (int i = 0; i < node.ChildCount; i++) stack.Push(node.Children[i]);
            }

            // (2) 모델의 계층구조를 로드한다.
            stack = new Stack<Node>();
            stack.Push(scene.RootNode);
            while (stack.Count > 0)
            {
                Node node = stack.Pop();

                // BoneName으로부터 boneIndex, parentIndex를 가져온다. 존재하지 않으면 -1이다.
                int boneIndex = _jointIds.ContainsKey(node.Name) ? _jointIds[node.Name] : -1;
                int parentIndex = (node.Parent != null) ?
                    _jointIds.ContainsKey(node.Parent.Name) ? _jointIds[node.Parent.Name] : -1 : -1;

                if (boneIndex >= 0)
                    _skeletonHierarchy[boneIndex] = parentIndex;

                for (int i = 0; i < node.ChildCount; i++) stack.Push(node.Children[i]);
            }

            _skeletonHierarchy[0] = -1;

            // (3) 루트본으로부터 계층적 조인트를 읽어온다.
            Stack<int> jStack = new Stack<int>();
            Dictionary<int, Bone> dicJoint = new Dictionary<int, Bone>();
            jStack.Push(0);
            while (jStack.Count > 0)
            {
                int jointId = jStack.Pop();
                string jointName = ""; // joint name을 얻는다.
                foreach (KeyValuePair<string, int> item in _jointIds)
                    if (item.Value == jointId) { jointName = item.Key; break; }
                int parentId = _skeletonHierarchy[jointId];

                Bone joint = new Bone(jointName, jointId, _boneOffsetMatrices[jointId].Inverse);

                dicJoint.Add(jointId, joint);

                if (dicJoint.ContainsKey(parentId))
                {
                    dicJoint[parentId].AddChild(joint);
                }
                else
                {
                    _rootJoint = joint;
                }

                List<int> childs = new List<int>(); // add childs collect.
                foreach (KeyValuePair<int, int> item in _skeletonHierarchy)
                {
                    if (item.Value == jointId) childs.Add(item.Key);
                }
                for (int i = 0; i < childs.Count; i++) jStack.Push(childs[i]); // input stack
            }

            // (4) 조인트의 바인딩 행렬을 읽어온다.
            Stack<Bone> j = new Stack<Bone>();
            j.Push(_rootJoint);
            while (j.Count > 0)
            {
                Bone joint = j.Pop();
                joint.InverseBindTransform = _boneOffsetMatrices[joint.Index];

                Matrix4x4f Mc = joint.InverseBindTransform;
                int parentIndex = _skeletonHierarchy[joint.Index];
                Matrix4x4f Mp = parentIndex < 0 ? Matrix4x4f.Identity : _boneOffsetMatrices[parentIndex];
                joint.BindTransform = Mp * Mc.Inverse;

                for (int i = 0; i < joint.Childrens.Count; i++) j.Push(joint.Childrens[i]);
            }

            // (5) 애니메이션들을 로딩한다.
            List<Assimp.Animation> animations = scene.Animations;
            foreach (Assimp.Animation ani in animations)
            {
                // 애니메이션을 로드한다.
                string animationName = ani.Name;
                _animation = new Motion(animationName, (float)ani.DurationInTicks);

                // 시간키를 모두 읽어와 keyframe을 만든다. 
                if (ani.HasNodeAnimations)
                {
                    foreach (VectorKey item in ani.NodeAnimationChannels[0].PositionKeys)
                    {
                        _animation.AddKeyFrame((float)item.Time);
                    }
                }

                //  애니메이션이 적용된 joint마다 순회한다.
                foreach (NodeAnimationChannel nodeAnimationChannel in ani.NodeAnimationChannels)
                {
                    // joint의 시간에 따라 순회한다.
                    string jointName = nodeAnimationChannel.NodeName;
                    for (int i = 0; i < nodeAnimationChannel.PositionKeyCount; i++)
                    {
                        float time = (float)nodeAnimationChannel.PositionKeys[i].Time;
                        Assimp.Quaternion q = nodeAnimationChannel.RotationKeys[i].Value;
                        Quaternion quaternion = new Quaternion(q.X, q.Y, q.Z, q.W);
                        quaternion.Normalize();
                        Vector3D position = nodeAnimationChannel.PositionKeys[i].Value;
                        Vector3D scale = nodeAnimationChannel.ScalingKeys[i].Value;
                        BonePose jointTransform = new BonePose(position, quaternion, scale);
                        _animation[time][jointName] = jointTransform;
                    }
                }
            }
        }        

        private Dictionary<TextureType, List<Texture>> LoadMaterials(Assimp.Scene scene, Mesh mesh)
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

            List<Texture> heightMaps = LoadMaterialTextures(material, TextureType.Height);
            textures[TextureType.Height] = heightMaps;

            return textures;
        }


        private List<Texture> LoadMaterialTextures(Assimp.Material mat, Assimp.TextureType typeName)
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
                        Texture texture = new Texture(filename);
                        textures.Add(texture);
                        TextureStorage.TexturesLoaded.Add(filename, texture);
                    }
                    else
                    {
                        Console.WriteLine($"텍스처 파일이 누락되었습니다. {filename}");
                    }
                }
            }

            return textures;
        }

        private TexturedModel LoadMesh(Assimp.Mesh mesh, Assimp.Scene scene)
        {
            float[] positions = null;
            float[] normals = null;
            float[] texCoords = null;
            float[] weights = null;
            float[] bones = null;

            Console.WriteLine($"* Mesh 정보 {mesh.Name} mesh count={mesh.VertexCount}");

            // 모델의 텍스쳐를 읽어온다.
            Dictionary<TextureType, List<Texture>> textures = LoadMaterials(scene, mesh);

            // Bone Index와 Weight를 읽는다.
            Dictionary<int, List<BoneWeight>> weightList = new Dictionary<int, List<BoneWeight>>();
            if (mesh.HasBones)
                LoadBoneWeight(mesh, ref weightList, ref _jointIds);

            // Vertices를 읽는다.
            if (mesh.HasVertices)
                LoadVertices(mesh, ref weightList, ref positions, ref weights, ref bones);

            // Normal을 읽는다.
            if (mesh.HasNormals)
                LoadNormals(mesh, ref normals);

            // TextureCoords를 읽는다.
            if (mesh.HasTextureCoords(0))
                LoadTexCoords(mesh, ref texCoords);

            // Faces Index를 읽는다.
            List<uint> indices = LoadFaceIndices(mesh);

            // VAO, VBO 모델을 만든다.
            // raw3d 모델을 만든다.
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            GpuLoader.StoreDataInAttributeList(0, 3, positions, BufferUsage.StaticDraw);
            GpuLoader.StoreDataInAttributeList(1, 2, texCoords, BufferUsage.StaticDraw);
            GpuLoader.StoreDataInAttributeList(2, 3, normals, BufferUsage.StaticDraw);
            GpuLoader.StoreDataInAttributeList(3, 3, bones, BufferUsage.StaticDraw);
            GpuLoader.StoreDataInAttributeList(4, 3, weights, BufferUsage.StaticDraw);
            //GpuLoader.BindIndicesBuffer(indices.ToArray());
            Gl.BindVertexArray(0);
            RawModel3d rawModel = new RawModel3d(vao, positions);
            rawModel.IsDrawElement = false;

            return new TexturedModel(rawModel, textures[TextureType.Diffuse][0]);
        }


        private void processNodeObj(Node node, Assimp.Scene scene)
        {
            Console.WriteLine($"Node 정보 {node.Name} mesh count={node.MeshCount}");

            // process all the node's meshes (if any)
            for (int i = 0; i < node.MeshCount; i++)
            {
                Assimp.Mesh mesh = scene.Meshes[node.MeshIndices[i]];
                //_modelTextured = LoadMeshObj(mesh, scene);
            }

            for (int i = 0; i < node.ChildCount; i++)
                processNodeObj(node.Children[i], scene); // 자식노드 재귀호출
        }

        private void LoadVertices(Mesh mesh, ref Dictionary<int, List<BoneWeight>> weightList, ref float[] positions, ref float[] weights, ref float[] bones)
        {
            Vector3D[] vectors = mesh.Vertices.ToArray();
            positions = new float[vectors.Length * 3];
            weights = new float[vectors.Length * 3];
            bones = new float[vectors.Length * 3];

            for (int i = 0; i < vectors.Length * 3; i++)
            {
                weights[i] = 0.0f;
                bones[i] = 0.0f;
            }

            for (int i = 0; i < weightList.Count; i++)
            {
                positions[3 * i + 0] = vectors[i].X;
                positions[3 * i + 1] = vectors[i].Y;
                positions[3 * i + 2] = vectors[i].Z;

                BoneWeight[] wlist = weightList[i].ToArray();

                if (wlist.Length == 1)
                {
                    weights[3 * i + 0] = wlist[0].Weight;
                    bones[3 * i + 0] = wlist[0].JointId;
                }

                if (wlist.Length == 2)
                {
                    weights[3 * i + 0] = wlist[0].Weight;
                    bones[3 * i + 0] = wlist[0].JointId;
                    weights[3 * i + 1] = wlist[1].Weight;
                    bones[3 * i + 1] = wlist[1].JointId;
                }

                if (wlist.Length >= 3)
                {
                    weights[3 * i + 0] = wlist[0].Weight;
                    bones[3 * i + 0] = wlist[0].JointId;
                    weights[3 * i + 1] = wlist[1].Weight;
                    bones[3 * i + 1] = wlist[1].JointId;
                    weights[3 * i + 2] = wlist[2].Weight;
                    bones[3 * i + 2] = wlist[2].JointId;
                }

                if (weights[3 * i + 0] == 0 && weights[3 * i + 1] == 0 && weights[3 * i + 2] == 0)
                {
                    //throw new Exception("");
                }
            }
        }

        private void LoadObjVertices(Mesh mesh, ref float[] positions)
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

        private void LoadNormals(Mesh mesh, ref float[] normals)
        {
            Assimp.Vector3D[] normalList = mesh.Normals.ToArray();
            normals = new float[normalList.Length * 3];
            for (int i = 0; i < normalList.Length; i++)
            {
                normals[3 * i + 0] = normalList[i].X;
                normals[3 * i + 1] = normalList[i].Y;
                normals[3 * i + 2] = normalList[i].Z;
            }
        }

        private void LoadBoneWeight(Mesh mesh, ref Dictionary<int, List<BoneWeight>> weightList, ref Dictionary<string, int> boneID)
        {
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                weightList[i] = new List<BoneWeight>();
            }

            // Bone Name을 int로 지정하고 딕셔너리로 저장
            int index = 0;
            foreach (Assimp.Bone bone in mesh.Bones)
            {
                if (!boneID.ContainsKey(bone.Name))
                    boneID.Add(bone.Name, index);
                //boneID[bone.Name] = index;
                index++;
            }

            foreach (Assimp.Bone bone in mesh.Bones)
            {
                int boneIndex = boneID[bone.Name];
                if (!_boneOffsetMatrices.ContainsKey(boneIndex))
                    _boneOffsetMatrices.Add(boneIndex, ToMatrix4x4f(bone.OffsetMatrix));
                //_boneOffsetMatrices[boneIndex] = ToMatrix4x4f(bone.OffsetMatrix);
                foreach (VertexWeight weight in bone.VertexWeights)
                {
                    BoneWeight boneWeight;
                    boneWeight.JointId = boneIndex;
                    boneWeight.Weight = weight.Weight;
                    weightList[weight.VertexID].Add(boneWeight);
                }
            }
        }

        private void LoadTexCoords(Mesh mesh, ref float[] texCoords)
        {
            Vector3D[] texCoordList = mesh.TextureCoordinateChannels[0].ToArray();
            texCoords = new float[texCoordList.Length * 2];
            for (int i = 0; i < texCoordList.Length; i++)
            {
                texCoords[2 * i + 0] = texCoordList[i].X;
                texCoords[2 * i + 1] = texCoordList[i].Y;
            }
        }

        private List<uint> LoadFaceIndices(Mesh mesh)
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

        private Matrix4x4f ToMatrix4x4f(Matrix4x4 matrix4)
        {
            Matrix4x4f mat = new Matrix4x4f();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    mat[(uint)j, (uint)i] = matrix4[i + 1, j + 1];
                }
            }
            return mat;
        }


    }
}
