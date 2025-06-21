using System.Collections.Generic;
using System.IO;

namespace Model3d
{
    public class Model3dManager
    {
        // 시스템 상태 관련 필드
        protected int _total = 0;               // 총 엔티티 수
        private string _rootPath = "";          // 리소스 루트 경로

        // 모델 및 리소스 관리
        protected Dictionary<string, List<TexturedModel>> _dicRawModel;  // 모델 데이터 저장소

        protected string RootPath { get => _rootPath; }


        float _sunVerticalTheta = 90.0f;


        public Model3dManager(string rootPath, string nullTextureFileName)
        {
            _rootPath = rootPath;
            TextureStorage.NullTextureFileName = nullTextureFileName;
            _dicRawModel = new Dictionary<string, List<TexturedModel>>();
        }

        public TexturedModel[] AddRawModel(string modelFileName)
        {
            string materialFileName = modelFileName.Replace(".obj", ".mtl");

            // 텍스쳐모델을 읽어온다.
            List<TexturedModel> texturedModels = ObjLoader.LoadObj(_rootPath + modelFileName);

            // 모델에 맞는 원래 모양의 바운딩 박스를 만든다.
            foreach (TexturedModel texturedModel in texturedModels)
            {
                texturedModel.GenerateBoundingBox();
            }

            // 모델을 캐시에 저장한다.
            _dicRawModel[Path.GetFileNameWithoutExtension(modelFileName)] = texturedModels;

            return texturedModels.ToArray();
        }

        public TexturedModel[] GetModels(string modelName)
        {
            return _dicRawModel.ContainsKey(modelName) ? _dicRawModel[modelName].ToArray() : null;
        }
    }
}
