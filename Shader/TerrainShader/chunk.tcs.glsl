#version 430

// 패치당 4개의 제어점 출력 설정
layout(vertices = 4) out;

// 버텍스 셰이더로부터의 입력/출력 데이터
in vec2 Tex1[];                    // 입력 텍스처 좌표
out vec2 Tex2[];                   // 출력 텍스처 좌표
out vec4 ViewSpacePos[];           // 뷰 공간 위치

// 변환 관련 uniform 변수들
uniform mat4 proj;                 // 투영 행렬
uniform mat4 model;                // 모델 행렬
uniform mat4 view;                 // 뷰 행렬

// 청크 관련 uniform 변수들
uniform vec3 chunkPos;             // 청크의 월드 위치
uniform vec3 chunkCoord;           // 청크의 그리드 좌표
uniform float chunkSize;           // 청크의 크기
uniform float chunkSeperateCount;  // 청크 분할 개수

void main()
{
   // 제어점 데이터 전달
   gl_out[gl_InvocationID].gl_Position = gl_in[gl_InvocationID].gl_Position;
   Tex2[gl_InvocationID] = Tex1[gl_InvocationID];

   // 청크 기반 텍스처 좌표 계산
   float tunit = 1.0f / chunkSeperateCount;
   float tx = 0.5f + float(chunkCoord.x) / chunkSeperateCount;
   float ty = 0.5f + float(chunkCoord.y) / chunkSeperateCount;

   // 각 제어점별 텍스처 좌표 할당
   if (gl_InvocationID == 0) Tex2[gl_InvocationID] = vec2(tx, ty);                     // 좌하단
   if (gl_InvocationID == 1) Tex2[gl_InvocationID] = vec2(tx + tunit, ty);             // 우하단
   if (gl_InvocationID == 2) Tex2[gl_InvocationID] = vec2(tx, ty + tunit);             // 좌상단
   if (gl_InvocationID == 3) Tex2[gl_InvocationID] = vec2(tx + tunit, ty + tunit);     // 우상단

   // 뷰 공간 변환 및 거리 계산
   mat4 gView = view * model;
   vec4 ViewSpacePos00 = gView * gl_in[0].gl_Position;  // 좌하단
   vec4 ViewSpacePos01 = gView * gl_in[1].gl_Position;  // 우하단
   vec4 ViewSpacePos10 = gView * gl_in[2].gl_Position;  // 좌상단
   vec4 ViewSpacePos11 = gView * gl_in[3].gl_Position;  // 우상단

   // 각 정점의 카메라 거리 계산
   float Len00 = length(ViewSpacePos00.xyz);
   float Len01 = length(ViewSpacePos01.xyz);
   float Len10 = length(ViewSpacePos10.xyz);
   float Len11 = length(ViewSpacePos11.xyz);

   // 테셀레이션 제어 상수
   const float MIN_DISTANCE = 1;      // 최대 테셀레이션 적용 거리
   const float MAx_DISTANCE = 400;    // 최소 테셀레이션 적용 거리
   const int MIN_TESS_LEVEL = 8;      // 최소 테셀레이션 분할 수
   const int MAx_TESS_LEVEL = 128;    // 최대 테셀레이션 분할 수

   // 거리 정규화
   float Distance00 = clamp((Len00 - MIN_DISTANCE) / (MAx_DISTANCE - MIN_DISTANCE), 0.0, 1.0);
   float Distance01 = clamp((Len01 - MIN_DISTANCE) / (MAx_DISTANCE - MIN_DISTANCE), 0.0, 1.0);
   float Distance10 = clamp((Len10 - MIN_DISTANCE) / (MAx_DISTANCE - MIN_DISTANCE), 0.0, 1.0);
   float Distance11 = clamp((Len11 - MIN_DISTANCE) / (MAx_DISTANCE - MIN_DISTANCE), 0.0, 1.0);

   // 엣지별 테셀레이션 레벨 계산
   float TessLevel0 = mix(MAx_TESS_LEVEL, MIN_TESS_LEVEL, min(Distance10, Distance00));  // 좌측
   float TessLevel1 = mix(MAx_TESS_LEVEL, MIN_TESS_LEVEL, min(Distance00, Distance01));  // 하단
   float TessLevel2 = mix(MAx_TESS_LEVEL, MIN_TESS_LEVEL, min(Distance01, Distance11));  // 우측
   float TessLevel3 = mix(MAx_TESS_LEVEL, MIN_TESS_LEVEL, min(Distance11, Distance10));  // 상단

   // 테셀레이션 레벨 설정
   gl_TessLevelOuter[0] = TessLevel0;  // 좌측 엣지
   gl_TessLevelOuter[1] = TessLevel1;  // 하단 엣지
   gl_TessLevelOuter[2] = TessLevel2;  // 우측 엣지
   gl_TessLevelOuter[3] = TessLevel3;  // 상단 엣지

   // 내부 테셀레이션 레벨 설정
   gl_TessLevelInner[0] = max(TessLevel1, TessLevel3);  // 수평 방향
   gl_TessLevelInner[1] = max(TessLevel0, TessLevel2);  // 수직 방향
}