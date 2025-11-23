#version 430 core
in vec2 texCoords;
in vec3 viewRay;
in vec3 worldPos;

layout(location = 0) out vec4 fragColor;

uniform vec3 camPosMeter;       // 카메라 위치
uniform float cloudDensity;     // 구름 밀도(3d텍스처로부터의 값에 곱해짐)
uniform vec3 cloudColor;        // 구름 색상
uniform float cubeSize;         // 큐브 크기
uniform vec3 lightDir;          // 광원 방향 (정규화된 벡터)
uniform vec3 lightColor;        // 광원 색상
uniform float lightIntensity;   // 광원 강도
uniform float g;                // Henyey-Greenstein 비대칭 인자 (-1~1)
uniform float cloudBottom;      // 구름 하단 높이
uniform float cloudTop;         // 구름 상단 높이
uniform float coverage;         // 구름 커버리지 (0.0-1.0)

layout(binding = 0) uniform sampler3D cloudTexture;     // 구름 밀도 텍스처
layout(binding = 1) uniform sampler3D shadowTexture;    // 그림자 텍스처


const int MAx_STEPS = 32;               // 최대 레이마칭 단계 수
const int SHADOW_STEPS = 64;            // 그림자 계산용 단계 수
const float STEP_SIZE = 0.05f;          // 레이마칭 스텝 크기
const float SHADOW_STEP_SIZE = 0.02f;   // 그림자 계산용 스텝 크기 (더 크게 설정하여 최적화)
const float DENSITY_THRESHOLD = 0.005;  // 구름 밀도 임계값
const vec3 BACKGROUND_COLOR = vec3(0);  // 배경색
const float AMBIENT_FACTOR = 0.2;       // 주변광 계수

bool isInCube(vec3 pos) {
    float halfSize = cubeSize * 0.5;
    float epsilon = 0.0001 * cubeSize;
    return pos.x >= -halfSize-epsilon && pos.x <= halfSize+epsilon && 
           pos.y >= -halfSize-epsilon && pos.y <= halfSize+epsilon && 
           pos.z >= -halfSize-epsilon && pos.z <= halfSize+epsilon;
}

// 높이 기반 그래디언트 계산
float calculateHeightGradient(float y) {
    // 높이 정규화 (0.0 ~ 1.0)
    float normalizedHeight = (y - cloudBottom) / (cloudTop - cloudBottom);
    normalizedHeight = clamp(normalizedHeight, 0.0, 1.0);
    
    // 아랫쪽과 윗쪽 경계에서 모두 부드럽게 감쇠
    float bottomFade = smoothstep(0.0, 0.1, normalizedHeight);
    float topFade = smoothstep(1.0, 0.9, normalizedHeight);
    
    // 결합된 그래디언트
    return bottomFade * topFade;
}

vec4 rayBoxIntersection(vec3 rayOrigin, vec3 rayDir) {
    float halfSize = cubeSize * 0.5;
    vec3 boxMin = vec3(-halfSize);
    vec3 boxMax = vec3(halfSize);
    
    vec3 safeRayDir = normalize(rayDir);
    vec3 dirInv;
    for(int i = 0; i < 3; i++) {
        dirInv[i] = abs(safeRayDir[i]) < 1e-6 ? 1e10 : 1.0 / safeRayDir[i];
    }
    
    vec3 t1 = (boxMin - rayOrigin) * dirInv;
    vec3 t2 = (boxMax - rayOrigin) * dirInv;
    vec3 tMin = min(t1, t2);
    vec3 tMax = max(t1, t2);
    
    float tNear = max(max(tMin.x, tMin.y), tMin.z);
    float tFar = min(min(tMax.x, tMax.y), tMax.z);
    
    if (tNear < tFar && tFar > 0.0) 
    {
        tNear = max(0.0001, tNear); // 매우 작은 값 방지
        vec3 intersectionPoint = rayOrigin + safeRayDir * tNear;
    
        // 교차점이 실제로 큐브 내부에 있는지 철저히 확인
        if (isInCube(intersectionPoint)) {
            return vec4(intersectionPoint, 1.0);
        }
    }
    return vec4(0.0, 0.0, 0.0, 0.0);
}

vec3 worldToTexCoord(vec3 worldPos) {
    float halfSize = cubeSize * 0.5;
    return (worldPos + vec3(halfSize)) / cubeSize;
}

float sampleCloudDensity(vec3 pos) {
    vec3 texCoord = worldToTexCoord(pos);
    if(texCoord.x < 0.0 || texCoord.x > 1.0 || 
       texCoord.y < 0.0 || texCoord.y > 1.0 || 
       texCoord.z < 0.0 || texCoord.z > 1.0) {
        return 0.0;
    }
    
    // 노이즈 값 가져오기
    vec4 noiseValue = texture(cloudTexture, texCoord);
    
    return noiseValue.r;

    // Worley 노이즈의 여러 채널 조합
    float f1 = noiseValue.r; // 가장 가까운 거리
    float f2 = noiseValue.g; // 두 번째로 가까운 거리
    float f2_f1 = noiseValue.b; // 경계 강조
    
    // 구름 형태 계산
    float baseShape = f1;
    float edges = pow(f2_f1, 2.0); // 셀 경계 강화
    
    // 커버리지 적용
    float actualCoverage = 1.0 - coverage; // 예: coverage가 0.65이면 actualCoverage는 0.35
    float density = smoothstep(actualCoverage, 1.0, baseShape) * edges;
    
    // 높이 그래디언트 적용
    float heightGradient = calculateHeightGradient(pos.y);
    density *= heightGradient;
    
    // 최종 밀도 계산
    return heightGradient;
}

// 기존 calculateLightTransmittance 함수 대신 사용할 함수
float getShadowTransmittance(vec3 pos) {
    vec3 texCoord = worldToTexCoord(pos);
    if(texCoord.x < 0.0 || texCoord.x > 1.0 || 
       texCoord.y < 0.0 || texCoord.y > 1.0 || 
       texCoord.z < 0.0 || texCoord.z > 1.0) {
        return 1.0; // 큐브 외부는 완전 투명
    }
    return texture(shadowTexture, texCoord).r;
}

// Henyey-Greenstein 위상 함수
float phaseHG(float cosTheta, float g) {
    float g2 = g * g;
    return (1.0 - g2) / (4.0 * 3.14159265 * pow(1.0 + g2 - 2.0 * g * cosTheta, 1.5));
}

// 광원 방향으로의 투과율 계산 (그림자)
float calculateLightTransmittance(vec3 position, vec3 direction) {
    float lightTransmittance = 1.0;
    float shadowStepSize = SHADOW_STEP_SIZE * (cubeSize / 20.0);
    
    // 현재 위치에서 광원 방향으로 레이마칭
    for (int i = 0; i < SHADOW_STEPS; i++) {
        position += direction * shadowStepSize;
        
        // 큐브를 벗어났는지 확인
        if (!isInCube(position)) break;
        
        // 밀도 샘플링
        float density = sampleCloudDensity(position);
        
        // Beer-Lambert 법칙으로 빛 감쇠 계산
        if (density > 0.0001) {
            float absorption = exp(-density * shadowStepSize);
            lightTransmittance *= absorption;
            
            // 최적화: 거의 완전히 감쇠되면 계산 중단
            if (lightTransmittance < 0.01) {
                lightTransmittance = 0.01; // 최소값 설정 (완전한 검은색 방지)
                break;
            }
        }
    }
    
    return lightTransmittance;
}

void main() 
{
    vec3 ray = normalize(viewRay);
    bool isInside = isInCube(camPosMeter);
    
    // 시작점 결정
    vec3 startPos;
    if (isInside) {
        // 내부에 있으면 카메라 위치에서 시작
        startPos = camPosMeter;
    } else {
        // 외부에 있으면 교차점 계산
        vec4 intersection = rayBoxIntersection(camPosMeter, ray);
        
        if (intersection.w < 0.5) {
            // 교차점이 없으면 배경색 반환하고 종료
            fragColor = vec4(BACKGROUND_COLOR, 1.0);
            return;
        }
        
        // 유효한 교차점을 시작점으로 사용
        startPos = intersection.xyz;
    }
    
    // 레이마칭 변수 초기화
    vec3 pos = startPos;
    float transmittance = 1.0;
    vec3 finalColor = vec3(0.0);
    
    // 조정된 스텝 크기
    float adjustedStepSize = STEP_SIZE * (cubeSize / 20.0);
    
    // 레이마칭 루프
    bool foundCloud = false; // 구름을 찾았는지 추적
    
    // 시점 방향과 광원 방향 사이의 각도의 코사인 계산 (위상 함수용)
    float cosTheta = dot(ray, lightDir);    
    
    // 위상 함수 평가
    float phase = phaseHG(cosTheta, g);
    
    // 광원 색상과 세기 조정
    vec3 ambientLight = cloudColor * AMBIENT_FACTOR;

    for (int i = 0; i < MAx_STEPS; i++) 
    {
        // 큐브를 벗어났는지 확인
        if (!isInCube(pos)) break;
        
        // 현재 위치에서 구름 밀도 샘플링
        float density = sampleCloudDensity(pos);
        float threshold = 0.001;
        
        if (density > threshold) 
        {
            foundCloud = true; // 구름 발견
            
            // 흡수 및 누적 색상 계산
            float absorption = exp(-density * adjustedStepSize);
            transmittance *= absorption;
            
            // ------- 단일 산란 계산 시작 -------
            
            // 현재 지점에서 광원 방향으로의 투과율 계산 (그림자)
            //float lightTrans = calculateLightTransmittance(pos, lightDir);
            float lightTrans = getShadowTransmittance(pos);
                        
            // 산란 색상 계산
            float scatteringFactor = density * adjustedStepSize * 2.0;
            
            // 주변광과 직접광의 조합
            vec3 directLight = lightColor * lightIntensity * phase * lightTrans;
            vec3 totalLight = ambientLight + directLight;
            
            // 최종 색상에 기여도 추가
            finalColor += totalLight * scatteringFactor * (isInside ? 1.5 : 1.0);
            
            // ------- 단일 산란 계산 끝 -------
            
            // 거의 불투명해지면 종료
            if (transmittance < 0.01) break;
        }
        
        // 다음 위치로 이동
        pos += ray * adjustedStepSize;
    }
    
    // 배경색과 합성
    finalColor += BACKGROUND_COLOR * transmittance;
    
    // 색상 출력
    if (length(finalColor) < 0.01 && foundCloud) {
        // 구름이 있었지만 색상이 너무 어두운 경우에만 디버그 색상
        //finalColor = isInside ? vec3(1.0, 0.0, 1.0) : vec3(0.5, 0.0, 0.0);
    }
    
    fragColor = vec4(finalColor, 1.0);
}