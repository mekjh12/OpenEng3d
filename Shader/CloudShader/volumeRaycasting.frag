#version 430 core
in vec2 texCoords;
in vec3 viewRay;
in vec3 worldPos;

layout(location = 0) out vec4 fragColor;

uniform vec3 camPosMeter;       // 카메라 위치
uniform float cubeSize;         // 큐브 크기

const vec3 BACKGROUND_COLOR = vec3(0);  // 배경색

/**
 * 프랙탈 노이즈 생성을 위한 회전 및 변환 행렬
 * 이 행렬은 노이즈 패턴에 방향성과 변화를 주어 자연스러운 효과 생성
 */
mat3 m = mat3( 0.00,  0.80,  0.60,  // 첫 번째 행
              -0.80,  0.36, -0.48,  // 두 번째 행
              -0.60, -0.48,  0.64 ); // 세 번째 행

/**
 * 해시 함수 - 입력 숫자로부터 의사난수 생성
 * 
 * @param n 시드 값
 * @return 0.0-1.0 사이의 의사난수 값
 */
float hash(float n)
{
    return fract(sin(n) * 43758.5453); // sin 함수를 사용해 난수화 후 소수 부분만 반환
}

/**
 * Perlin 노이즈 구현 - 3D 공간에서 부드러운 노이즈 생성
 * 
 * @param x 3D 공간 좌표
 * @return 0.0-1.0 사이의 노이즈 값
 */
float noise(in vec3 x)
{
    // 입력 좌표의 정수 부분(격자점)과 소수 부분 분리
    vec3 p = floor(x);
    vec3 f = fract(x);
    
    // 부드러운 보간을 위한 에르미트 보간 함수 적용 (f(t) = 3t² - 2t³)
    f = f*f*(3.0-2.0*f);
    
    // 격자점의 해시 값 계산을 위한 인덱스
    float n = p.x + p.y*57.0 + 113.0*p.z;
    
    // 주변 8개 격자점에서 해시 값을 계산하고 trilinear 보간
    // 1. x축 방향 보간
    float res = mix(
                    mix(
                        mix(hash(n+0.0), hash(n+1.0), f.x),         // y=0, z=0 선상의 두 점 보간
                        mix(hash(n+57.0), hash(n+58.0), f.x),        // y=1, z=0 선상의 두 점 보간
                        f.y                                          // y축 방향으로 보간
                    ),
                    mix(
                        mix(hash(n+113.0), hash(n+114.0), f.x),      // y=0, z=1 선상의 두 점 보간
                        mix(hash(n+170.0), hash(n+171.0), f.x),      // y=1, z=1 선상의 두 점 보간
                        f.y                                          // y축 방향으로 보간
                    ),
                    f.z                                              // z축 방향으로 최종 보간
                );
    return res;
}

/**
 * Fractional Brownian Motion(FBM) - 여러 옥타브의 노이즈를 합성
 * 이 함수는 다양한 스케일의 노이즈를 중첩하여 자연스러운 질감 생성
 * 
 * @param p 3D 공간 좌표
 * @return 합성된 노이즈 값
 */
float fbm(vec3 p)
{
    float f;
    
    // 첫 번째 옥타브 (가장 큰 스케일)
    f  = 0.5000 * noise(p);
    // 회전 및 스케일 변환 적용
    p = m * p * 2.02;
    
    // 두 번째 옥타브 (중간 스케일)
    f += 0.2500 * noise(p);
    // 회전 및 스케일 변환 적용
    p = m * p * 2.03;
    
    // 세 번째 옥타브 (가장 작은 스케일)
    f += 0.1250 * noise(p);
    
    return f;
}

/**
 * 볼륨 렌더링을 위한 구름 밀도 함수
 * 위치에 따라 구름 형태를 정의하는 밀도 값 반환
 * 
 * @param p 3D 공간 좌표
 * @return 해당 위치의 구름 밀도 값
 */
float scene(vec3 p)
{	
    // 기본 구형 형태를 만들고(.1-length(p)*.05) 여기에 노이즈(fbm)를 더해 복잡한 구름 형태 생성
    // - length(p)가 커질수록 값이 작아져서 중심에서 멀어질수록 밀도가 낮아짐
    // - fbm(p*.3)는 프랙탈 노이즈를 통해 불규칙한 세부 구조 추가
    return 0.1 - length(p) * 0.05 + fbm(p * 0.3);
}

/**
 * 점이 렌더링 영역(큐브) 내부에 있는지 확인
 * 
 * @param pos 검사할 3D 위치
 * @return 위치가 큐브 내부면 true, 아니면 false
 */
bool isInCube(vec3 pos) 
{
    // 큐브의 절반 크기
    float halfSize = cubeSize * 0.5;
    
    // 수치 오차를 보정하기 위한 작은 여백(epsilon) 추가
    float epsilon = 0.0001 * cubeSize;
    
    // 모든 축에 대해 위치가 큐브 영역 내에 있는지 검사
    return pos.x >= -halfSize-epsilon && pos.x <= halfSize+epsilon && 
           pos.y >= -halfSize-epsilon && pos.y <= halfSize+epsilon && 
           pos.z >= -halfSize-epsilon && pos.z <= halfSize+epsilon;
}

/**
 * 광선(ray)과 렌더링 영역(큐브)의 교차점 계산
 * 
 * @param rayOrigin 광선의 시작점
 * @param rayDir 광선의 방향
 * @return vec4(교차점 xyz, 교차 여부 w): w가 1.0이면 교차, 0.0이면 미교차
 */
vec4 rayBoxIntersection(vec3 rayOrigin, vec3 rayDir) 
{
    // 큐브의 최소/최대 경계점 계산
    float halfSize = cubeSize * 0.5;
    vec3 boxMin = vec3(-halfSize);
    vec3 boxMax = vec3(halfSize);
    
    // 광선 방향 정규화 및 안전 처리
    vec3 safeRayDir = normalize(rayDir);
    
    // 광선 방향의 역수 계산 (나눗셈 최소화를 위해)
    // 방향 성분이 0에 가까울 경우 무한대 대신 큰 값으로 대체
    vec3 dirInv;
    for(int i = 0; i < 3; i++) {
        dirInv[i] = abs(safeRayDir[i]) < 1e-6 ? 1e10 : 1.0 / safeRayDir[i];
    }
    
    // 각 축별로 큐브의 min/max 면과의 교차 시간(t) 계산
    vec3 t1 = (boxMin - rayOrigin) * dirInv;
    vec3 t2 = (boxMax - rayOrigin) * dirInv;
    
    // 각 축별 진입 시간(tMin)과 탈출 시간(tMax) 정렬
    vec3 tMin = min(t1, t2);
    vec3 tMax = max(t1, t2);
    
    // 실제 큐브 진입 시간은 세 축 중 가장 늦은 진입 시간
    float tNear = max(max(tMin.x, tMin.y), tMin.z);
    // 실제 큐브 탈출 시간은 세 축 중 가장 빠른 탈출 시간
    float tFar = min(min(tMax.x, tMax.y), tMax.z);
    
    // 교차점이 유효한지 확인 (진입 시간이 탈출 시간보다 작고, 탈출 시간이 양수)
    if (tNear < tFar && tFar > 0.0) 
    {
        // 너무 가까운 교차점 방지 (수치 오차 대비)
        tNear = max(0.0001, tNear);
        
        // 교차점 계산
        vec3 intersectionPoint = rayOrigin + safeRayDir * tNear;
    
        // 교차점이 실제로 큐브 내부에 있는지 확인 (경계 케이스 처리)
        if (isInCube(intersectionPoint)) {
            return vec4(intersectionPoint, 1.0); // 교차점 + 교차 플래그(1.0)
        }
    }
    
    // 교차하지 않음
    return vec4(0.0, 0.0, 0.0, 0.0);
}

void main() 
{
    const int nbSample = 64;
    const int nbSampleLight = 6;
    
    float zMax = 40.0;
    float step = zMax/float(nbSample);
    float zMaxl = 20.0;
    float stepl = zMaxl/float(nbSampleLight);
    float absorption = 100.0;
    vec3 sun_direction = normalize(vec3(0.0, 0.0, 1.0));
    
    vec3 ray = normalize(viewRay);   
    
    // 레이마칭 변수 초기화
    vec3 currentPos = camPosMeter;
    vec4 finalColor = vec4(0.0);
    vec3 rayDirection = normalize(viewRay);
    float transmittance = 1.0f; // 광선 투과율(1.0=완전 투명, 0.0=완전 불투명)

    for(int i = 0; i < nbSample; i++)
    {
        float cloudDensity = scene(currentPos);
        if(cloudDensity > 0.0)
        {
            float sampleContribution = cloudDensity / float(nbSample);
            transmittance *= 1.0 - sampleContribution * absorption;
            
            // 투명도가 너무 낮아지면 계산 중단
            if(transmittance <= 0.005f) break;
                
            // 광원으로부터의 빛 투과 계산
            float lightTransmittance = 1.0;
            for(int j = 0; j < nbSampleLight; j++)
            {
                vec3 lightSamplePos = currentPos + normalize(sun_direction) * float(j) * stepl;
                float lightSampleDensity = scene(lightSamplePos);
                if(lightSampleDensity > 0.0)
                    lightTransmittance *= 1.0 - lightSampleDensity * absorption / float(nbSample);
                
                // 빛이 거의 투과되지 않으면 계산 중단
                if(lightTransmittance <= 0.01) break;
            }
            
            // 주변광(환경광)과 직접광(태양광) 합산
            vec4 ambientLight = vec4(1.0) * 50.0 * sampleContribution * transmittance;
            vec4 directLight = vec4(1.0, 0.7, 0.4, 1.0) * 80.0 * sampleContribution * transmittance * lightTransmittance;
            finalColor += ambientLight + directLight;
        }
        currentPos += rayDirection * step;
    }

    fragColor = finalColor;
}