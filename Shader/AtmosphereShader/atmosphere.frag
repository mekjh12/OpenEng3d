// GLSL 버전 4.30을 사용
#version 430

// 원주율 상수 정의
#define M_PI 3.1415926535897932384626433832795

// 버텍스 셰이더로부터 받는 입력 변수들
in vec3 fsPosition;     // 프래그먼트의 월드 공간 위치

// 최종 출력 색상
out vec4 fragColor;

// 유니폼 변수 선언
uniform vec3 viewPos;   // 관찰자(카메라)의 위치
uniform vec3 sunPos;    // 태양의 위치 (광원 방향)

// 광선 샘플링 관련 변수
uniform int viewSamples;    // 시점 광선의 샘플링 수
uniform int lightSamples;   // 광원 광선의 샘플링 수

// 대기 산란 시뮬레이션 매개변수
uniform float I_sun;                // 태양 광원의 강도
uniform float R_e;                  // 행성의 반지름 [m]
uniform float R_a;                  // 대기권의 반지름 [m]
uniform vec3  beta_R;               // Rayleigh 산란 계수
uniform float beta_M;               // Mie 산란 계수
uniform float H_R;                  // Rayleigh 스케일 높이
uniform float H_M;                  // Mie 스케일 높이
uniform float g;                    // Mie 산란 방향성 - 매질의 비등방성
uniform float toneMappingFactor;    // 톤 매핑 적용 강도

/**
 * 광선과 구의 교차점을 계산하는 함수
 * @param o 광선의 시작점
 * @param d 광선의 방향 (단위벡터일 필요는 없다.)
 * @param r 구의 반지름
 * @return 교차점까지의 거리 (t값)
 */
vec2 raySphereIntersection(vec3 o, vec3 d, float r)
{
    // 2차 방정식을 해석적으로 풀이
    // 구는 원점을 중심으로 가정
    // (o+td).(o+td)=r^2
    // (d.d)t^2+2(o.d)t+(o.o-r^2)=0
    // f(t) = at^2 + bt + c

    float a = dot(d, d);
    float b = dot(o, d);
    float c = dot(o, o) - r * r;

    // 판별식 계산
    float discriminant  = (b * b) - (a * c);

    // 교차점이 없는 경우
    if (discriminant  < 0.0) {
        return vec2(100000.0f, -100000.0f);
    }

    float sqrtDet = sqrt(discriminant );

    // 두 교차점 반환
    return vec2((-b - sqrtDet) / a, (-b + sqrtDet) / a);
}

/**
 * 주어진 시점 광선의 하늘 색상을 계산하는 함수
 * @param ray 시점 광선의 방향 단위 벡터
 * @param origin 시점 광선의 시작점
 * @return 계산된 색상
 */
vec3 computeSkyColor(vec3 ray, vec3 origin)
{
    // 광원 방향 정규화
    vec3 sunDir = normalize(sunPos);

    // 대기권과의 교차점 계산 (매개변수 t로 최소, 최대 반환)
    vec2 t = raySphereIntersection(origin, ray, R_a);

    // 후방 교차는 무시 (ray반대방향이면 0>tx>ty)이다.
    if (t.x > t.y) return vec3(0.0, 0.0, 0.0);

    // 샘플링 구간 설정
    t.y = min(t.y, raySphereIntersection(origin, ray, R_e).x);
    float segmentLen = (t.y - t.x) / float(viewSamples);

    float tCurrent = 0.0f;

    // Rayleigh와 Mie 산란 누적값
    vec3 sum_R = vec3(0);
    vec3 sum_M = vec3(0);

    // 광학 깊이 초기화
    float optDepth_R = 0.0;
    float optDepth_M = 0.0;

    // 태양과 광선 방향 사이의 코사인 각도
    float mu = dot(ray, sunDir);
    float mu_2 = mu * mu;
    
    // Rayleigh와 Mie 위상 함수 계산
    float phase_R = 3.0 / (16.0 * M_PI) * (1.0 + mu_2);

    float g_2 = g * g;
    float phase_M = 3.0 / (8.0 * M_PI) * 
                          ((1.0 - g_2) * (1.0 + mu_2)) / 
                          ((2.0 + g_2) * pow(1.0 + g_2 - 2.0 * g * mu, 1.5));

    // 시점 광선을 따라 샘플링
    for (int i = 0; i < viewSamples; ++i)
    {
        // 현재 샘플 위치 계산
        vec3 vSample = origin + ray * (tCurrent + segmentLen * 0.5);
        float height = length(vSample) - R_e;

        if (height>R_a) break;

        // 현재 샘플의 광학 깊이 계산
        float h_R = exp(-height / H_R) * segmentLen;
        float h_M = exp(-height / H_M) * segmentLen;
        optDepth_R += h_R;
        optDepth_M += h_M;

        // 2차 광선(태양 광선) 처리
        float segmentLenLight = 
            raySphereIntersection(vSample, sunDir, R_a).y / float(lightSamples);
        float tCurrentLight = 0.0;

        float optDepthLight_R = 0.0;
        float optDepthLight_M = 0.0;

        // 태양 광선을 따라 샘플링
        for (int j = 0; j < lightSamples; ++j)
        {
            vec3 lSample = vSample + sunDir * 
                           (tCurrentLight + segmentLenLight * 0.5);
            float heightLight = length(lSample) - R_e;
            
            if (heightLight>R_a) break;

            optDepthLight_R += exp(-heightLight / H_R) * segmentLenLight;
            optDepthLight_M += exp(-heightLight / H_M) * segmentLenLight;

            tCurrentLight += segmentLenLight;
        }

        // 광선 감쇠 계산
        vec3 att = exp(-(beta_R * (optDepth_R + optDepthLight_R) + 
                         beta_M * 1.1f * (optDepth_M + optDepthLight_M)));
        
        // 산란 누적
        sum_R += h_R * att;
        sum_M += h_M * att;

        tCurrent += segmentLen;
    }

    return I_sun * (sum_R * beta_R * phase_R + sum_M * beta_M * phase_M);
}

void main()
{   
    

    // 최종 색상 계산
    vec3 acolor = computeSkyColor(normalize(fsPosition - viewPos), viewPos + vec3(0,0,R_e));

    // 톤 매핑 적용
    //acolor = mix(acolor, (1.0 - exp(-1.0 * acolor)), 0.5f);

    // 최종 출력 색상 설정
    fragColor = vec4(acolor, 1.0);
}