using Model3d;
using System.Collections.Generic;
using System.Xml;
using System;
using OpenGL;
using ZetaExt;

namespace Animate
{
    public class SimpleDaeLoader
    {
        /// <summary>
        /// 메시의 기본 정보만을 읽어온다.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static TexturedModel LoadOnlyGeometryMesh(string filename)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(filename);

            // 텍스처, 매터리얼을 로딩한다.
            Dictionary<string, Texture> textures = AniColladaLoader.LibraryImages(filename, xml);
            Dictionary<string, string> materialToEffect = AniColladaLoader.LoadMaterials(xml);
            Dictionary<string, string> effectToImage = AniColladaLoader.LoadEffect(xml);

            // 기하정보(position, normal, texcoord, color)를 로딩한다.
            List<MeshTriangles> meshes = AniColladaLoader.LibraryGeometris(xml, out List<Vertex3f> lstPositions, out List<Vertex2f> lstTexCoord, out List<Vertex3f> lstNormals);

            XmlNodeList library_visual_scenes = xml.GetElementsByTagName("library_visual_scenes");
            XmlNode nodes = library_visual_scenes[0];// ["visual_scene"];
            string[] value = nodes.InnerText.Split(' ');
            float[] items = new float[value.Length];
            for (int i = 0; i < value.Length; i++) items[i] = float.Parse(value[i]);
            Matrix4x4f transform = new Matrix4x4f(items).Transposed;

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

    }
}