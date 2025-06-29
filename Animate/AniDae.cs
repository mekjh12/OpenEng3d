using Model3d;
using OpenGL;
using System;
using System.Collections.Generic;
using System.Xml;
using ZetaExt;

namespace Animate
{
    public class AniDae
    {
        string _filename; // dae
        MotionStorage _motions;
        List<TexturedModel> _texturedModels;
        TexturedModel _bodyTexturedModel; // 모델중에서 나체를 지정한다.
        Bone _rootBone;

        Dictionary<string, Bone> _dicBones;
        Dictionary<string, int> _dicBoneIndex;
        float _hipHeightScaled = 1.0f; // 비율을 얻는다.
        Vertex3f _lowerCollider = new Vertex3f(-0.3f, -0.3f, 0.0f);
        Vertex3f _upperCollider = new Vertex3f(0.3f, 0.3f, 1.8f);

        public Vertex3f LowerCollider => _lowerCollider;

        public Vertex3f UpperCollider => _upperCollider;

        string[] _boneNames;
        Matrix4x4f _bindShapeMatrix;

        /// <summary>
        /// 엉덩이의 지면으로부터의 상대적 높이 비율
        /// </summary>
        public float HipHeightScale
        {
            get => _hipHeightScaled;
            set => _hipHeightScaled = value;
        }

        //RawModel3d _rawModel;
        //List<Vertex3f> _vertices;
        //List<Vertex4f> _boneIndices;
        //List<Vertex4f> _boneWeights;
        //public Dictionary<string, int> DicBoneIndex => _dicBoneIndex;
        //public List<Vertex3f> Vertices// => _vertices;
        //public List<Vertex4f> BoneIndices => _boneIndices;
        //public List<Vertex4f> BoneWeights => _boneWeights;

        public Bone GetBoneByName(string boneName)
        {
            return _dicBones.ContainsKey(boneName) ? _dicBones[boneName] : null;
        }

        public MotionStorage Motions => _motions;

        public Dictionary<string, Bone> DicBones => _dicBones;

        public Bone RootBone => _rootBone;

        public int BoneCount => _boneNames.Length;

        public string[] BoneNames => _boneNames;

        public List<TexturedModel> Models => _texturedModels;

        public TexturedModel BodyWeightModels => _bodyTexturedModel;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="filename"></param>
        public AniDae(string filename, bool isLoadAnimation = true)
        {
            _filename = filename;

            List<TexturedModel> models = LoadFile(filename);
            _bodyTexturedModel = models[0];

            if (_texturedModels == null)
                _texturedModels = new List<TexturedModel>();

            _texturedModels.AddRange(models);

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
            AniXmlLoader.LibraryController(xml, out List<string> clothBoneNames, out Dictionary<string, Matrix4x4f> invBindPoses,
                out List<Vertex4i> lstBoneIndex, out List<Vertex4f> lstBoneWeight, out Matrix4x4f bindShapeMatrix);
            _bindShapeMatrix = bindShapeMatrix;

            // (4-1) boneName, boneIndexDictionary
            Dictionary<int, int> map = new Dictionary<int, int>();
            for (int i = 0; i < clothBoneNames.Count; i++)
            {
                string clothBoneName = clothBoneNames[i].Trim();
                if (_dicBoneIndex.ContainsKey(clothBoneName))
                {
                    map.Add(i, _dicBoneIndex[clothBoneName]);
                }
                else
                {
                    Console.WriteLine($"현재 뼈대에 매칭되는 본({clothBoneName})이 없습니다. ");
                }
            }

            // (4-2) bone-index modify.
            for (int i = 0; i < lstBoneIndex.Count; i++)
            {
                int bx = map[lstBoneIndex[i].x];
                int by = map[lstBoneIndex[i].y];
                int bz = map[lstBoneIndex[i].z];
                int bw = map[lstBoneIndex[i].w];
                lstBoneIndex[i] = new Vertex4i(bx, by, bz, bw);
            }

            // (5) source positions으로부터 
            Matrix4x4f A0 = _rootBone.LocalBindTransform;
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
                RawModel3d _rawModel = Clothes.Expand(lstPositions, lstTexCoord, lstBoneIndex, lstBoneWeight, meshTriangles, expandValue);

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

        public Bone AddBone(string boneName, int boneIndex, string parentBoneName, Matrix4x4f inverseBindTransform,
            Matrix4x4f localBindTransform)
        {
            Bone parentBone = GetBoneByName(parentBoneName);
            Bone cBone = new Bone(boneName, boneIndex);
            parentBone.AddChild(cBone);
            cBone.Parent = parentBone;
            cBone.LocalBindTransform = localBindTransform;
            cBone.InverseBindTransform = inverseBindTransform;
            _dicBones[boneName] = cBone;

            // 배열이므로 boneNames 리스트를 다시 선언하여 저장한다.
            List<string> boneNameList = new List<string>();
            for (int i = 0; i < _boneNames.Length; i++)
            {
                boneNameList.Add(_boneNames[i]);
            }
            boneNameList.Add(boneName);
            _boneNames = boneNameList.ToArray();

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

            // (3) library_controllers = boneNames, InvBindPoses, boneIndex, boneWeight
            // invBindPoses는 계산할 수 있으므로 생략가능하다.
            AniXmlLoader.LibraryController(xml, 
                out List<string> boneNames, 
                out Dictionary<string, Matrix4x4f> invBindPoses,
                out List<Vertex4i> lstBoneIndex, 
                out List<Vertex4f> lstBoneWeight, 
                out Matrix4x4f bindShapeMatrix);

            _bindShapeMatrix = bindShapeMatrix;

            // 뼈대명 배열을 만들고 뼈대 인덱스 딕셔너리를 만든다.
            _boneNames = new string[boneNames.Count];
            _dicBoneIndex = new Dictionary<string, int>();
            for (int i = 0; i < boneNames.Count; i++)
            {
                _boneNames[i] = boneNames[i].Trim();
                _dicBoneIndex.Add(_boneNames[i], i);
            }

            // (4) library_animations = 애니메이션 정보
            AniXmlLoader.LibraryAnimations(this, xml);

            // (5) library_visual_scenes = bone hierarchy + rootBone
            _rootBone = AniXmlLoader.LibraryVisualScenes(xml, invBindPoses, _dicBoneIndex, out _dicBones);

            // (6) source positions으로부터 
            Matrix4x4f A0 = _rootBone.LocalBindTransform;
            Matrix4x4f S = _bindShapeMatrix;
            Matrix4x4f A0xS = A0 * S;
            for (int i = 0; i < lstPositions.Count; i++)
            {
                lstPositions[i] = A0xS.Multiply(lstPositions[i]);
            }

            // (7) invBindPose 행렬을 계산한다.
            Stack<Bone> bStack = new Stack<Bone>();
            bStack.Push(_rootBone);
            while (bStack.Count > 0)
            {
                Bone cBone = bStack.Pop();
                Matrix4x4f prevAnimatedMat = (cBone.Parent == null ? Matrix4x4f.Identity : cBone.Parent.AnimatedBindTransform);
                cBone.AnimatedBindTransform = prevAnimatedMat * cBone.LocalBindTransform;
                cBone.InverseBindTransform = cBone.AnimatedBindTransform.Inversed();
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
                    _boneIndices.Add(lstBoneIndex[(int)meshTriangles.Vertices[i]]);

                for (int i = 0; i < meshTriangles.Vertices.Count; i++)
                    _boneWeights.Add(lstBoneWeight[(int)meshTriangles.Vertices[i]]);

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
