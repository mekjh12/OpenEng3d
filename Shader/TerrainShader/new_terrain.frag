#version 430 core

// G-Buffer MRT 출력
layout(location = 0) out vec4 gAlbedo;
layout(location = 1) out vec4 gPosition;
layout(location = 2) out vec4 gNormal;
layout(location = 3) out float gDepth;
layout(location = 4) out vec4 gStructure;

// UBO: 카메라
layout(std140, binding = 0) uniform CameraBlock { 
    mat4 view; mat4 proj; mat4 vp; vec4 cameraPos; 
} camera;

// 입력 (TES에서 전달)
in vec2 Tex3;
in vec4 fragPos;
in float viewDepth;
in vec3 viewPosOut;

// 텍스처들
uniform sampler2D heightMapHighRes;
uniform sampler2D gNormalMap;
uniform sampler2D gTextureHeight0;
uniform sampler2D gTextureHeight1;
uniform sampler2D gTextureHeight2;
uniform sampler2D gTextureHeight3;
uniform sampler2D gTextureHeight4;
uniform sampler2D gDetailMap;
uniform bool gIsDetailMap = true;

// ⭐ 계곡 감지 파라미터
uniform float gValleyDetectionRadius = 5.0;  // 계곡 감지 반경 (텍셀 단위)
uniform float gValleyThreshold = 0.02;       // 계곡 판단 임계값
uniform bool gShowValleyDebug = true;        // 디버그 모드 ON/OFF

// 기존 파라미터들
uniform float gHeight0 = 0.1f;//0.17f
uniform float gHeight1 = 0.35f;//0.25f
uniform float gHeight2 = 0.5f;//0.35f
uniform float gHeight3 = 0.7f;//0.71f
uniform float gHeight4 = 0.8f;//0.82f

// 지층맵 유니폼
uniform sampler2D gRockTexture;
uniform sampler2D gFaultMap;
uniform float gFaultMapScale = 0.0001f;
uniform float gFaultDisplacementScale = 80.0f;
uniform float gFaultZoneWidth = 0.05f;
uniform float gFaultZoneIntensity = 0.7f;

// 강유역 유니폼
uniform sampler2D gRiverRoadMap;


uniform sampler2D gMossRockTexture;
uniform float gValleyBlendStart = 0.3;    // 블렌딩 시작 지점
uniform float gValleyBlendEnd = 0.7;      // 블렌딩 종료 지점 (완전히 계곡 텍스처)
uniform float gValleyTexScale = 2000.0f;  // 계곡 텍스처 스케일

uniform float gColorTexcoordScaling = 800.0f;
uniform mat3 normalMatrix;
uniform bool onFunc = true;
uniform float gTime;

// 함수 선언
float hash(vec2 p);
float noise(vec2 p);
vec3 ApplyFaultSystem(vec3 worldPos, out float faultZoneMask);
vec4 BlendTerrainTexturesAdvanced(vec3 worldPos, vec3 normalWorld, float height, float dist);
vec4 GetTriplanarTextureAdvanced(sampler2D tex, vec3 worldPos, vec3 normal, float scale);
float DetectValleyCurvature(vec2 uv, float currentHeight);
vec4 ApplyValleyTexturing(vec2 uv, vec3 normalWorld, vec3 riverMask, vec4 baseColor);
vec4 ApplyRiverTexturing(vec3 pos, vec3 normalWorld, vec4 baseColor);

vec4 CalculateStructureOutput(float z)
{
    uint zBits = floatBitsToUint(z);
    uint hBits = zBits & 0xFFFFE000u;
    float h = uintBitsToFloat(hBits);
    
    float dzdx = dFdx(z) * 64.0;
    float dzdy = dFdy(z) * 64.0;
    
    return vec4(dzdx, dzdy, h, z - h);
}

//-----------------------------------------------------------------------------
// 메인 함수
//-----------------------------------------------------------------------------
void main()
{
    // 높이 샘플링
    float height = texture(heightMapHighRes, Tex3).r;
    
    // 법선 로드
    vec3 normalTangent = texture(gNormalMap, Tex3).rgb;
    normalTangent = normalTangent * 2.0 - 1.0;
    vec3 normalWorld = normalize(normalMatrix * normalTangent);
    
    // 경사도 계산
    float slope = 1.0 - clamp(normalWorld.z, 0.0, 1.0);

    // 카메라와의 거리
    float dist = length(fragPos.xyz - camera.cameraPos.xyz);
    
    // 기본 지형 텍스처 블렌딩
    vec4 texColor = BlendTerrainTexturesAdvanced(fragPos.xyz, normalWorld, height, dist);
        
    // 계곡 텍스처 적용 (스무스 블렌딩)
    texColor = ApplyRiverTexturing(fragPos.xyz, normalWorld, texColor);

    // G-Buffer 출력
    gAlbedo = vec4(2.0f * texColor.rgb, 1.0); // <====2.0은 지워야 함(임시)
    gPosition = vec4(fragPos.xyz, 1.0);
    gNormal = vec4(normalWorld, 1.0);//texture(heightMapHighRes, texCoord).r / 
    gDepth = viewDepth / 10000.0;

    // Structure에 기록하기
    float z = viewPosOut.z;
    gStructure = CalculateStructureOutput(z);
}

vec4 SampleRiverSmooth(vec2 uv)
{
    vec2 texSize = vec2(1024.0); // 텍스처 해상도
    vec2 texelCoord = uv * texSize - 0.5;
    vec2 f = fract(texelCoord);
    
    // smoothstep으로 블록 경계를 부드럽게
    f = f * f * (3.0 - 2.0 * f); // smoothstep hermite
    // 더 부드럽게 하려면: f = f*f*f*(f*(f*6.0-15.0)+10.0); // quintic
    
    vec2 snapped = (floor(texelCoord) + f + 0.5) / texSize;
    return texture(gRiverRoadMap, snapped);
}

vec4 ApplyRiverTexturing(vec3 pos, vec3 normalWorld, vec4 baseColor)
{
    vec2 uv = fract(pos.xy / 9216.0);
    vec3 riverRoad = SampleRiverSmooth(uv).rgb;
    float river = riverRoad.b;
    float road  = riverRoad.r;
    
    // 하드 threshold 대신 부드러운 블렌딩
    float riverAlpha = smoothstep(0.3, 0.7, river);
    float roadAlpha  = smoothstep(0.3, 0.7, road);
    
    vec4 result = baseColor;

    vec4 roadColor = texture(gTextureHeight4, uv * 1000.0f);


    result = mix(result, vec4(0.0, 0.0, 1.0, 1.0), riverAlpha);
    result = mix(result, vec4(roadColor.rgb, 1.0), roadAlpha);
    return result;
}


//-----------------------------------------------------------------------------
// ⭐ 계곡 텍스처 적용 함수 (부드러운 블렌딩)
//-----------------------------------------------------------------------------
vec4 ApplyValleyTexturing(vec2 uv, vec3 normalWorld, vec3 riverMask, vec4 baseColor)
{
    // riverMask.r 값을 factor로 변환
    // 0.3 이하: 계곡 아님
    // 0.3~0.7: 부드러운 전환
    // 0.7 이상: 완전한 계곡
    float valleyFactor = smoothstep(gValleyBlendStart, gValleyBlendEnd, riverMask.r);
    
    // 계곡이 아닌 영역은 원본 반환
    if (valleyFactor < 0.001) {
        return baseColor;
    }
    
    // 계곡 텍스처 샘플링
    vec4 mossRockColor = texture(gMossRockTexture, uv * gValleyTexScale);     
    mossRockColor *= texture(gDetailMap, uv * 10.0); 

    // 경사도 기반 추가 블렌딩 (선택적)
    // 가파른 경사에서는 계곡 효과를 약화
    float slope = 1.0 - clamp(normalWorld.z, 0.0, 1.0);
    float slopeFactor = smoothstep(0.6, 0.3, slope);  // 경사가 가파를수록 감소
    valleyFactor *= slopeFactor;
    
    // 최종 블렌딩
    return mix(baseColor, mossRockColor, valleyFactor);
}

//-----------------------------------------------------------------------------
// ⭐ 계곡 감지 함수
// 주변 8방향 높이를 샘플링하여 현재 위치가 움푹 들어간 곳인지 판단
//-----------------------------------------------------------------------------
float DetectValleyCurvature(vec2 uv, float currentHeight)
{
    vec2 texelSize = 1.0 / textureSize(heightMapHighRes, 0);
    float h = texelSize.x * gValleyDetectionRadius;
    
    // 9개 포인트 샘플링 (3x3 그리드)
    float h00 = texture(heightMapHighRes, uv + vec2(-h, h)).r;
    float h01 = texture(heightMapHighRes, uv + vec2(0, h)).r;
    float h02 = texture(heightMapHighRes, uv + vec2(h, h)).r;
    
    float h10 = texture(heightMapHighRes, uv + vec2(-h, 0)).r;
    float h11 = currentHeight;  // 중심
    float h12 = texture(heightMapHighRes, uv + vec2(h, 0)).r;
    
    float h20 = texture(heightMapHighRes, uv + vec2(-h, -h)).r;
    float h21 = texture(heightMapHighRes, uv + vec2(0, -h)).r;
    float h22 = texture(heightMapHighRes, uv + vec2(h, -h)).r;
    
    // 2차 미분 계산 (곡률)
    float d2x = h10 - 2.0 * h11 + h12;  // X방향 곡률
    float d2y = h01 - 2.0 * h11 + h21;  // Y방향 곡률
    
    // 평균 곡률 (음수 = 오목, 양수 = 볼록)
    float curvature = (d2x + d2y) * 0.5;
    
    // 음수(오목)일 때만 계곡
    float valleyStrength = smoothstep(-gValleyThreshold, -gValleyThreshold * 2.0, curvature);
    
    return valleyStrength;
}

//-----------------------------------------------------------------------------
// 단층 시스템 (기존 코드)
//-----------------------------------------------------------------------------
vec3 ApplyFaultSystem(vec3 worldPos, out float faultZoneMask)
{
    vec2 faultUV = worldPos.xy * gFaultMapScale;
    vec3 faultData = texture(gFaultMap, faultUV).rgb;
    float displacement = (faultData.r * 2.0 - 1.0) * gFaultDisplacementScale;
    float edgeDistance = faultData.g;
    faultZoneMask = 1.0 - smoothstep(0.0, gFaultZoneWidth, edgeDistance);
    vec3 displaced = worldPos;
    displaced.z += displacement;
    return displaced;
}

//-----------------------------------------------------------------------------
// 트라이플래너 매핑 (기존 코드)
//-----------------------------------------------------------------------------
vec4 GetTriplanarTextureAdvanced(sampler2D tex, vec3 worldPos, vec3 normal, float scale)
{
    float microScale = scale;
    float macroScale = scale * 0.1;
    
    vec3 blendWeights = pow(abs(normal), vec3(4.0));
    blendWeights /= (blendWeights.x + blendWeights.y + blendWeights.z);
    
    vec4 microX = texture(tex, worldPos.zy * microScale);
    vec4 microY = texture(tex, worldPos.xz * microScale);
    vec4 microZ = texture(tex, worldPos.xy * microScale);
    vec4 microColor = microX * blendWeights.x + microY * blendWeights.y + microZ * blendWeights.z;
    
    vec4 macroX = texture(tex, worldPos.zy * macroScale);
    vec4 macroY = texture(tex, worldPos.xz * macroScale);
    vec4 macroZ = texture(tex, worldPos.xy * macroScale);
    vec4 macroColor = macroX * blendWeights.x + macroY * blendWeights.y + macroZ * blendWeights.z;
    
    float dist = length(camera.cameraPos.xyz - worldPos);
    float fade = clamp(dist / 500.0, 0.0, 1.0);
    return mix(microColor, macroColor, fade);
}

//-----------------------------------------------------------------------------
// 지형 텍스처 블렌딩 (기존 코드)
//-----------------------------------------------------------------------------
vec4 BlendTerrainTexturesAdvanced(vec3 worldPos, vec3 normalWorld, float height, float dist)
{
    float worldTexScale = gColorTexcoordScaling * 0.0001; 
    vec2 topDownUV = worldPos.xy * worldTexScale; 

    vec4 heightColor;        
    float Factor = (height - gHeight1) / (gHeight2 - gHeight1);
    heightColor = mix(texture(gTextureHeight2, topDownUV), texture(gTextureHeight3, topDownUV), Factor);
    
    // 높이 기반 기본 색상
    if (height < gHeight0) {
        heightColor = texture(gTextureHeight0, topDownUV);
    } else if (height < gHeight1) {
        float Factor = (height - gHeight0) / (gHeight1 - gHeight0);
        heightColor = mix(texture(gTextureHeight0, topDownUV), texture(gTextureHeight1, topDownUV), Factor);
    } else if (height < gHeight2) {
        float Factor = (height - gHeight1) / (gHeight2 - gHeight1);
        heightColor = mix(texture(gTextureHeight1, topDownUV), texture(gTextureHeight2, topDownUV), Factor);
    } else if (height < gHeight3) {
        float Factor = (height - gHeight2) / (gHeight3 - gHeight2);
        heightColor = mix(texture(gTextureHeight2, topDownUV), texture(gTextureHeight3, topDownUV), Factor);
    } else { 
        float Factor = clamp((height - gHeight3) / (gHeight4 - gHeight3), 0.0, 1.0);
        heightColor = mix(texture(gTextureHeight3, topDownUV), texture(gTextureHeight4, topDownUV), Factor);
    }

    // 경사도 계산
    float slope = 1.0 - clamp(normalWorld.z, 0.0, 1.0);
    float slopeBlend = smoothstep(0.2, 0.5, slope);
    slopeBlend=0.0; // 여기지워야함


    // 바위 텍스처
    vec4 rockColor = GetTriplanarTextureAdvanced(gRockTexture, worldPos, normalWorld, worldTexScale);
    
    // 단층 시스템
    float faultZoneMask;
    vec3 displacedPos = ApplyFaultSystem(worldPos, faultZoneMask);
    float distBlendFactor = 1.0 - smoothstep(200, 300, dist);
    float strataInput = (displacedPos.z + displacedPos.y * 0.2) * 10.0;
    float strataPattern = 0.9 + 0.1 * sin(strataInput) * distBlendFactor;
    float variation = sin(displacedPos.z * 2.0 + displacedPos.x * 0.5) * 0.05;
    strataPattern += variation;
    vec3 strataColor = mix(vec3(0.9, 0.8, 0.7), vec3(1.0, 1.0, 1.0), strataPattern);
    vec3 brecciaColor = vec3(0.45, 0.4, 0.35);
    strataColor = mix(strataColor, brecciaColor, faultZoneMask * gFaultZoneIntensity);
    rockColor.rgb *= strataPattern;
    rockColor.rgb *= strataColor;
    
    // 최종 합성
    vec4 finalColor = mix(heightColor, rockColor, slopeBlend);
    
    if (gIsDetailMap) {
        finalColor *= texture(gDetailMap, topDownUV * 10.0); 
    }
    
    return finalColor;
}

//-----------------------------------------------------------------------------
// 간단한 2D 노이즈 함수
//-----------------------------------------------------------------------------
float hash(vec2 p)
{
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float noise(vec2 p)
{
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    
    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));
    
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}