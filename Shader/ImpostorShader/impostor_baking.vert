#version 450 core

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 입력 (Vertex Attributes)
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
layout(location = 0) in vec3 position;       // 로컬 공간 위치
layout(location = 1) in vec2 textureCoords;  // UV 좌표
layout(location = 2) in vec3 normal;         // 로컬 공간 법선
layout(location = 3) in float materialID;    // 머티리얼 인덱스

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 유니폼 변수
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
uniform mat4 mvp;           // Model-View-Projection 행렬
uniform mat4 mv;            // Model-View 행렬 (뷰 공간 변환용)
uniform mat3 normalMatrix;  // Normal Matrix (법선 변환용, Model의 역전치)

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 출력 (Fragment Shader로 전달)
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
out vec2 vTexCoord;      // UV 좌표
out float vMaterialID;   // 머티리얼 인덱스
out vec3 vViewPos;       // 뷰 공간 위치 (깊이 계산용)
out vec3 vNormal;        // 뷰 공간 법선 (노멀 맵 생성용)

void main()
{
    gl_Position = mvp * vec4(position, 1.0);
    vTexCoord = textureCoords;
    vMaterialID = materialID;
    vViewPos = (mv * vec4(position, 1.0)).xyz;
    vNormal = normalize(normalMatrix * normal);
}
