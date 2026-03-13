//-----------------------------------------------------------------------------
// 강 렌더링 테셀레이션 평가 셰이더 (TES)
// - 높이맵(Unit 0, 1)으로 Z축 높이 설정
// - 강 마스크(Unit 2)로 강 외부 버텍스를 클립 공간 밖으로 추방
//-----------------------------------------------------------------------------
#version 430

layout(quads, fractional_odd_spacing, ccw) in;

layout(std140, binding = 0) uniform CameraBlock {
    mat4 view;
    mat4 proj;
    mat4 vp;
    vec4 cameraPos;
} camera;

in vec2 Tex2[];

out vec2 Tex3;
out vec4 fragPos;
out float viewDepth;
out vec3 viewPosOut;

// Unit 0: 고해상도 높이맵
uniform sampler2D heightMapHighRes;
// Unit 1: 저해상도 높이맵
uniform sampler2D heightMapLowRes;
// Unit 2: 강 마스크
uniform sampler2D riverMask;

uniform float blendFactor  = 1.0;
uniform float heightScale  = 200.0;
uniform mat4  model;

// 강 메시를 지형보다 살짝 올려 z-fighting 방지
uniform float riverHeightOffset = 0.5;

void main()
{
    float u = gl_TessCoord.x;
    float v = gl_TessCoord.y;

    // 텍스처 좌표 이중선형 보간
    vec2 t00 = Tex2[0];
    vec2 t01 = Tex2[1];
    vec2 t10 = Tex2[2];
    vec2 t11 = Tex2[3];

    vec2 t0 = (t01 - t00) * u + t00;
    vec2 t1 = (t11 - t10) * u + t10;
    Tex3 = (t1 - t0) * v + t0;

    // --- 강 마스크 체크: 강 외부면 클립 공간 밖으로 추방 ---
    vec2 maskUV = Tex3;
    maskUV.y = 1.0 - maskUV.y; // 텍스처 좌표계 보정
    float mask = texture(riverMask, maskUV).g;
    if (mask < 0.4)
    {
        gl_Position = vec4(10.0, 10.0, 10.0, 1.0);
        fragPos     = vec4(0.0);
        viewDepth   = 0.0;
        viewPosOut  = vec3(0.0);
        return;
    }

    // --- 위치 이중선형 보간 ---
    vec4 p00 = gl_in[0].gl_Position;
    vec4 p01 = gl_in[1].gl_Position;
    vec4 p10 = gl_in[2].gl_Position;
    vec4 p11 = gl_in[3].gl_Position;

    vec4 p0 = (p01 - p00) * u + p00;
    vec4 p1 = (p11 - p10) * u + p10;
    vec4 p  = (p1 - p0) * v + p0;

    // --- 높이맵 샘플링 (지형 TES와 동일, 인접 청크 없이 단순 버전) ---
    float heightHigh = texture(heightMapHighRes, Tex3).r;
    float heightLow  = texture(heightMapLowRes,  Tex3).r;
    float height     = mix(heightLow, heightHigh, blendFactor);

    // Z축 Up 좌표계 - 지형보다 riverHeightOffset만큼 위
    p.z = heightScale * height + riverHeightOffset;

    // --- 공간 변환 ---
    fragPos    = model * p;
    vec4 viewPos = camera.view * fragPos;
    viewDepth  = viewPos.z;
    viewPosOut = viewPos.xyz;
    gl_Position = camera.vp * fragPos;
}
