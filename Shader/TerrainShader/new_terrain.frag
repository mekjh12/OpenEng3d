#version 430 core

// G-Buffer MRT 출력
layout(location = 0) out vec4 gAlbedo;
layout(location = 1) out vec4 gPosition;
layout(location = 2) out vec4 gNormal;
layout(location = 3) out float gDepth;

// UBO: 카메라
layout(std140, binding = 0) uniform CameraBlock { mat4 view; mat4 proj; mat4 vp; vec4 cameraPos; } camera;

// 입력 (TES에서 전달)
in vec2 Tex3;
in vec4 fragPos;
in float viewDepth;

// 텍스처들
uniform sampler2D gHeightMap;
uniform sampler2D gNormalMap;
uniform sampler2D gTextureHeight0;
uniform sampler2D gTextureHeight1;
uniform sampler2D gTextureHeight2;
uniform sampler2D gTextureHeight3;
uniform sampler2D gTextureHeight4;
uniform sampler2D gDetailMap;
uniform bool gIsDetailMap = true;
uniform float gHeight0 = 0.17f;
uniform float gHeight1 = 0.25f;
uniform float gHeight2 = 0.35f;
uniform float gHeight3 = 0.71f;
uniform float gHeight4 = 0.82f;

// 지층맵 유니폼
uniform sampler2D gRockTexture;
uniform sampler2D gFaultMap;                    // 단층 맵
uniform float gFaultMapScale = 0.0001f;         // UV 스케일
uniform float gFaultDisplacementScale = 80.0f;  // 변위 강도 (미터)
uniform float gFaultZoneWidth = 0.05f;          // 각력암 폭
uniform float gFaultZoneIntensity = 0.7f;       // 각력암 강도

uniform float gColorTexcoordScaling = 800.0f;
uniform mat3 normalMatrix;
uniform bool onFunc = true;

//-----------------------------------------------------------------------------
// 단층 시스템: 보로노이 맵 기반 지층 변위
//-----------------------------------------------------------------------------
vec3 ApplyFaultSystem(vec3 worldPos, out float faultZoneMask)
{
    if (!onFunc) {
        return worldPos;
    }

    // 1. 보로노이 단층 맵 샘플링
    vec2 faultUV = worldPos.xy * gFaultMapScale;
    vec3 faultData = texture(gFaultMap, faultUV).rgb;
    
    // R: 변위량 (0~1 → -1~1로 복원)
    float displacement = (faultData.r * 2.0 - 1.0) * gFaultDisplacementScale;
    
    // G: 경계 거리 (0=경계선, 1=셀 중심)
    float edgeDistance = faultData.g;
    
    // 2. 단층대(Fault Zone) 마스크 계산
    faultZoneMask = 1.0 - smoothstep(0.0, gFaultZoneWidth, edgeDistance);
    
    // 3. 월드 좌표 변위 적용 (Z축 = 수직 변위)
    vec3 displaced = worldPos;
    displaced.z += displacement;
    
    return displaced;
}

//-----------------------------------------------------------------------------
// 개선된 트라이플래너 매핑 (Macro/Micro Tiling Variation 적용)
//-----------------------------------------------------------------------------
vec4 GetTriplanarTextureAdvanced(sampler2D tex, vec3 worldPos, vec3 normal, float scale)
{
    // 1. 두 가지 스케일 설정
    float microScale = scale;          // 기본 상세 질감 (예: 1.0)
    float macroScale = scale * 0.1;    // 10배 더 큰 거대 질감 (반복 패턴을 깨는 용도)

    // 2. Micro 샘플링 (Z-Up)
    vec3 blendWeights = pow(abs(normal), vec3(4.0));
    blendWeights /= (blendWeights.x + blendWeights.y + blendWeights.z);

    vec4 microX = texture(tex, worldPos.zy * microScale);
    vec4 microY = texture(tex, worldPos.xz * microScale);
    vec4 microZ = texture(tex, worldPos.xy * microScale);
    vec4 microColor = microX * blendWeights.x + microY * blendWeights.y + microZ * blendWeights.z;

    // 3. Macro 샘플링 (Z-Up) - 더 큰 스케일로 한 번 더 샘플링
    vec4 macroX = texture(tex, worldPos.zy * macroScale);
    vec4 macroY = texture(tex, worldPos.xz * macroScale);
    vec4 macroZ = texture(tex, worldPos.xy * macroScale);
    vec4 macroColor = macroX * blendWeights.x + macroY * blendWeights.y + macroZ * blendWeights.z;

    // 4. 두 텍스처 합성
    // 단순히 더하면 너무 밝아지므로, 마이크로 텍스처의 디테일을 유지하면서 
    // 매크로의 큰 색상 변화를 곱해주는 방식(Soft Light 혹은 Overlay 유사 방식)이 좋습니다.
    // 여기서는 간단하고 효과적인 보간(mix)을 사용합니다.
    float dist = length(camera.cameraPos.xyz - worldPos);
    float fade = clamp(dist / 500.0, 0.0, 1.0); // 500 유닛 거리에서 완전히 교체
    return mix(microColor, macroColor, fade);
}

//-----------------------------------------------------------------------------
// 고급 지형 텍스처 블렌딩 (Z-Up, 높이 + 경사 + 트라이플래너)
//-----------------------------------------------------------------------------
vec4 BlendTerrainTexturesAdvanced(
    float Height, vec3 worldPos, vec3 normalWorld, 
    float texScale, // C#에서 넘어오는 기본 스케일 값 (예: 800.0)
    bool useDetail)
{
    // [핵심 보정] 월드 좌표는 단위가 크므로 스케일을 대폭 낮춰야 질감이 보입니다.
    // 기존 Tex3(0~1) 대비 worldPos(0~2000)라면 0.001~0.005 정도가 적당합니다.
    float worldTexScale = texScale * 0.0001; 
    vec2 topDownUV = worldPos.xy * worldTexScale; 

    // ----------------------------------------------------
    // 1. 높이 기반 기본 색상 계산 (평지용)
    // ----------------------------------------------------
    vec4 heightColor;
    if (Height < gHeight0) {
        heightColor = texture(gTextureHeight0, topDownUV);
    } else if (Height < gHeight1) {
        float Factor = (Height - gHeight0) / (gHeight1 - gHeight0);
        heightColor = mix(texture(gTextureHeight0, topDownUV), texture(gTextureHeight1, topDownUV), Factor);
    } else if (Height < gHeight2) {
        float Factor = (Height - gHeight1) / (gHeight2 - gHeight1);
        heightColor = mix(texture(gTextureHeight1, topDownUV), texture(gTextureHeight2, topDownUV), Factor);
    } else if (Height < gHeight3) {
        float Factor = (Height - gHeight2) / (gHeight3 - gHeight2);
        heightColor = mix(texture(gTextureHeight2, topDownUV), texture(gTextureHeight3, topDownUV), Factor);
    } else { 
        float Factor = clamp((Height - gHeight3) / (gHeight4 - gHeight3), 0.0, 1.0);
        heightColor = mix(texture(gTextureHeight3, topDownUV), texture(gTextureHeight4, topDownUV), Factor);
    }

    // ----------------------------------------------------
    // 2. 경사도 계산 (Z-Up 기준)
    // normalWorld.z = cos(θ), slope = 1 - cos(θ)
    // slope 범위: 0.0(평지 0°) → 1.0(절벽 90°)
    // ----------------------------------------------------
    float slope = 1.0 - clamp(normalWorld.z, 0.0, 1.0);
    
    // ----------------------------------------------------
    // 3. 경사 블렌딩 (약 37°~60°)
    // 0.2 미만: 평지(높이 기반), 0.5 이상: 절벽(바위)
    // ----------------------------------------------------
    float slopeBlend = smoothstep(0.2, 0.5, slope);

    // ----------------------------------------------------
    // 4. 절벽 바위 텍스처 (트라이플래너 적용으로 늘어짐 방지)
    // ----------------------------------------------------
    vec4 rockColor = GetTriplanarTextureAdvanced(gRockTexture, worldPos, normalWorld, worldTexScale);

    // ============================================================
    // ⭐ 단층 시스템 적용
    // ============================================================
    float faultZoneMask;
    vec3 displacedPos = ApplyFaultSystem(worldPos, faultZoneMask);
    
    // 지층 효과 설정 (displacedPos 사용)
    float strataInput = (displacedPos.z + displacedPos.y * 0.2) * 10.0; // strata(단층)
    float strataPattern = 0.9 + 0.1 * sin(strataInput);
    
    // 불규칙함을 주기 위한 노이즈
    float variation = sin(displacedPos.z * 2.0 + displacedPos.x * 0.5) * 0.05;
    strataPattern += variation;
    
    // 지층 색상
    vec3 strataColor = mix(vec3(0.9, 0.8, 0.7), vec3(1.0, 1.0, 1.0), strataPattern);
    
    // ⭐ 단층대(Fault Zone) 효과: 각력암 색상
    vec3 brecciaColor = vec3(0.45, 0.4, 0.35); // (각력암)어두운 파쇄암
    strataColor = mix(strataColor, brecciaColor, faultZoneMask * gFaultZoneIntensity);
    
    // 바위 색상에 지층 무늬 적용
    rockColor.rgb *= strataPattern;
    
    // 5. 최종 합성: 높이별 색상 위에 경사도에 따라 바위를 덮음
    vec4 finalColor = mix(heightColor, rockColor, slopeBlend);

    // 6. 디테일 맵 적용
    if (useDetail) {
        // 디테일 맵은 더 촘촘하게 타일링 (10배)
        finalColor *= texture(gDetailMap, topDownUV * 10.0); 
    }

    return finalColor;
}

//-----------------------------------------------------------------------------
// 메인 함수
//-----------------------------------------------------------------------------
void main()
{
    // 높이 샘플링
    float height = texture(gHeightMap, Tex3).r;
    
    // Normal Map에서 법선 로드
    vec3 normalTangent = texture(gNormalMap, Tex3).rgb;
    normalTangent = normalTangent * 2.0 - 1.0;  // [0,1] → [-1,1]
    
    // 3. 월드 공간으로 변환
    vec3 normalWorld = normalize(normalMatrix * normalTangent);
    
    // 4. 지형 텍스처 블렌딩 (함수 교체)
    // Tex3 대신 fragPos.xyz(월드 좌표)와 normalWorld(법선)를 넘겨줍니다.
    vec4 texColor = BlendTerrainTexturesAdvanced(
        height, fragPos.xyz, normalWorld, // 변경된 인자
        gColorTexcoordScaling, gIsDetailMap);

    // 5. G-Buffer 출력
    gAlbedo = vec4(texColor.rgb, 1.0);
    gPosition = vec4(fragPos.xyz, 1.0);
    gNormal = vec4(normalWorld, 1.0);
    gDepth = viewDepth / 10000.0;
}

