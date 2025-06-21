#version 430 core

#include "./../includes/lib_fog_effect.glsl"

// 상수 정의
#define M_PI 3.1415926535897932384626433832795

// 입력 변수
in vec2 texCoords;
in vec3 viewRay;
in vec3 worldPos;

// 출력 변수
layout(location = 0) out vec4 fragColor;

// 대기 매개변수
uniform float R_e;             // 지구 반지름(km)
uniform float R_a;             // 대기 반지름(km)
uniform vec3 beta_R;           // Rayleigh 산란 계수
uniform float beta_M;          // Mie 산란 계수
uniform float H_R;             // Rayleigh 스케일 높이
uniform float H_M;             // Mie 스케일 높이
uniform float g;               // Mie 비대칭 계수
uniform vec3 sunPos;           // 태양 위치
uniform vec3 I_sun;            // 태양 강도
uniform int viewSamples;       // 뷰 광선 샘플 수
uniform vec3 camPosMeter;

// 태양 디스크 관련 유니폼 추가
uniform float sunDiskSize;      // 태양 디스크 크기 (각도)
uniform vec3 sunDiskColor;      // 태양 디스크 색상

// 일출/일몰 색상 효과 관련 유니폼 추가
uniform float sunsetFactor;    // 일출/일몰 효과 강도 조절 (0-1)
uniform vec3 sunsetColor;      // 일출/일몰 주 색상 (기본값: 붉은 오렌지색)

// LUT 텍스처
layout(binding = 0) uniform sampler2D transmittanceLUT;  // 투과율 LUT

// 안개 관련 uniform 블록
layout(std140) uniform HalfFogUniforms
{
    vec4  halfFogPlane;         // 0..15
    vec3  halfFogColor;         // 16..31 (실제로 16바이트)
    float padding1;             // 28-31 바이트 (vec3 패딩)
    float halfFogDensity;       // 32..35
    int   isHalfFogEnabled;     // 36..39
    float padding2;             // 4 바이트
    float padding3;             // 4 바이트 48바이트
};

// 거리 기반 안개를 위한 UBO 정의 (필요한 변수만 포함)
layout(std140, binding=2) uniform DistanceFogUniforms
{
    vec3  distFogCenter;     // 0-11
    float distFogMinRadius;  // 12-15
    float distFogMaxRadius;  // 16-19
    int   distFogEnabled;    // 20-23
    float distFogPadding1;   // 24-27
    float distFogPadding2;   // 28-31
};

// 추가 유니폼 변수
layout(binding = 1) uniform sampler3D cloudTexture;   // 3D 구름 텍스처
uniform float cloudCoverage;      // 구름 커버리지 (0-1)
uniform float cloudDensity;       // 구름 밀도
uniform float cloudHeight;        // 구름 높이 (km)
uniform float cloudThickness;     // 구름 두께 (km)
uniform int enableClouds;         // 구름 활성화 여부
uniform float cloudBrightness;    // 구름 밝기
uniform float cloudShadowStrength; // 구름 그림자 강도


vec3 computeSkyColor(vec3 ray, vec3 origin);
vec3 computeEarthColor(vec3 pos);
vec2 raySphereIntersection(vec3 o, vec3 d, float r);
vec2 getTransmittanceUV(float height, float mu);
vec3 getTransmittance(float height, float mu);
vec3 applySunsetColors(vec3 rayDir, vec3 sunDir, vec3 skyColor);
vec3 renderSunDisk(vec3 rayDir, vec3 sunDir, vec3 skyColor);
vec3 applyLensFlare(vec3 rayDir, vec3 sunDir, vec3 color);
vec4 raymarchClouds(vec3 rayOrigin, vec3 rayDir, float startDist, float endDist);
float sampleCloudDensity(vec3 positionKm);

// 메인 함수
void main() 
{ 
    // 뷰 레이 정규화 및 태양 방향 정규화
    vec3 ray = normalize(viewRay);

    // 카메라 위치 킬로미터 변환 & 원점이 행성의 중심
    vec3 cameraPosKm = camPosMeter * 0.001 + vec3(0, 0, R_e);

    // 카메라 위치 (origin)에서 ray 방향으로 하늘 색상 계산
    vec3 skyColor = computeSkyColor(ray, cameraPosKm);
        
    // 일출/일몰 색상 추가
    skyColor = applySunsetColors(ray, normalize(sunPos), skyColor);
    
    // 태양 디스크 추가
    vec3 finalColor = renderSunDisk(ray, normalize(sunPos), skyColor);
    
    // 구름 렌더링
    // 구름층과의 교차점 계산
    float cloudLayerInner = R_e + 5.0; // 5km 높이에서 구름 시작
    float cloudLayerOuter = cloudLayerInner + 2.0; // 2km 두께의 구름층

    vec2 cloudIntersectInner = raySphereIntersection(cameraPosKm, ray, cloudLayerInner);
    vec2 cloudIntersectOuter = raySphereIntersection(cameraPosKm, ray, cloudLayerOuter);

    // 카메라가 구름층 아래에 있는 경우
    if(length(cameraPosKm) < cloudLayerInner)
    {
        float startDist = cloudIntersectInner.y;
        float endDist = cloudIntersectOuter.y;
    
        vec4 clouds = raymarchClouds(cameraPosKm, ray, startDist, endDist);
        
        //clouds = vec4((endDist-startDist)/160.0f,0,0,1);

        // 구름과 하늘색 혼합
        finalColor = mix(finalColor, clouds.rgb, clouds.a);
    }

    // 톤 매핑 (HDR -> LDR)
    finalColor = finalColor / (finalColor + vec3(1.0));
    
    // 감마 보정
    finalColor = pow(finalColor, vec3(1.0 / 2.2));
    
    // 반평면 안개 적용
    if (isHalfFogEnabled == 1.0f)
    {
        finalColor = CalculateAndApplyFog(
                finalColor,
                halfFogColor,
                halfFogDensity,
                camPosMeter,
                worldPos.xyz,
                halfFogPlane
            );
    }
    
    // 최종 색상 출력
    fragColor = vec4(finalColor, 1.0);
}


// 단순화된 구름 광선 행진 함수
vec4 raymarchClouds(vec3 rayOrigin, vec3 rayDir, float startDist, float endDist) 
{
    // 광선 행진 파라미터
    const int CLOUD_STEPS = 64;
    const int LIGHT_STEPS = 6;
    
    float stepSize = (endDist - startDist) / float(CLOUD_STEPS);
    
    // 투과율과 누적된 구름 색상
    vec3 transmittance = vec3(1.0);
    vec3 cloudColor = vec3(0.0);
    
    // 광선 행진 시작
    for(int i = 0; i < CLOUD_STEPS; ++i) 
    {
        // 현재 위치 계산
        float t = startDist + stepSize * (float(i) + 0.5);
        vec3 pos = rayOrigin + rayDir * t;
        
        // 구름 밀도 샘플링
        float density = sampleCloudDensity(pos);
        
        if(density > 0.001) {
            // 빛 투과 계산 (태양 방향)
            float lightDensity = 0.0;
            vec3 lightStep = normalize(sunPos) * stepSize * 2.0;
            vec3 lightPos = pos;
            
            // 빛 방향으로 투과 계산
            for(int j = 0; j < LIGHT_STEPS; ++j) {
                lightPos += lightStep;
                lightDensity += sampleCloudDensity(lightPos);
            }
            
            // 빛 투과율 계산 (그림자 강도 0.2 고정)
            vec3 sunTransmittance = vec3(exp(-lightDensity * 0.2));
            
            // Beer's Law 기반 투과율 감소
            float extinction = density * stepSize;
            vec3 cloudExtinction = vec3(exp(-extinction));
            
            // 산란 계산
            vec3 inScatter = (vec3(1.0) - cloudExtinction) * sunTransmittance * transmittance;
            
            // 구름 색상 업데이트 (밝기 1.2 고정)
            cloudColor += inScatter * vec3(1.2);
            
            // 투과율 업데이트
            transmittance *= cloudExtinction;
            
            // 투과율이 매우 낮으면 조기 종료
            if(transmittance.r < 0.01) break;
        }
    }
    
    // 최종 색상 및 불투명도 반환
    return vec4(cloudColor, 1.0 - transmittance.r);
}

// 단순화된 구름 볼륨 밀도 샘플링 함수
float sampleCloudDensity(vec3 positionKm) 
{
    // 지구 표면으로부터의 높이 계산
    float heightAboveEarth = length(positionKm) - R_e;
    
    // 구름층 높이 범위 확인 (5km~7km로 고정)
    float cloudLayerMin = 5.0; // 구름 시작 높이 (km)
    float cloudLayerMax = 7.0; // 구름 끝 높이 (km)
    
    if(heightAboveEarth < cloudLayerMin || heightAboveEarth > cloudLayerMax) {
        return 0.0; // 구름층 밖은 밀도 0
    }

    // 높이에 따른 감쇠 (구름층 경계에서 부드럽게)
    float heightGradient = 1.0 - 2.0 * abs((heightAboveEarth - cloudLayerMin) / (cloudLayerMax - cloudLayerMin) - 0.5);
    heightGradient = smoothstep(0.0, 1.0, heightGradient);
    
    // 3D 텍스처 좌표 계산
    vec3 normalizedPos = normalize(positionKm);

    vec3 s = vec3(positionKm.x * 0.003f, positionKm.y * 0.003f, (heightAboveEarth-5.0f)*0.5f);

    // 3D 노이즈 텍스처 샘플링
    vec4 noiseValue = texture(cloudTexture, s);
    
    return noiseValue.r;
    
    // Worley 노이즈의 여러 채널 조합
    float f1 = noiseValue.r; // 가장 가까운 거리
    float f2 = noiseValue.g; // 두 번째로 가까운 거리
    float f2_f1 = noiseValue.b; // 경계 강조
    
    // 구름 형태 계산
    float baseShape = f1;
    float edges = pow(f2_f1, 2.0); // 셀 경계 강화
    
    // 커버리지 적용 (고정 값 0.65 사용)
    float coverage = 0.35; // 1.0 - 0.65
    float density = smoothstep(coverage, 1.0, baseShape) * edges;
    
    // 높이 그래디언트 적용
    density *= heightGradient;
    
    // 최종 밀도 반환 (고정 밀도값 0.5 사용)
    return density * 0.5;
}

// 일출/일몰 색상 효과 함수
vec3 applySunsetColors(vec3 rayDir, vec3 sunDir, vec3 skyColor) 
{
    // 태양과 지평선 사이의 각도 (라디안)
    float sunElevation = asin(sunDir.z);
    
    // 지평선 위 태양 고도 각도를 0~1 범위로 매핑 (0: 지평선, 1: 15도 이상)
    float elevationFactor = smoothstep(-0.05, 0.25, sunElevation);
    
    // 일출/일몰 강도 (태양이 지평선에 있을 때 최대)
    float sunsetStrength = (1.0 - elevationFactor) * sunsetFactor;
    
    // 일출/일몰 방향에 따른 강도 조절 (태양 반대편도 약간 색상 변화)
    float dirFactor = dot(normalize(vec3(rayDir.xy, 0.0)), normalize(vec3(sunDir.xy, 0.0)));
    dirFactor = pow(max(0.0, dirFactor), 1.5); // 태양 방향에 더 강한 효과
    
    // 고도에 따른 색상 효과 (고도가 높을수록 약한 효과)
    float heightFactor = 1.0 - min(1.0, rayDir.z * 2.0);
    
    // 지평선 강화 효과 (지평선 부근에서 더 강한 효과)
    float horizonFactor = 1.0 - abs(rayDir.z);
    horizonFactor = pow(horizonFactor, 8.0);
    
    // 최종 일출/일몰 효과 강도
    float finalSunsetFactor = sunsetStrength * dirFactor * heightFactor;
    
    // 지평선 부근 추가 강화
    finalSunsetFactor = mix(finalSunsetFactor, max(finalSunsetFactor, horizonFactor * sunsetStrength), 0.5);
    
    // 일출/일몰 색상 계산
    vec3 baseColor = sunsetColor; // 기본 일몰 색상 (붉은 오렌지색)
    
    // 태양과의 각도에 따라 약간 다른 색상 (태양 가까이는 노란색, 멀리는 분홍/보라색)
    vec3 sunsetNearColor = vec3(1.0, 0.6, 0.2); // 태양 근처 (노란색/주황색)
    vec3 sunsetFarColor = vec3(0.8, 0.3, 0.5);  // 태양 반대편 (분홍/보라색)
    
    vec3 gradientColor = mix(sunsetFarColor, sunsetNearColor, dirFactor);
    
    // 최종 색상 계산 (하늘 색상과 일출/일몰 색상 혼합)
    vec3 finalColor = mix(skyColor, gradientColor, finalSunsetFactor);
    
    // 밝기 조정 (일출/일몰 색상이 너무 어두워지지 않도록)
    float luminance = dot(skyColor, vec3(0.299, 0.587, 0.114));
    float targetLuminance = max(luminance, 0.2);
    float colorLuminance = dot(finalColor, vec3(0.299, 0.587, 0.114));
    
    // 휘도 보존 보정
    if (colorLuminance > 0.0) {
        finalColor *= targetLuminance / max(colorLuminance, 0.1);
    }    

    return finalColor;
}

// 태양 디스크 렌더링 함수
vec3 renderSunDisk(vec3 rayDir, vec3 sunDir, vec3 skyColor) 
{
    float cosAngle = dot(rayDir, sunDir);
    float sunAngularSize = cos(sunDiskSize * 0.5 * 0.0174533); // 도에서 라디안으로 변환
    
    // 태양 디스크
    if (cosAngle > sunAngularSize) {
        float sunFactor = smoothstep(sunAngularSize, mix(sunAngularSize, 1.0, 0.05), cosAngle);
        return mix(skyColor, sunDiskColor * I_sun, sunFactor);
    }
    
    // 태양 광륜(코로나) 효과
    float coronaFactor = pow(max(0.0, cosAngle), 128.0);
    vec3 coronaColor = sunDiskColor * coronaFactor * 2.0;
    
    return skyColor + coronaColor;
}

// 하늘 색상 계산
vec3 computeSkyColor(vec3 rayNormal, vec3 originKm) 
{
    // 광원 방향 정규화
    vec3 sunDir = normalize(sunPos);

    // 대기권 경계와의 교차점
    vec2 atmosphereIntersect = raySphereIntersection(originKm, rayNormal, R_a);
    if (atmosphereIntersect.x > atmosphereIntersect.y) return vec3(0);

    // 행성 표면과의 교차점
    vec2 planetIntersect = raySphereIntersection(originKm, rayNormal, R_e);

    // 샘플링 범위 설정 (대기 시작 ~ 행성 표면 또는 대기 끝)
    float startDist = max(0.0f, atmosphereIntersect.x);
    float endDist = planetIntersect.x > 0 ? 
                    min(atmosphereIntersect.y, planetIntersect.x) : 
                    atmosphereIntersect.y;

    float segmentLen = (endDist - startDist) / float(viewSamples);
    float tCurrent = startDist;

    // Rayleigh와 Mie 산란 누적값
    vec3 sum_R = vec3(0);
    vec3 sum_M = vec3(0);

    // 시점 광선을 따라 샘플링
    for (int i = 0; i < viewSamples; ++i) 
    {
        // 현재 샘플 위치 계산        
        vec3 vSample = originKm + rayNormal * (tCurrent + segmentLen * 0.5f);
        float height = length(vSample) - R_e;
        
        if (height > R_a - R_e) break;  // 대기권 밖으로 나가면 중단

        // 카메라에서 현재 샘플까지의 투과율 (수정된 부분)
        vec3 transmittanceView;
        
        // 광학 깊이를 이용한 접근법: 두 지점 사이의 정확한 투과율 계산
        if (length(originKm) - R_e <= 0.0001) 
        {
            // 카메라가 지표면에 있는 경우 (거의 0에 가까움)
            float viewMu = dot(normalize(vSample - originKm), rayNormal);
            float cameraDist = length(originKm) - R_e;
            float sampleDist = height;
            
            // 카메라에서 샘플 방향으로 대기권 끝까지의 투과율
            transmittanceView = getTransmittance(cameraDist, viewMu);
        } 
        else 
        {
            // 카메라와 샘플 사이의 투과율 계산
            vec3 sampleToCamera = originKm - vSample;
            float distToCamera = length(sampleToCamera);
            vec3 dirToCamera = sampleToCamera / distToCamera;
            
            // 샘플에서 카메라 방향으로의 각도
            float sampleMu = dot(normalize(sampleToCamera), dirToCamera);
            
            // 샘플에서 카메라까지의 광학 깊이를 근사
            // 중간 지점에서 대기 밀도 추정
            float midHeight = (height + (length(originKm) - R_e)) * 0.5;
            float opticalDepthRM = exp(-midHeight / H_R) * distToCamera; // Rayleigh
            float opticalDepthMM = exp(-midHeight / H_M) * distToCamera; // Mie
            
            // 광학 깊이에서 투과율 계산
            transmittanceView = exp(-(
                beta_R * opticalDepthRM + 
                beta_M * opticalDepthMM * vec3(1.0)
            ));
        }
        
        // 현재 샘플에서 태양까지의 투과율 (기존 코드 유지)
        float lightMu = dot(normalize(vSample), sunDir);
        vec3 transmittanceLight = getTransmittance(height, lightMu);
        
        // 결합된 투과율
        vec3 transmittance = transmittanceView * transmittanceLight;
        
        // 현재 샘플의 밀도 계산
        float h_R = exp(-height / H_R) * segmentLen;
        float h_M = exp(-height / H_M) * segmentLen;

        // 산란 누적
        sum_R += h_R * transmittance;
        sum_M += h_M * transmittance;

        tCurrent += segmentLen;
    }
    
    // 위상 함수 계산
    float mu = dot(rayNormal, sunDir);
    float mu_2 = mu * mu;
    float phase_R = 3.0 / (16.0 * M_PI) * (1.0 + mu_2);
    
    float g_2 = g * g;
    float phase_M = 3.0 / (8.0 * M_PI) * 
                          ((1.0 - g_2) * (1.0 + mu_2)) / 
                          ((2.0 + g_2) * pow(1.0 + g_2 - 2.0 * g * mu, 1.5));
          
    // 대기 색상 계산
    vec3 dayColor = I_sun * (sum_R * beta_R * phase_R + sum_M * beta_M * phase_M);

    return dayColor;
}

// 광선과 구의 교차점을 계산
vec2 raySphereIntersection(vec3 o, vec3 d, float r) 
{
    float a = dot(d, d);
    float b = 2.0 * dot(o, d);
    float c = dot(o, o) - r * r;
    float discriminant = b * b - 4.0 * a * c;
    
    if (discriminant < 0.0) { // 허근
        return vec2(100000.0, -100000.0);
    }
    
    float sqrtDet = sqrt(discriminant);
    return vec2((-b - sqrtDet) / (2.0 * a), (-b + sqrtDet) / (2.0 * a));
}

// 높이와 시야각에서 LUT UV 좌표를 계산, Transmittance는 투과율
vec2 getTransmittanceUV(float height, float mu) 
{
    // 높이를 [0,1] 범위로 정규화
    float h = (height - R_e) / (R_a - R_e);
    
    // mu를 수평선 기준으로 [0,1] 범위 매핑
    float horizonMu = -sqrt(1.0 - (R_e * R_e) / (height * height));
    
    float x;
    if (mu < horizonMu) {
        // 수평선 아래
        x = (mu - (-1.0)) / (horizonMu - (-1.0)) * 0.5;
    } else {
        // 수평선 위
        x = 0.5 + (mu - horizonMu) / (1.0 - horizonMu) * 0.5;
    }
    
    return vec2(x, 1.0f - h);
}

// LUT에서 투과율 조회
vec3 getTransmittance(float height, float mu) 
{
    vec2 uv = getTransmittanceUV(height, mu);
    return texture(transmittanceLUT, uv).rgb;
}

// 지구 표면 색상 계산
vec3 computeEarthColor(vec3 pos) 
{
    // 간단한 지구 표면 색상 구현
    // 바다(파란색)와 육지(초록색) 패턴
    vec2 latLong = vec2(
        atan(pos.z, pos.x),
        asin(pos.y / R_e)
    );
    
    // 간단한 패턴 생성
    float pattern = sin(latLong.x * 5.0) * cos(latLong.y * 3.0);
    
    // 바다와 육지 색상 혼합
    vec3 oceanColor = vec3(0.1, 0.2, 0.4);
    vec3 landColor = vec3(0.1, 0.3, 0.1);
    
    return mix(oceanColor, landColor, smoothstep(-0.2, 0.2, pattern));
}
