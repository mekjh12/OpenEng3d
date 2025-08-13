using Common.Abstractions;
using Model3d;
using OpenGL;
using System;
using System.Collections.Generic;
using System.Xml;
using ZetaExt;

namespace Animate
{
    public class AniRigLoader
    {
        public static (Armature, MotionStorage, List<TexturedModel>, Matrix4x4f) LoadFile(string filename) 
        {
            // dae 파일을 읽어온다.
            XmlDocument xml = new XmlDocument();
            xml.Load(filename);

            // 텍스처, 재질, 이미지효과 정보를 읽어온다.
            Dictionary<string, Texture> textures = AniColladaLoader.LibraryImages(filename, xml);
            Dictionary<string, string> materialToEffect = AniColladaLoader.LoadMaterials(xml);
            Dictionary<string, string> effectToImage = AniColladaLoader.LoadEffect(xml);

            Armature armature = new Armature();
            MotionStorage motions = new MotionStorage();

            // 지오메트리 정보를 읽어온다. position, normal, texcoord, color, MeshTriangles 정보가 포함되어 있다.
            List<MeshTriangles> meshes = AniColladaLoader.LibraryGeometris(xml,
                out List<Vertex3f> lstPositions,
                out List<Vertex2f> lstTexCoord,
                out List<Vertex3f> lstNormals);

            // 컨트롤러 정보, 역바인딩포즈를 읽어온다.
            AniColladaLoader.LibraryController(xml,
                out List<string> boneNames,
                out Dictionary<string, Matrix4x4f> invBindPoses,
                out List<BoneWeightVector4> vertexBoneData,
                out Matrix4x4f bindShapeMatrix);

            // 정점과 정점 컨트롤 데이터의 갯수가 일치하는지 확인한다.
            if (lstPositions.Count != vertexBoneData.Count)
                Console.WriteLine("[주의] 지오메트리의 정점의 갯수와 정점을 컨트롤할 데이터의 갯수가 다릅니다.");

            // 뼈대명 배열을 만들고 뼈대 인덱스 딕셔너리를 만든다.
            armature.SetupBoneMapping(boneNames.ToArray());

            // (4) library_animations = 애니메이션 정보
            //AniXmlLoader.LibraryAnimations(xml);

            // (5) library_visual_scenes = bone hierarchy + rootBone
            AniColladaLoader.LibraryVisualScenes(xml, invBindPoses, ref armature);

            // (6) source positions으로부터 
            Matrix4x4f A0 = armature.RootBone.BoneTransforms.LocalBindTransform;
            Matrix4x4f S = bindShapeMatrix;
            Matrix4x4f A0xS = A0 * S;

            // 모든 꼭지점에 바이딩세이프를 적용한다.
            for (int i = 0; i < lstPositions.Count; i++)
            {
                lstPositions[i] = A0xS.Multiply(lstPositions[i]);
            }

            Dictionary<string, Matrix4x4f> boneDict = new Dictionary<string, Matrix4x4f>();

            // 역바인딩포즈를 계산한다. 
            // LibraryController에서 미리 계산된 역바인딩포즈가 있지만, 
            // 애니메이션을 적용하기 위해서 다시 계산한다.
            foreach (Bone cBone in armature.RootBone.ToBFSList())
            {
                // 부모 뼈대의 애니메이션 바인드 포즈를 계산한다.
                Matrix4x4f prevAnimatedMat = Matrix4x4f.Identity;

                if (cBone.Parent != null)
                {
                    prevAnimatedMat = boneDict[cBone.Parent.Name];
                }

                // 현재 뼈대의 애니메이션 바인드 포즈를 계산한다.
                boneDict[cBone.Name] = prevAnimatedMat * cBone.BoneTransforms.LocalBindTransform;

                // 역바인딩포즈를 계산한다.
                cBone.BoneTransforms.InverseBindPoseTransform = boneDict[cBone.Name].Inversed();
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

            return (armature, motions, texturedModels, bindShapeMatrix);
        }
    }
}
