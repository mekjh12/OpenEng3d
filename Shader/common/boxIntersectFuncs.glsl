#version 430
//-----------------------------------------------------------------------------
// 광선 추적을 위한 기본 구조체 정의
//-----------------------------------------------------------------------------
// 광선을 정의하는 구조체
struct Ray {
    vec3 origin;    // 광선의 시작점
    vec3 direction; // 광선의 방향 벡터
};

// 축 정렬 경계 상자(AABB)를 정의하는 구조체
struct AABB {
    vec3 top;     // 상자의 최대점 (max point)
    vec3 bottom;  // 상자의 최소점 (min point)
};

//-----------------------------------------------------------------------------
// Slab 방법을 이용한 광선-상자 교차 검사
// 참고: https://tavianator.com/2011/ray_box.html
//-----------------------------------------------------------------------------
void ray_box_intersection(Ray ray, AABB box, out float t_0, out float t_1) 
{
    // 광선 방향의 역수를 계산 (나눗셈을 곱셈으로 최적화)
    vec3 direction_inv = 1.0 / ray.direction;
    
    // 각 축에 대한 교차점 매개변수 계산
    vec3 t_top = direction_inv * (box.top - ray.origin);
    vec3 t_bottom = direction_inv * (box.bottom - ray.origin);
    
    // 각 축별 진입점 매개변수 계산
    vec3 t_min = min(t_top, t_bottom);
    vec2 t = max(t_min.xx, t_min.yz);
    t_0 = max(0.0, max(t.x, t.y));  // 음수 매개변수 제외
    
    // 각 축별 퇴출점 매개변수 계산
    vec3 t_max = max(t_top, t_bottom);
    t = min(t_max.xx, t_max.yz);
    t_1 = min(t.x, t.y);
}