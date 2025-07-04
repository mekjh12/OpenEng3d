using Model3d;
using OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using ZetaExt;

namespace Animate
{
    public class AniRig
    {
        string _filename;
        readonly string _name;

        MotionStorage _motions;
        List<TexturedModel> _texturedModels;
        Armature _armature;

        public Dictionary<string, Bone> DicBones => _armature.DicBones;

        //TexturedModel _nudeBodyTexturedModel; // 모델의 나체를 지정한다.


        Vertex3f _lowerCollider = new Vertex3f(-0.3f, -0.3f, 0.0f);
        Vertex3f _upperCollider = new Vertex3f(0.3f, 0.3f, 1.8f);

        public Armature Armature => _armature;

        public Vertex3f LowerCollider => _lowerCollider;

        public Vertex3f UpperCollider => _upperCollider;

        Matrix4x4f _bindShapeMatrix;


        public MotionStorage Motions => _motions;

        //public Bone RootBone => _rootBone;

        public int BoneCount => _armature.BoneNames.Length;

        public List<TexturedModel> Models => _texturedModels;

        //public TexturedModel BodyWeightModels => _nudeBodyTexturedModel;

        public string Name => _name;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="filename"></param>
        public AniRig(string filename, bool isLoadAnimation = true)
        {
            _filename = filename;
            _name = Path.GetFileNameWithoutExtension(filename);

            // 뼈대 정보를 초기화한다.
            _armature = new Armature();

            // 모델 정보를 가져온다.
            List<TexturedModel> models = LoadFile(filename);
            //_nudeBodyTexturedModel = models[0];
            if (_texturedModels == null)
                _texturedModels = new List<TexturedModel>();
            _texturedModels.AddRange(models);

            // 모션 스토리지를 초기화한다.
            _motions = new MotionStorage();
            _motions.AddMotion(new Motion("default", lengthInSeconds: 2.0f));
        }

        public List<TexturedModel> WearCloth(string fileName, float expandValue = 0.00005f)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(fileName);
            _filename = fileName;

            // (1) library_images = textures
            Dictionary<string, Texture> textures = AniXmlLoader.LibraryImages(_filename, xml);
            Dictionary<string, string> materialToEffect = AniXmlLoader.LoadMaterials(xml);
            Dictionary<string, string> effectToImage = AniXmlLoader.LoadEffect(xml);

            // (2) library_geometries = position, normal, texcoord, color
            List<MeshTriangles> meshes = AniXmlLoader.LibraryGeometris(xml,
                out List<Vertex3f> lstPositions, out List<Vertex2f> lstTexCoord, out List<Vertex3f> lstNormals);

            // (3) library_controllers = boneIndex, boneWeight, bindShapeMatrix
            AniXmlLoader.LibraryController(xml, 
                out List<string> clothBoneNames, 
                out Dictionary<string, Matrix4x4f> invBindPoses,
                out List<BoneWeightVector4> vertexBoneData, 
                out Matrix4x4f bindShapeMatrix);
            _bindShapeMatrix = bindShapeMatrix;

            // (4-1) boneName, boneIndexDictionary
            Dictionary<int, int> map = new Dictionary<int, int>();
            for (int i = 0; i < clothBoneNames.Count; i++)
            {
                string clothBoneName = clothBoneNames[i].Trim();
                if (_armature.IsExistBone(clothBoneName))
                {
                    map.Add(i, _armature.GetBoneIndex(clothBoneName));
                }
                else
                {
                    Console.WriteLine($"현재 뼈대에 매칭되는 본({clothBoneName})이 없습니다. ");
                }
            }

            // (4-2) bone-index modify.
            for (int i = 0; i < vertexBoneData.Count; i++)
            {
                var current = vertexBoneData[i];
                var newIndices = new Vertex4i(
                    map[current.BoneIndices.x],
                    map[current.BoneIndices.y],
                    map[current.BoneIndices.z],
                    map[current.BoneIndices.w]
                );
                vertexBoneData[i] = current.WithBoneIndices(newIndices);
            }

            // (5) source positions으로부터 
            Matrix4x4f A0 = _armature.RootBone.LocalBindTransform;
            Matrix4x4f S = _bindShapeMatrix;
            Matrix4x4f A0xS = A0 * S;
            for (int i = 0; i < lstPositions.Count; i++)
            {
                lstPositions[i] = A0xS.Multiply(lstPositions[i]);
            }

            // (6) 읽어온 정보의 인덱스를 이용하여 배열을 만든다.
            List<TexturedModel> texturedModels = new List<TexturedModel>();
            foreach (MeshTriangles meshTriangles in meshes)
            {
                RawModel3d _rawModel = Clothes.Expand(lstPositions, lstTexCoord,
                    vertexBoneData, meshTriangles, expandValue);

                string effect = materialToEffect[meshTriangles.Material].Replace("#", "");
                string imageName = (effectToImage[effect]);

                if (textures.ContainsKey(imageName))
                {
                    TexturedModel texturedModel = new TexturedModel(_rawModel, textures[imageName]);
                    texturedModel.IsDrawElement = _rawModel.IsDrawElement;
                    texturedModel.VertexCount = _rawModel.VertexCount;
                    texturedModels.Add(texturedModel);
                }
            }

            return texturedModels;
        }

        public void AddMotion(Motion motion)
        {
            Motions.AddMotion(motion);
        }

        /// <summary>
        /// 뼈대를 추가한다.
        /// </summary>
        /// <param name="boneName">추가할 뼈대 이름</param>
        /// <param name="boneIndex">추가할 뼈대 인덱스</param>
        /// <param name="parentBoneName">붙일 뼈대 이름</param>
        /// <param name="inverseBindPoseTransform">캐릭터 공간의 바인딩행렬의 역행렬을 지정한다.</param>
        /// <param name="localBindTransform">부모 뼈공간에서의 바인딩 행렬을 지정한다.</param>
        /// <returns></returns>
        public Bone AddBone(string boneName, int boneIndex, 
            string parentBoneName, 
            Matrix4x4f inverseBindPoseTransform,
            Matrix4x4f localBindTransform)
        {
            // 뼈대 이름이 이미 존재하는지 확인한다.
            if (_armature.IsExistBone(boneName))
            {
                throw new Exception($"뼈대 이름({boneName})이 이미 존재합니다.");
            }

            // 뼈대 인덱스가 이미 존재하는지 확인한다.
            if (_armature.IsExistBoneIndex(boneName))
            {
                throw new Exception($"뼈대 인덱스({boneIndex})가 이미 존재합니다.");
            }

            // 부모 뼈대를 찾는다.
            if (!_armature.IsExistBoneIndex(parentBoneName))
            {
                throw new Exception($"부모 뼈대 이름({parentBoneName})이 존재하지 않습니다.");
            }

            // 부모 뼈대를 찾고, 새로운 뼈대를 생성한다.
            Bone parentBone = _armature.GetBoneByName(parentBoneName);
            Bone cBone = new Bone(boneName, boneIndex);
            parentBone.AddChild(cBone);
            cBone.Parent = parentBone;
            cBone.LocalBindTransform = localBindTransform;
            cBone.InverseBindPoseTransform = inverseBindPoseTransform;
            _armature.AddBone(boneName, cBone);

            return cBone;
        }

        public List<TexturedModel> LoadFile(string filename)
        {
            // dae 파일을 읽어온다.
            XmlDocument xml = new XmlDocument();
            xml.Load(filename);
            _filename = filename;

            // 텍스처, 재질, 이미지효과 정보를 읽어온다.
            Dictionary<string, Texture> textures = AniXmlLoader.LibraryImages(_filename, xml);
            Dictionary<string, string> materialToEffect = AniXmlLoader.LoadMaterials(xml);
            Dictionary<string, string> effectToImage = AniXmlLoader.LoadEffect(xml);

            // 지오메트리 정보를 읽어온다. position, normal, texcoord, color, MeshTriangles 정보가 포함되어 있다.
            List<MeshTriangles> meshes = AniXmlLoader.LibraryGeometris(xml, 
                out List<Vertex3f> lstPositions,
                out List<Vertex2f> lstTexCoord, 
                out List<Vertex3f> lstNormals);

            // 컨트롤러 정보, 역바인딩포즈를 읽어온다.
            AniXmlLoader.LibraryController(xml, 
                out List<string> boneNames, 
                out Dictionary<string, Matrix4x4f> invBindPoses,
                out List<BoneWeightVector4> vertexBoneData,
                out Matrix4x4f bindShapeMatrix);

            // 정점과 정점 컨트롤 데이터의 갯수가 일치하는지 확인한다.
            if (lstPositions.Count != vertexBoneData.Count)
                Console.WriteLine("[주의] 지오메트리의 정점의 갯수와 정점을 컨트롤할 데이터의 갯수가 다릅니다.");

            // 모델의 bind shape matrix을 읽어온다.
            _bindShapeMatrix = bindShapeMatrix;

            // 뼈대명 배열을 만들고 뼈대 인덱스 딕셔너리를 만든다.
            _armature.SetBoneNames(boneNames.ToArray());

            // (4) library_animations = 애니메이션 정보
            AniXmlLoader.LibraryAnimations(this, xml);

            // (5) library_visual_scenes = bone hierarchy + rootBone
            Bone rootBone = AniXmlLoader.LibraryVisualScenes(xml, invBindPoses, ref _armature);
            _armature.SetRootBone(rootBone); 

            // (6) source positions으로부터 
            Matrix4x4f A0 = _armature.RootBone.LocalBindTransform;
            Matrix4x4f S = _bindShapeMatrix;
            Matrix4x4f A0xS = A0 * S;
            for (int i = 0; i < lstPositions.Count; i++)
            {
                lstPositions[i] = A0xS.Multiply(lstPositions[i]);
            }

            // 역바인딩포즈를 계산한다. 
            // LibraryController에서 미리 계산된 역바인딩포즈가 있지만, 
            // 애니메이션을 적용하기 위해서 다시 계산한다.
            Stack<Bone> bStack = new Stack<Bone>();
            bStack.Push(_armature.RootBone);
            while (bStack.Count > 0)
            {
                // 뼈대를 꺼내고, 
                Bone cBone = bStack.Pop();

                // 부모 뼈대의 애니메이션 바인드 포즈를 계산한다.
                Matrix4x4f prevAnimatedMat = (cBone.Parent == null ? Matrix4x4f.Identity : cBone.Parent.AnimatedBindPoseTransform);

                // 현재 뼈대의 애니메이션 바인드 포즈를 계산한다.
                cBone.AnimatedBindPoseTransform = prevAnimatedMat * cBone.LocalBindTransform;

                // 역바인딩포즈를 계산한다.
                cBone.InverseBindPoseTransform = cBone.AnimatedBindPoseTransform.Inversed();

                // 자식 뼈대를 스택에 추가한다.
                foreach (Bone child in cBone.Childrens) bStack.Push(child);
            }

            // lstPositions, lstTexCoord, lstNormals, lstBoneIndex, lstBoneWeight를 이용하여 RawModel3d를 생성한다.
            // 읽어온 정보의 MeshTriangles를 이용하여 GPU에 폴리곤 정보 데이터를 전송한다.
            List<TexturedModel> texturedModels = new List<TexturedModel>();
            foreach (MeshTriangles meshTriangles in meshes)
            {
                // 로딩한 postions, boneIndices, boneWeights를 버텍스로
                List<Vertex3f> _vertices = new List<Vertex3f>();
                List<Vertex2f> _texcoords = new List<Vertex2f>();
                List<Vertex3f> _normals = new List<Vertex3f>();
                List<Vertex4i> _boneIndices = new List<Vertex4i>();
                List<Vertex4f> _boneWeights = new List<Vertex4f>();

                for (int i = 0; i < meshTriangles.Vertices.Count; i++)
                    _vertices.Add(lstPositions[(int)meshTriangles.Vertices[i]]);

                for (int i = 0; i < meshTriangles.Texcoords.Count; i++)
                    _texcoords.Add(lstTexCoord[(int)meshTriangles.Texcoords[i]]);

                for (int i = 0; i < meshTriangles.Normals.Count; i++)
                    _normals.Add(lstNormals[(int)meshTriangles.Normals[i]]);

                for (int i = 0; i < meshTriangles.Vertices.Count; i++)
                    _boneIndices.Add(vertexBoneData[(int)meshTriangles.Vertices[i]].BoneIndices);

                for (int i = 0; i < meshTriangles.Vertices.Count; i++)
                    _boneWeights.Add(vertexBoneData[(int)meshTriangles.Vertices[i]].BoneWeights);

                // GPU에 전송할 모델을 생성한다.
                RawModel3d _rawModel = new RawModel3d();
                _rawModel.Init(vertices: _vertices.ToArray(), texCoords: _texcoords.ToArray(), normals: _normals.ToArray(),
                    boneIndex: _boneIndices.ToArray(), boneWeight: _boneWeights.ToArray());
                _rawModel.GpuBind();

                // 텍스쳐를 매핑한다.
                string effect = materialToEffect[meshTriangles.Material].Replace("#", "");
                string imageName = (effectToImage[effect]);

                // 텍스쳐가 존재하는지 확인하고 텍스처 모델을 생성한다.
                if (textures.ContainsKey(imageName))
                {
                    TexturedModel texturedModel = new TexturedModel(_rawModel, textures[imageName]);
                    texturedModel.IsDrawElement = false;
                    texturedModels.Add(texturedModel);
                }
            }

            return texturedModels;
        }
    }
}
