//-----------------------------------------------------------------------------
// 테셀레이션 평가 셰이더 (Tessellation Evaluation Shader)
// 패치의 테셀레이션된 점들에 대해 높이맵 기반의 변위를 적용
//-----------------------------------------------------------------------------

#version 430

// 공통 헤더 포함
#include "./../includes/lib_terrain_height_sampling.glsl"

// 테셀레이션 제어 파라미터
layout (quads, fractional_odd_spacing, ccw) in;

// 테셀레이션 제어 셰이더로부터의 입력
in vec2 Tex2[];           // 패치의 각 제어점에 대한 텍스처 좌표
in vec4 ViewSpacePos[];   // 패치의 각 제어점에 대한 뷰 공간 위치

// 프래그먼트 셰이더로의 출력
out vec2 Tex3;           // 보간된 텍스처 좌표
out float Height;        // 샘플링된 높이값
out vec4 viewPos;        // 뷰 공간에서의 위치
out vec4 fragPos;        // 월드 공간에서의 위치

// 유니폼 변수
uniform sampler2D gHeightMap;		// 높이맵 텍스처
uniform mat4 proj;					// 투영 행렬
uniform mat4 model;					// 모델 행렬
uniform mat4 view;					// 뷰 행렬
uniform float heightScale = 200.0f;	// 높이스케일

// 주변 높이맵 텍스처
uniform sampler2D adjacentHeightMap0; // 주변 높이맵
uniform sampler2D adjacentHeightMap1; // 주변 높이맵
uniform sampler2D adjacentHeightMap2; // 주변 높이맵
uniform sampler2D adjacentHeightMap3; // 주변 높이맵
uniform sampler2D adjacentHeightMap4; // 주변 높이맵
uniform sampler2D adjacentHeightMap5; // 주변 높이맵
uniform sampler2D adjacentHeightMap6; // 주변 높이맵
uniform sampler2D adjacentHeightMap7; // 주변 높이맵

void main()
{   
   // 테셀레이터에서 생성된 보간 좌표
   float u = gl_TessCoord.x;    // u 방향 보간 계수
   float v = gl_TessCoord.y;    // v 방향 보간 계수

   // 패치의 네 꼭지점에 대한 텍스처 좌표
   vec2 t00 = Tex2[0];     // 좌하단
   vec2 t01 = Tex2[1];     // 우하단
   vec2 t10 = Tex2[2];     // 좌상단
   vec2 t11 = Tex2[3];     // 우상단

   // 이중선형 보간으로 텍스처 좌표 계산
   vec2 t0 = (t01 - t00) * u + t00;    // 아래쪽 에지 보간
   vec2 t1 = (t11 - t10) * u + t10;    // 위쪽 에지 보간
   Tex3 = (t1 - t0) * v + t0;          // 최종 텍스처 좌표

   // 높이맵에서 높이값 샘플링
   Height = SampleHeightForSimpleTerrain(Tex3, gHeightMap,
       adjacentHeightMap0, adjacentHeightMap2, adjacentHeightMap4, adjacentHeightMap6).r;

   // 패치의 네 꼭지점 위치
   vec4 p00 = gl_in[0].gl_Position;    // 좌하단
   vec4 p01 = gl_in[1].gl_Position;    // 우하단
   vec4 p10 = gl_in[2].gl_Position;    // 좌상단
   vec4 p11 = gl_in[3].gl_Position;    // 우상단

    // 위치에 대한 이중선형 보간
    vec4 p0 = (p01 - p00) * u + p00;    // 아래쪽 에지 보간
    vec4 p1 = (p11 - p10) * u + p10;    // 위쪽 에지 보간
    vec4 p = (p1 - p0) * v + p0;        // 최종 위치

    // 높이맵 기반 수직 변위 적용 (높이 스케일)
    p.z = heightScale * Height;

    // 최종 위치 계산 (월드 -> 뷰 -> 클립 공간 변환)
    fragPos = model * p;             // 월드 공간 위치
    viewPos = view * fragPos;        // 뷰 공간 위치
    gl_Position = proj * viewPos;    // 클립 공간 위치
}


