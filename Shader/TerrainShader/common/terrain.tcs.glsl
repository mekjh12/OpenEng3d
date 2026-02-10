//-----------------------------------------------------------------------------
// 테셀레이션 제어 셰이더 (Tessellation Control Shader)
// 거리 기반 적응형 테셀레이션을 구현
//-----------------------------------------------------------------------------
#version 430

// 패치당 4개의 제어점 출력 설정
layout(vertices = 4) out;
layout(std140, binding = 0) uniform CameraBlock {mat4 view; mat4 proj; mat4 vp; vec4 cameraPos;} camera;

// 버텍스 셰이더로부터의 입력
in vec2 Tex1[];           // 입력 텍스처 좌표 배열
out vec2 Tex2[];          // 출력 텍스처 좌표 배열

// 변환 행렬 및 설정
uniform mat4 model;       // 모델 행렬
uniform float heightScale = 200.0f; // TES와 동일하게 맞춰주세요 (높이 고려용)

// ----------------------------------------------------------------------------
// 헬퍼 함수: 패치가 시야(Frustum) 안에 있는지 검사
// ----------------------------------------------------------------------------
bool IsPatchVisible(vec4 p0, vec4 p1, vec4 p2, vec4 p3)
{
    // 패치의 4개 모서리 (바닥면: 높이 0)
    vec4[] corners = vec4[](p0, p1, p2, p3);
    
    // 검사할 총 8개의 점 (바닥면 4개 + 최대 높이면 4개)
    // 지형이 솟아오를 수 있으므로 heightScale만큼 z축으로 올린 가상의 천장도 검사해야 함
    // 주의: TES에서 p.z = heightScale * Height 로 쓰므로, Z축이 Up 벡터라고 가정
    vec4 upVec = vec4(0.0, 0.0, heightScale, 0.0); 

    // 모든 점이 Frustum의 특정 평면(예: 왼쪽) 밖에 있는지 카운트
    // 6개 평면: Left, Right, Bottom, Top, Near, Far
    int outCode[6] = int[](0, 0, 0, 0, 0, 0);

    for(int i = 0; i < 4; ++i)
    {
        // 바닥점과 천장점 2개를 동시에 검사
        vec4 points[2];
        points[0] = camera.vp * model * corners[i];          // 바닥 (Height 0)
        points[1] = camera.vp * model * (corners[i] + upVec); // 천장 (Max Height)

        for(int k = 0; k < 2; ++k)
        {
            vec4 p = points[k];
            // Clip Space 범위: -w <= x,y,z <= w
            // 약간의 여유(Margin)를 주어 팝핑 현상 방지 (+ p.w * 0.1)
            if(p.x < -p.w) outCode[0]++; // Left
            if(p.x >  p.w) outCode[1]++; // Right
            if(p.y < -p.w) outCode[2]++; // Bottom
            if(p.y >  p.w) outCode[3]++; // Top
            if(p.z < -p.w) outCode[4]++; // Near
            if(p.z >  p.w) outCode[5]++; // Far
        }
    }

    // 만약 8개의 점이 모두 특정 평면의 바깥쪽에 있다면, 이 패치는 완전히 안 보임
    for(int i = 0; i < 6; ++i)
    {
        if(outCode[i] == 8) return false; // Culled
    }

    return true; // Visible
}

void main()
{
    // 1. 통과 데이터 처리 (모든 Invocation에서 수행)
    gl_out[gl_InvocationID].gl_Position = gl_in[gl_InvocationID].gl_Position;
    Tex2[gl_InvocationID] = Tex1[gl_InvocationID];

    // 2. 테셀레이션 레벨 계산 (Invocation 0 에서만 수행하여 부하 감소)
    if (gl_InvocationID == 0)
    {
        // --- Frustum Culling 로직 ---
        if (!IsPatchVisible(gl_in[0].gl_Position, gl_in[1].gl_Position, 
                            gl_in[2].gl_Position, gl_in[3].gl_Position))
        {
            // 시야 밖이면 레벨을 음수나 0으로 설정 -> GPU가 해당 패치 폐기
            gl_TessLevelOuter[0] = -1.0;
            gl_TessLevelOuter[1] = -1.0;
            gl_TessLevelOuter[2] = -1.0;
            gl_TessLevelOuter[3] = -1.0;
            gl_TessLevelInner[0] = -1.0;
            gl_TessLevelInner[1] = -1.0;
            return;
        }
        // ---------------------------

        // 모델-뷰 변환 행렬
        mat4 gView = camera.view * model;

        // 각 정점을 뷰 공간으로 변환
        vec4 ViewSpacePos00 = gView * gl_in[0].gl_Position; 
        vec4 ViewSpacePos01 = gView * gl_in[1].gl_Position; 
        vec4 ViewSpacePos10 = gView * gl_in[2].gl_Position; 
        vec4 ViewSpacePos11 = gView * gl_in[3].gl_Position; 

        // 뷰 공간 거리 계산
        float Len00 = length(ViewSpacePos00.xyz);
        float Len01 = length(ViewSpacePos01.xyz);
        float Len10 = length(ViewSpacePos10.xyz);
        float Len11 = length(ViewSpacePos11.xyz);

        const float MIN_DISTANCE = 20.0;   // 거리 조절 (너무 가까우면 과부하)
        const float MAX_DISTANCE = 800.0;  // 거리 조절

        // 거리 정규화 [0, 1]
        float d00 = clamp((Len00 - MIN_DISTANCE) / (MAX_DISTANCE - MIN_DISTANCE), 0.0, 1.0);
        float d01 = clamp((Len01 - MIN_DISTANCE) / (MAX_DISTANCE - MIN_DISTANCE), 0.0, 1.0);
        float d10 = clamp((Len10 - MIN_DISTANCE) / (MAX_DISTANCE - MIN_DISTANCE), 0.0, 1.0);
        float d11 = clamp((Len11 - MIN_DISTANCE) / (MAX_DISTANCE - MIN_DISTANCE), 0.0, 1.0);

        const float MIN_TESS = 4.0;   // 멀리 있을 때 최소 레벨
        const float MAX_TESS = 64.0;  // 가까이 있을 때 최대 레벨

        // 엣지 레벨 계산 (mix 함수로 선형 보간)
        float tessLevel0 = mix(MAX_TESS, MIN_TESS, min(d10, d00)); // Left
        float tessLevel1 = mix(MAX_TESS, MIN_TESS, min(d00, d01)); // Bottom
        float tessLevel2 = mix(MAX_TESS, MIN_TESS, min(d01, d11)); // Right
        float tessLevel3 = mix(MAX_TESS, MIN_TESS, min(d11, d10)); // Top

        // 상수 정의
        const float EDGE_THRESHOLD = 0.001f;
        const float OPPOSITE_EDGE = 0.999f;

        // 엣지 케이스: 여기서는 모서리를 포함한 모든 엣지 케이스 처리
        vec2 texCoord = Tex2[gl_InvocationID];
        bool isRightEdge = texCoord.x > OPPOSITE_EDGE;
        bool isLeftEdge = texCoord.x < EDGE_THRESHOLD;
        bool isTopEdge = texCoord.y > OPPOSITE_EDGE;
        bool isBottomEdge = texCoord.y < EDGE_THRESHOLD;

        // 레벨 적용
        gl_TessLevelOuter[0] = tessLevel0;
        gl_TessLevelOuter[1] = tessLevel1;
        gl_TessLevelOuter[2] = tessLevel2;
        gl_TessLevelOuter[3] = tessLevel3;

        gl_TessLevelInner[0] = max(tessLevel1, tessLevel3);
        gl_TessLevelInner[1] = max(tessLevel0, tessLevel2);
    }
}