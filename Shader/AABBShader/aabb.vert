#version 430

// 버텍스 셰이더 입력: AABB 중심점
in vec3 position;           // AABB의 중심점

// 인스턴스별 속성
in vec3 instanceHalfSize;   // AABB의 반 크기 (halfSize)
in vec4 instanceColor;      // AABB의 색상

// 지오메트리 셰이더로 전달할 데이터
out VS_OUT {
    vec3 halfSize;
    vec4 color;
} vs_out;

void main()
{
    // AABB 중심점을 그대로 전달
    gl_Position = vec4(position, 1.0);
    
    // 인스턴스 데이터 전달
    vs_out.halfSize = instanceHalfSize;
    vs_out.color = instanceColor;
}