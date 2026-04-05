#version 430

layout (quads, fractional_odd_spacing, ccw) in;

layout(std140, binding = 0) uniform CameraBlock {
    mat4 view;
    mat4 proj;
    mat4 vp;
    vec4 cameraPos;
} camera;

in vec2 Tex2[];

out float viewDepth;

uniform sampler2D gHeightMap;  // Texture0 그대로
uniform mat4 model;
uniform float heightScale;

void main()
{
    float u = gl_TessCoord.x;
    float v = gl_TessCoord.y;

    vec2 t00 = Tex2[0];
    vec2 t01 = Tex2[1];
    vec2 t10 = Tex2[2];
    vec2 t11 = Tex2[3];
    vec2 t0  = (t01 - t00) * u + t00;
    vec2 t1  = (t11 - t10) * u + t10;
    vec2 texCoord = (t1 - t0) * v + t0;

    // 인접 타일 없이 clamp로 경계 처리
    // UV가 정확히 0.0/1.0일 때 텍스처 wrap 모드 영향 방지
    // (texel 크기 기반 margin 불필요 - 단순 경계값 회피용)
    texCoord = clamp(texCoord, 0.0001, 0.9999);

    float height = texture(gHeightMap, texCoord).r;

    vec4 p00 = gl_in[0].gl_Position;
    vec4 p01 = gl_in[1].gl_Position;
    vec4 p10 = gl_in[2].gl_Position;
    vec4 p11 = gl_in[3].gl_Position;
    vec4 p0  = (p01 - p00) * u + p00;
    vec4 p1  = (p11 - p10) * u + p10;
    vec4 p   = (p1 - p0) * v + p0;
    p.z = heightScale * height;

    vec4 worldPos = model * p;
    vec4 viewPos  = camera.view * worldPos;
    viewDepth     = viewPos.z;
    gl_Position   = camera.vp * worldPos;
}