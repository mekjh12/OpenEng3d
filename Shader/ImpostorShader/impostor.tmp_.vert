#version 430

// 버텍스 쉐이더 입력: 정점의 로컬 좌표 (모델 공간)
in vec3 position;			// 물체의 위치점 한 개만 들어온다.

uniform mat4 model;			// 모델 변환 행렬

void main()
{
   // 로컬 좌표를 월드 공간으로 변환
   // position을 vec4로 확장하고(w=1.0) 모델 행렬을 적용
   vec4 worldPos = model * vec4(position, 1.0);
   
   // 최종 정점 위치 설정
   // 지오메트리 쉐이더나 다음 파이프라인 단계로 전달됨
   gl_Position = worldPos;
}
