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
        public static Motionable LoadBindPoseMotion(AnimRig animRig)
        {
            // AnimRig를 이용하여 기본 바인딩 포즈를 1초 Motion으로 생성한다.
            Motion bindPoseMotion = new Motion("BindPose", 1.0f);
            // 0초와 1초에 키프레임을 생성한다.
            bindPoseMotion.AddKeyFrame(0.0f);
            bindPoseMotion.AddKeyFrame(1.0f);

            // 모든 뼈대에 대해 바인딩 포즈를 적용한다.
            foreach (Bone bone in animRig.Armature.RootBone.ToBFSList())
            {
                // 바인딩 포즈의 로컬 변환 행렬에서 위치, 회전, 스케일을 추출한다.
                Matrix4x4f localBindTransform = bone.BoneMatrixSet.LocalBindTransform;

                // 스케일 추출 (각 열벡터의 크기)
                float dist0 = localBindTransform.Column0.xyz().Length();
                float dist1 = localBindTransform.Column1.xyz().Length();
                float dist2 = localBindTransform.Column2.xyz().Length();

                // 회전행렬에서 열벡터를 정규화한다.
                Matrix4x4f normalizedMatrix = localBindTransform;
                normalizedMatrix.NormalizeColumn(0);
                normalizedMatrix.NormalizeColumn(1);
                normalizedMatrix.NormalizeColumn(2);

                // 위치 추출
                Vertex3f position = normalizedMatrix.Position;

                // 회전 쿼터니언 추출 및 정규화
                ZetaExt.Quaternion rotation = normalizedMatrix.ToQuaternion();
                rotation.Normalize();

                // 스케일 벡터 생성
                Vertex3f scaling = new Vertex3f(dist0, dist1, dist2);

                // 본 변환 객체 생성 (위치, 회전, 스케일 모두 포함)
                BoneTransform boneTransform = new BoneTransform(position, rotation, scaling);

                // 0초와 1초 키프레임에 동일한 바인딩 포즈를 추가한다.
                bindPoseMotion[0.0f].AddBoneTransform(bone.Name, boneTransform);
                bindPoseMotion[1.0f].AddBoneTransform(bone.Name, boneTransform);
            }
            return bindPoseMotion;
        }

        public static (Armature, MotionStorage, List<TexturedModel>, Matrix4x4f) LoadFile(string filename, string hipBoneName) 
        {
            // dae 파일을 읽어온다.
            XmlDocument xml = new XmlDocument();
            xml.Load(filename);

            // 텍스처, 재질, 이미지효과 정보를 읽어온다.
            Dictionary<string, Texture> textures = AniColladaLoader.LibraryImages(filename, xml);
            Dictionary<string, string> materialToEffect = AniColladaLoader.LoadMaterials(xml);
            Dictionary<string, string> effectToImage = AniColladaLoader.LoadEffect(xml);

            Armature armature = new Armature(hipBoneName);
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

            // library_visual_scenes = bone hierarchy + rootBone
            AniColladaLoader.LibraryVisualScenes(xml, invBindPoses, ref armature, out Matrix4x4f bind);

            // 힙본을 설정한다.
            if (armature.DicBones.ContainsKey(hipBoneName))
            {
                armature.DicBones[hipBoneName].IsHipBone = true;
            }
            else
            {
                throw new Exception("골격(Armature)을 사용하기 위해서는 hipBone의 Name을 설정해야 합니다.");
            }

            // (6) source positions으로부터 
            Matrix4x4f A0 = armature.RootBone.BoneMatrixSet.LocalBindTransform;
            Matrix4x4f S = bindShapeMatrix;
            Matrix4x4f A0xS = A0 * bindShapeMatrix;

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

                // 부모 뼈대가 존재하면 부모 뼈대의 애니메이션 바인드 포즈를 가져온다.
                if (cBone.Parent != null)
                {
                    prevAnimatedMat = boneDict[cBone.Parent.Name];
                }

                // 현재 뼈대의 애니메이션 바인드 포즈를 계산한다.
                boneDict[cBone.Name] = prevAnimatedMat * cBone.BoneMatrixSet.LocalBindTransform;

                // 역바인딩포즈를 계산한다.
                cBone.BoneMatrixSet.InverseBindPoseTransform = boneDict[cBone.Name].Inversed();
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
