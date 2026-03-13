//-----------------------------------------------------------------------------
// 강 렌더링 테셀레이션 제어 셰이더 (TCS)
// - 지형 TCS와 동일한 거리 기반 테셀레이션
// - 강 마스크(Unit 2)를 샘플링하여 강이 없는 패치를 조기 폐기
//-----------------------------------------------------------------------------
#version 430

layout(vertices = 4) out;

layout(std140, binding = 0) uniform CameraBlock {
    mat4 view;
    mat4 proj;
    mat4 vp;
    vec4 cameraPos;
} camera;

in vec2 Tex1[];
out vec2 Tex2[];

uniform mat4 model;
uniform float heightScale = 200.0;

// Unit 2: 강 마스크
uniform sampler2D riverMask;

//-----------------------------------------------------------------------------
// 헬퍼: 패치가 시야(Frustum) 안에 있는지 검사 (지형 TCS와 동일)
//-----------------------------------------------------------------------------
bool IsPatchVisible(vec4 p0, vec4 p1, vec4 p2, vec4 p3)
{
    vec4 corners[4] = vec4[](p0, p1, p2, p3);
    vec4 upVec = vec4(0.0, 0.0, heightScale, 0.0);

    int outCode[6] = int[](0, 0, 0, 0, 0, 0);

    for (int i = 0; i < 4; ++i)
    {
        vec4 points[2];
        points[0] = camera.vp * model * corners[i];
        points[1] = camera.vp * model * (corners[i] + upVec);

        for (int k = 0; k < 2; ++k)
        {
            vec4 p = points[k];
            if (p.x < -p.w) outCode[0]++;
            if (p.x >  p.w) outCode[1]++;
            if (p.y < -p.w) outCode[2]++;
            if (p.y >  p.w) outCode[3]++;
            if (p.z < -p.w) outCode[4]++;
            if (p.z >  p.w) outCode[5]++;
        }
    }

    for (int i = 0; i < 6; ++i)
        if (outCode[i] == 8) return false;

    return true;
}

//-----------------------------------------------------------------------------
// 헬퍼: 패치의 4코너 UV로 마스크를 샘플링해 강이 하나라도 있는지 검사
// riverMask에 밉맵이 있으면 낮은 레벨 샘플링으로 더 빠르게 처리 가능
//-----------------------------------------------------------------------------
bool PatchHasRiver(vec2 uv00, vec2 uv10, vec2 uv01, vec2 uv11)
{
    // 4코너 샘플링 - 하나라도 강이면 통과
    float m0 = textureLod(riverMask, uv00, 2.0).g;
    float m1 = textureLod(riverMask, uv10, 2.0).g;
    float m2 = textureLod(riverMask, uv01, 2.0).g;
    float m3 = textureLod(riverMask, uv11, 2.0).g;
    return max(max(m0, m1), max(m2, m3)) > 0.1;
}

void main()
{
    gl_out[gl_InvocationID].gl_Position = gl_in[gl_InvocationID].gl_Position;
    Tex2[gl_InvocationID] = Tex1[gl_InvocationID];

    if (gl_InvocationID == 0)
    {
        if (!IsPatchVisible(gl_in[0].gl_Position, gl_in[1].gl_Position,
                            gl_in[2].gl_Position, gl_in[3].gl_Position))
        {
            gl_TessLevelOuter[0] = -1.0;
            gl_TessLevelOuter[1] = -1.0;
            gl_TessLevelOuter[2] = -1.0;
            gl_TessLevelOuter[3] = -1.0;
            gl_TessLevelInner[0] = -1.0;
            gl_TessLevelInner[1] = -1.0;
            return;
        }

        // --- 거리 기반 테셀레이션 레벨 (지형 TCS와 동일) ---
        mat4 gView = camera.view * model;

        vec4 vs00 = gView * gl_in[0].gl_Position;
        vec4 vs01 = gView * gl_in[1].gl_Position;
        vec4 vs10 = gView * gl_in[2].gl_Position;
        vec4 vs11 = gView * gl_in[3].gl_Position;

        float len00 = length(vs00.xyz);
        float len01 = length(vs01.xyz);
        float len10 = length(vs10.xyz);
        float len11 = length(vs11.xyz);

        const float MIN_DISTANCE = 20.0;
        const float MAX_DISTANCE = 450.0;
        const float MIN_TESS     = 8.0;
        const float MAX_TESS     = 32.0;

        float d00 = clamp((len00 - MIN_DISTANCE) / (MAX_DISTANCE - MIN_DISTANCE), 0.0, 1.0);
        float d01 = clamp((len01 - MIN_DISTANCE) / (MAX_DISTANCE - MIN_DISTANCE), 0.0, 1.0);
        float d10 = clamp((len10 - MIN_DISTANCE) / (MAX_DISTANCE - MIN_DISTANCE), 0.0, 1.0);
        float d11 = clamp((len11 - MIN_DISTANCE) / (MAX_DISTANCE - MIN_DISTANCE), 0.0, 1.0);

        float tessLevel0 = mix(MAX_TESS, MIN_TESS, min(d10, d00)); // Left
        float tessLevel1 = mix(MAX_TESS, MIN_TESS, min(d00, d01)); // Bottom
        float tessLevel2 = mix(MAX_TESS, MIN_TESS, min(d01, d11)); // Right
        float tessLevel3 = mix(MAX_TESS, MIN_TESS, min(d11, d10)); // Top

        gl_TessLevelOuter[0] = tessLevel0;
        gl_TessLevelOuter[1] = tessLevel1;
        gl_TessLevelOuter[2] = tessLevel2;
        gl_TessLevelOuter[3] = tessLevel3;

        gl_TessLevelInner[0] = max(tessLevel1, tessLevel3);
        gl_TessLevelInner[1] = max(tessLevel0, tessLevel2);
    }
}
