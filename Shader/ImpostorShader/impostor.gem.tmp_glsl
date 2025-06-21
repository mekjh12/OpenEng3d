#version 430

// 입력 프리미티브 타입 지정 (점)
layout (points) in;
// 출력 프리미티브 타입 지정 (삼각형 스트립, 최대 4개의 정점 출력)
layout (triangle_strip, max_vertices = 4) out;

// 지오메트리 쉐이더 출력 구조체 (프래그먼트 쉐이더로 전달)
out GS_OUT {
   vec2 texCoord;   // UV 텍스처 좌표
} gs_out;

// Uniform 변수들
uniform mat4 vp;                        // View-Projection 행렬
uniform vec3 cameraPosition;            // 카메라 위치
uniform vec3 worldPosition;             // 객체의 월드 위치
uniform float aabbSizeModel;             // Model-AABB(Axis-Aligned Bounding Box)의 크기
uniform vec3 aabbCenterEntity;          // AABB의 중심점

void main()
{
    // AABB의 크기 계산, 최대점에서 최소점을 빼서 바운딩 박스의 실제 크기를 구함
    //vec3 aabbDimensions = aabbSizeModel;
    //float maxDist = max(max(aabbDimensions.x, aabbDimensions.y),aabbDimensions.z);

    // 빌보드 방향 계산
    // 1. 카메라 방향 벡터 계산 (정규화된 카메라까지의 방향)
    vec3 toCamera = normalize(cameraPosition - worldPosition);
    // 2. 빌보드의 오른쪽 벡터 계산 (z축과 카메라 방향의 외적)
    vec3 right = normalize(cross(vec3(0.0, 0.0, 1.0), toCamera)) * aabbSizeModel;
    // 3. 빌보드의 위쪽 벡터 계산 (카메라 방향과 오른쪽 벡터의 외적)
    vec3 up = normalize(cross(toCamera, right)) * aabbSizeModel;
   
    // 빌보드의 네 모서리 위치 계산
    vec3 positions[4];
    positions[0] = aabbCenterEntity + (-right - up);          // 좌하단
    positions[1] = aabbCenterEntity + (right - up);           // 우하단
    positions[2] = aabbCenterEntity + (-right + up);          // 좌상단
    positions[3] = aabbCenterEntity + (right + up);           // 우상단
   
    // UV 텍스처 좌표 설정
    const vec2 texCoords[4] = vec2[4](
        vec2(0.0, 0.0),    // 좌하단
        vec2(1.0, 0.0),    // 우하단
        vec2(0.0, 1.0),    // 좌상단
        vec2(1.0, 1.0)     // 우상단
    );
   
    // 정점 생성 및 출력
    for (int i = 0; i < 4; i++) 
    {
        // MVP 행렬을 적용하여 클립 공간으로 변환
        gl_Position = vp * vec4(positions[i], 1.0);
        // 텍스처 좌표 전달
        gs_out.texCoord = texCoords[i];
        // 정점 출력
        EmitVertex();
    }
   
    // 프리미티브 완성
    EndPrimitive();
}
