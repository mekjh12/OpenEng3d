#version 430
layout (quads, fractional_odd_spacing, ccw) in;

in vec2 Tex2[];
out float lightViewDepth;  // ⭐ Light 뷰 공간 깊이 출력

uniform sampler2D gHeightMap;
uniform mat4 lightView;
uniform mat4 lightProj;
uniform mat4 model;
uniform float heightScale = 200.0;

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
    vec2 texCoord = (t1 - t0) * v + t0;
    
    // 높이 샘플링
    float height = texture(gHeightMap, texCoord).r;
    
    // 위치 보간
    vec4 p00 = gl_in[0].gl_Position;
    vec4 p01 = gl_in[1].gl_Position;
    vec4 p10 = gl_in[2].gl_Position;
    vec4 p11 = gl_in[3].gl_Position;
    vec4 p0 = (p01 - p00) * u + p00;
    vec4 p1 = (p11 - p10) * u + p10;
    vec4 p = (p1 - p0) * v + p0;
    
    // 높이 적용
    p.z = heightScale * height;
    
    // 월드 -> 태양 관점 변환
    vec4 worldPos = model * p;

     // ⭐ Light Space로 변환 (뷰 공간 깊이 계산용)
    vec4 lightSpacePos = lightView * worldPos;
    
    // ⭐ Light 뷰 공간 깊이 (음수, Z가 멀수록 작음)
    lightViewDepth = -lightSpacePos.z;

    gl_Position = lightProj * lightSpacePos;
}