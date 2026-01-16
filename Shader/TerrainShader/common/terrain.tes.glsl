//-----------------------------------------------------------------------------
// 테셀레이션 평가 셰이더 - 위치만 계산
//-----------------------------------------------------------------------------
#version 430
layout (quads, fractional_odd_spacing, ccw) in;

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
out vec3 viewPosOut;  // Structure Buffer용 카메라 공간 위치

uniform sampler2D gHeightMap;
uniform mat4 model;
uniform float heightScale = 200.0f;

void main()
{   
    float u = gl_TessCoord.x;
    float v = gl_TessCoord.y;
    
    // 텍스처 좌표 보간
    vec2 t00 = Tex2[0];
    vec2 t01 = Tex2[1];
    vec2 t10 = Tex2[2];
    vec2 t11 = Tex2[3];
    vec2 t0 = (t01 - t00) * u + t00;
    vec2 t1 = (t11 - t10) * u + t10;
    Tex3 = (t1 - t0) * v + t0;
    
    float Height = texture(gHeightMap, Tex3).r;
    
    // 위치 계산
    vec4 p00 = gl_in[0].gl_Position;
    vec4 p01 = gl_in[1].gl_Position;
    vec4 p10 = gl_in[2].gl_Position;
    vec4 p11 = gl_in[3].gl_Position;
    vec4 p0 = (p01 - p00) * u + p00;
    vec4 p1 = (p11 - p10) * u + p10;
    vec4 p = (p1 - p0) * v + p0;
    p.z = heightScale * Height;
    
    fragPos = model * p;
    vec4 viewPos = camera.view * fragPos;
    viewDepth = viewPos.z;
    viewPosOut = viewPos.xyz;
    gl_Position = camera.vp * fragPos;
}