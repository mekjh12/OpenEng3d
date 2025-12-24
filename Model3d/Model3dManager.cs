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
        protected Dictionary<string, UnifiedTexturedModel> _dicRawModel;  // 모델 데이터 저장소

        protected string RootPath { get => _rootPath; }


        float _sunVerticalTheta = 90.0f;


        public Model3dManager(string rootPath, string nullTextureFileName)
        {
            _rootPath = rootPath;
            TextureStorage.NullTextureFileName = nullTextureFileName;
            _dicRawModel = new Dictionary<string, UnifiedTexturedModel>();
        }

        public UnifiedTexturedModel AddRawModel(string modelFileName)
        {
            string materialFileName = modelFileName.Replace(".obj", ".mtl");

            // 모델을 읽어온다.
            UnifiedTexturedModel texturedModels = ObjLoaderEx.LoadObjUnified(_rootPath + modelFileName);

            // LOD1 모델을 읽어온다.
            UnifiedTexturedModel texturedModel_lod1 = ObjLoaderEx.LoadObjUnified(_rootPath + modelFileName.Replace(".obj", "_lod1.obj"));

            var texturedModel_Lod1 = new UnifiedTexturedModelLOD(texturedModels, texturedModel_lod1);

            // 모델을 캐시에 저장한다.
            _dicRawModel[Path.GetFileNameWithoutExtension(modelFileName)] = texturedModel_Lod1;

            return texturedModel_Lod1;
        }

        public UnifiedTexturedModel GetModels(string modelName)
        {
            return _dicRawModel.ContainsKey(modelName) ? _dicRawModel[modelName] : null;
        }
    }
}
