using Common.Abstractions;
using Model3d;
using Renderer;
using Shader;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BillBoard
{
    public class ImpostorAssets
    {
        Dictionary<string, uint> _dicImpostorAtlas;                     // 임포스터 아틀라스 딕셔너리
        Dictionary<string, uint> _dicNormalImpostorAtlas;               // 노말 임포스터 아틀라스 딕셔너리
        Dictionary<string, ImpostorSettings> _impostorSettings;         // 임포스터 셋팅 딕셔너리
        Dictionary<string, UnifiedTexturedModel> _unifiedTexturedModel; // 통합 텍스처 모델 딕셔너리

        ImpostorAtlasGenerator _atlasGenerator;                     // 임포스터 아틀라스 생성기
        UnlitShader _shader;                                        // 임포스터 셰이더
        Camera _camera;                                             // 카메라

        string _currentImposterName = "";                           // 현재 모델 이름
        ImpostorRenderData _impostorRenderData;                     // 임포스터 렌더링 데이터

        // ---------------------------------------------------------
        // 속성
        // ---------------------------------------------------------

        public ImpostorRenderData ImpostorRenderData => _impostorRenderData;
        public uint CurrentAtlasTexture => GetAtlasTexture(_currentImposterName);

        // ---------------------------------------------------------
        // 생성자
        // ---------------------------------------------------------

        public ImpostorAssets(UnlitShader shader, Camera camera)
        {
            _shader = shader;
            _camera = camera;
            _atlasGenerator = new ImpostorAtlasGenerator();
            _dicImpostorAtlas = new Dictionary<string, uint>();
            _dicNormalImpostorAtlas = new Dictionary<string, uint>();
            _impostorSettings = new Dictionary<string, ImpostorSettings>();
            _unifiedTexturedModel = new Dictionary<string, UnifiedTexturedModel>();
        }

        public UnifiedTexturedModel UnifiedTexturedModel(string modelName)
        {
            if (_unifiedTexturedModel.ContainsKey(modelName))
            {
                return _unifiedTexturedModel[modelName];
            }
            return null;
        }

        public void CreateImpostorModel(ImpostorSettings settings, UnifiedTexturedModel texturedModels)
        {
            // (1) 해당 모델의 임포스터가 아직 생성되지 않은 경우에만 생성
            if (!_dicImpostorAtlas.ContainsKey(settings.Name))
            {
                uint textureId = _atlasGenerator.GenerateAtlas(_shader, settings, settings.Name,
                    texturedModels, _camera);

                _dicImpostorAtlas.Add(settings.Name, textureId);
                _dicNormalImpostorAtlas.Add(settings.Name, textureId);

                _unifiedTexturedModel.Add(settings.Name, texturedModels);

                // 임포스터 렌더링 데이터 설정
                SetImposterRenderData(settings, textureId);
            }

            // (2) 해당 셋팅의 임포스터가 아직 생성되지 않은 경우에만 생성
            if (!_impostorSettings.ContainsKey(settings.Name))
            {
                _impostorSettings.Add(settings.Name, settings);
            }

            // 현재 모델 이름 설정
            _currentImposterName = settings.Name;
        }

        private uint GetAtlasTexture(string modelName)
        {
            return _dicImpostorAtlas.ContainsKey(modelName) ? _dicImpostorAtlas[modelName] : 0;
        }

        private uint GetNormalAtlasTexture(string modelName)
        {
            return _dicNormalImpostorAtlas.ContainsKey(modelName) ? _dicNormalImpostorAtlas[modelName] : 0;
        }

        private ImpostorSettings GetImpostorSettings(string modelName)
        {
            return _impostorSettings.ContainsKey(modelName) ? _impostorSettings[modelName] : ImpostorSettings.CreateDefault(modelName);
        }

        public ImpostorRenderData GetImpostorRenderData(string modelName)
        {
            ImpostorSettings settings = GetImpostorSettings(modelName);
            ImpostorRenderData renderData = new ImpostorRenderData
            {
                atlasSize = settings.AtlasSize,
                individualSize = settings.IndividualSize,
                horizontalFrames = settings.HorizontalAngles,
                verticalFrames = settings.VerticalAngles,
                enableEdgeLine = false,
                AtlasTextureId = GetAtlasTexture(modelName),
                NormalAtlasTextureId = GetNormalAtlasTexture(modelName),
            };
            return renderData;
        }

        public void SetImposter(string modelName, bool enableEdgeline = false)
        {
            // 해당 모델의 임포스터가 존재하는지 확인
            if (_dicImpostorAtlas.ContainsKey(modelName) == false)
            {
                throw new System.Exception($"ImpostorLODSystem: No impostor atlas found for model '{modelName}'.");
            }

            uint textureId = GetAtlasTexture(modelName);
            ImpostorSettings currentImposterSettings = GetImpostorSettings(modelName);
            SetImposterRenderData(currentImposterSettings, textureId);
        }

        private void SetImposterRenderData(ImpostorSettings settings, uint textureId)
        {
            if (_impostorRenderData == null)
            {
                _impostorRenderData = new ImpostorRenderData();
            }

            _impostorRenderData.AtlasTextureId = textureId;
            _impostorRenderData.atlasSize = settings.AtlasSize;
            _impostorRenderData.individualSize = settings.IndividualSize;
            _impostorRenderData.horizontalFrames = settings.HorizontalAngles;
            _impostorRenderData.verticalFrames = settings.VerticalAngles;
            _impostorRenderData.enableEdgeLine = false;
        }
    }
}