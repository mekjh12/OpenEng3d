using OpenGL;
using Shader;
using System;
using System.Collections.Generic;
using ZetaExt;

namespace Model3d
{
    /// <summary>
    /// 3D 모델의 임포스터 아틀라스를 생성하는 클래스.
    /// 임포스터는 3D 모델을 다양한 각도에서 2D 이미지로 렌더링한 것으로,
    /// 원거리에서 3D 모델 대신 사용하여 렌더링 성능을 향상시킬 수 있습니다.
    /// 
    /// 중요한 제약사항:
    /// 1. 아틀라스 생성 시점의 제약:
    ///    - 오직 수평각(방위각)과 수직각(고도각)으로만 뷰를 생성
    ///    - 카메라는 항상 모델의 중심을 향하고 roll 회전이 없는 상태로만 캡처됨
    ///    - 원거리 객체의 특성을 고려하여 수직각의 범위를 제한함:
    ///      * 멀리 있는 객체는 주로 수평선 근처나 그 위에서 관찰됨
    ///      * 매우 낮은 각도나 높은 각도에서 보는 경우가 드묾
    ///      * 따라서 수직각을 -30° ~ 60° 정도로 제한하여 메모리와 성능 최적화
    /// 
    ///    [아틀라스 뷰 생성 다이어그램]
    ///    수직각 (VerticalAngle)
    ///          60°    |    카메라 위치들
    ///          ↑      |      *   *   *
    ///          |      |    *   *   *   *
    ///     고도각      |  *   *   *   *   *
    ///          ↓      |    *   *   *   *
    ///         -30°    |      *   *   *
    ///                 +------------------→ 수평각 (HorizontalAngle)
    ///                 0°     180°    360°
    /// 
    /// 2. 실행 시점의 문제:
    ///    - 모델이 pitch나 roll로 회전된 상태로 월드에 배치되면
    ///    - 그 회전된 상태는 아틀라스에 미리 렌더링된 뷰들 중 어느 것과도 정확히 매칭되지 않음
    ///    - GetAtlasOffset을 정교하게 계산해도, 실제로 그 시점에서 본 모습과 일치하는 이미지가 아틀라스에 없음
    /// 
    ///    [회전 제약 다이어그램]
    ///    허용됨:           제한됨:
    ///      Yaw             Pitch           Roll
    ///       ↓               ↓               ↓
    ///      [■]            [■]             [■]
    ///       ↺              ↺               ↺
    ///                      x               x
    /// 
    /// 해결 가능한 접근 방법들:
    /// 1. 제한적 사용:
    ///    - pitch와 roll 회전을 사용하지 않고 yaw 회전만 사용하도록 제한
    ///    - 또는 pitch/roll이 있는 모델은 임포스터를 사용하지 않음
    /// 2. 아틀라스 확장:
    ///    - pitch와 roll에 대한 추가 뷰들도 아틀라스에 포함
    ///    - 하지만 이는 아틀라스 크기를 크게 증가시키고 생성 시간도 길어짐
    /// 3. 실시간 보정:
    ///    - 가장 가까운 뷰를 선택하고 셰이더에서 추가 변환 적용
    ///    - 하지만 이는 임포스터의 장점을 일부 상쇄할 수 있음
    /// 
    /// 현재 구현에서는 pitch/roll 회전이 있는 모델에 대해서는 임포스터를 사용하지 않거나,
    /// yaw 회전만 사용하도록 제한하는 것이 가장 현실적인 해결책입니다.
    /// </summary>
    public class ImpostorLODSystem
    {
        float _impostorDistance;                      // 임포스터로 전환할 거리 임계값
        ImpostorAtlasGenerator _atlasGenerator;       // 임포스터 텍스처 아틀라스 생성기
        Dictionary<string, uint> _impostorModels;     // 엔티티별 임포스터 모델 캐시 (Key: 모델명, Value: 텍스처 ID)
        Dictionary<string, ImpostorSettings> _impostorSettings;

        /// <summary>
        /// ImpostorLODSystem 생성자
        /// </summary>
        /// <param name="impostorDistance">임포스터로 전환할 거리 임계값</param>
        /// <param name="settings">임포스터 생성 설정</param>
        public ImpostorLODSystem(float impostorDistance = 200.0f)
        {
            _impostorDistance = impostorDistance;
            _atlasGenerator = new ImpostorAtlasGenerator();
            _impostorModels = new Dictionary<string, uint>();
            _impostorSettings = new Dictionary<string, ImpostorSettings>();
        }

        /// <summary>
        /// 주어진 엔티티에 대해 임포스터를 사용할지 결정
        /// </summary>
        /// <param name="entity">검사할 엔티티</param>
        /// <param name="cameraPosition">현재 카메라 위치</param>
        /// <returns>임포스터 사용 여부</returns>
        public bool ShouldUseImpostor(LodEntity entity, 
            Vertex3f cameraPosition, 
            Matrix4x4f viewprojMatrix, 
            bool isUseSpaceArea = false)
        {
            if (!isUseSpaceArea)
            {
                // 1. 거리 체크(속도를 위해서 간결히)
                float distance = (entity.Position - cameraPosition).DistanceSquare();
                float dist = entity.DistanceLodLow;
                return (distance > dist * dist);
            }
            else
            {
                // 1. 화면에 보이는 현재 노드의 정규화된 화면 면적 계산
                float area = entity.AABB.CalculateScreenSpaceArea(viewprojMatrix);
                if (area > 0.002f) return false;

                // 2. 회전 행렬 분석
                Matrix4x4f rotationMatrix = entity.ModelMatrix;

                // 회전 행렬에서 right와 up 벡터 추출
                Vertex3f right = rotationMatrix.Column0.Vertex3f().Normalized;
                Vertex3f up = -rotationMatrix.Column2.Vertex3f().Normalized;  // z 오른손 법칙

                // pitch와 roll이 0인 경우:
                // - right 벡터는 xY 평면에 있어야 함 (z가 0에 가까움)
                // - up 벡터는 Z축과 일치해야 함 (x, y가 0에 가까움)
                const float TOLERANCE = 0.01f;  // 허용 오차

                bool hasNoPitch = Math.Abs(right.z) < TOLERANCE;
                bool hasNoRoll = Math.Abs(up.x) < TOLERANCE && Math.Abs(up.y) < TOLERANCE;

                return hasNoPitch && hasNoRoll;
            }
        }

        public void CreateImpostorModel(string modelname, ImpostorSettings settings, UnlitShader shader, TexturedModel[] texturedModels)
        {
            // 해당 모델의 임포스터가 아직 생성되지 않은 경우에만 생성
            if (!_impostorModels.ContainsKey(modelname))
            {
                _impostorModels.Add(modelname, _atlasGenerator.GenerateAtlas(shader, settings, modelname, texturedModels));
            }

            // 해당 셋팅의 임포스터가 아직 생성되지 않은 경우에만 생성
            if (!_impostorSettings.ContainsKey(modelname))
            {
                _impostorSettings.Add(modelname, settings);
            }
        }

        public Vertex2f GetAtlasOffset(ImpostorSettings settings, Vertex3f cameraPosition, Entity entity)
        {
            // 1. 카메라에서 모델로의 방향 벡터 계산 (월드 공간)
            Vertex3f toCamera = (cameraPosition - entity.Position).Normalized;

            // 2. 모델의 회전 행렬만 추출
            Matrix4x4f modelMatrix = entity.ModelMatrix;
            Matrix4x4f rotationMatrix = modelMatrix.ToMatrix3x3f();

            // 3. 월드 공간의 카메라 방향을 모델의 로컬 공간으로 변환
            Vertex3f localDir = rotationMatrix.Inverse.Transform(toCamera).Normalized;

            // 4. 로컬 공간에서 수평각과 수직각 계산
            float horizontalAngle = ((float)Math.Atan2(localDir.y, localDir.x)).ToDegree();
            float verticalAngle = ((float)Math.Asin(localDir.z)).ToDegree();

            // 5. 각도 범위 조정
            float paddingHorizontalAngle = 180.0f / settings.HorizontalAngles;
            float paddingVerticalAngle = 0.5f * (settings.VerticalAngleMax - settings.VerticalAngleMin) / settings.VerticalAngles;
            horizontalAngle = (horizontalAngle + paddingHorizontalAngle + 360.0f) % 360.0f; // 0~360 범위로 정규화
            verticalAngle = (verticalAngle + paddingVerticalAngle).Clamp(settings.VerticalAngleMin, settings.VerticalAngleMax);

            // 6. 아틀라스 인덱스 계산
            int hIndex = (int)(horizontalAngle * settings.HorizontalAngles / 360.0f);
            int vIndex = (int)((verticalAngle - settings.VerticalAngleMin) /
                              (settings.VerticalAngleMax - settings.VerticalAngleMin) *
                              (settings.VerticalAngles - 1));

            // 7. 범위 검사
            hIndex = hIndex.Clamp(0, settings.HorizontalAngles - 1);
            vIndex = vIndex.Clamp(0, settings.VerticalAngles - 1);

            // 8. 최종 UV 오프셋 반환
            return new Vertex2f(
                (float)hIndex * settings.IndividualSize / settings.AtlasSize,
                (float)vIndex * settings.IndividualSize / settings.AtlasSize
            );
        }

        /// <summary>
        /// 주어진 엔티티의 임포스터 텍스처 ID 반환
        /// </summary>
        /// <param name="entity">엔티티</param>
        /// <returns>텍스처 ID (없는 경우 0)</returns>
        public uint AtlasTexture(Entity entity)
        {
            string modelname = entity.ModelName;
            return _impostorModels.ContainsKey(modelname) ? _impostorModels[modelname] : 0;
        }

        public ImpostorSettings GetImpostorSettings(Entity entity)
        {
            string modelname = entity.ModelName;
            return _impostorSettings.ContainsKey(modelname) ? _impostorSettings[modelname] : ImpostorSettings.CreateDefault();
        }
    }
}