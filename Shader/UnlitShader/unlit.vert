#version 450 core
layout(location = 0) in vec3 position;
layout(location = 1) in vec2 textureCoords;
layout(location = 3) in float materialID;

uniform mat4 mvp;
uniform mat4 mv;

out vec2 vTexCoord;
out float vMaterialID;
out vec3 vViewPos;

void main()
{
    gl_Position = mvp * vec4(position, 1.0);
    vTexCoord = textureCoords;
    vMaterialID = materialID;
    
    // 뷰 공간 위치 계산
    vViewPos = (mv * vec4(position, 1.0)).xyz;
}