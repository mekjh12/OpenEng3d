//-----------------------------------------------------------------------------
// 지형 렌더링을 위한 프래그먼트 셰이더
// - 높이 기반 텍스처 블렌딩
// - 노멀 맵 기반 조명
// - 반평면 안개 효과
//-----------------------------------------------------------------------------
#version 430

// 출력 컬러 버퍼 설정
layout(location = 0) out vec4 FragColor;

// 버텍스 셰이더로부터의 입력
in vec2 Tex3;         // 텍스처 좌표
in float Height;      // 프래그먼트의 높이값

// 높이별 지형 텍스처
uniform sampler2D gTextureHeight0;    // 최하단 높이 텍스처
uniform sampler2D gTextureHeight1;    // 하단 높이 텍스처
uniform sampler2D gTextureHeight2;    // 중단 높이 텍스처
uniform sampler2D gTextureHeight3;    // 상단 높이 텍스처
uniform sampler2D gTextureHeight4;    // 최상단 높이 텍스처

// 지형 맵 텍스처
uniform sampler2D gHeightMap;         // 높이 맵
uniform sampler2D gDetailMap;         // 디테일 맵
uniform bool gIsDetailMap;            // 디테일 맵 사용 여부

// 높이 구간 경계값 (0.0 ~ 1.0 범위)
uniform float gHeight0 = 0.07f;       // 최하단 경계
uniform float gHeight1 = 0.15f;       // 하단 경계
uniform float gHeight2 = 0.25f;       // 중단 경계
uniform float gHeight3 = 0.71f;       // 상단 경계
uniform float gHeight4 = 0.82f;       // 최상단 경계
uniform float gHeight5 = 1.00f;       // 최대 높이

// 렌더링 파라미터
uniform vec3 gReversedLightDir;           // 반전된 빛의 방향
uniform float gColorTexcoordScaling = 800.0f;  // 텍스처 타일링 배율

//-----------------------------------------------------------------------------
// 높이 기반 텍스처 블렌딩
//-----------------------------------------------------------------------------
vec4 BlendTerrainTextures()
{
   vec4 TexColor;
   // 텍스처 타일링을 위한 좌표 스케일링
   vec2 ScaledTexCoord = Tex3 * gColorTexcoordScaling;

   // 높이에 따른 텍스처 선택 및 블렌딩
   if (Height < gHeight0) {
      // 최하단 구간: 단일 텍스처
      TexColor = texture(gTextureHeight0, ScaledTexCoord);
   } 
   else if (Height < gHeight1) {
      // 하단 구간: 텍스처0과 텍스처1 블렌딩
      vec4 Color0 = texture(gTextureHeight0, ScaledTexCoord);
      vec4 Color1 = texture(gTextureHeight1, ScaledTexCoord);
      float Delta = gHeight1 - gHeight0;
      float Factor = (Height - gHeight0) / Delta;
      TexColor = mix(Color0, Color1, Factor);
   } 
   else if (Height < gHeight2) {
      // 중하단 구간: 텍스처1과 텍스처2 블렌딩
      vec4 Color0 = texture(gTextureHeight1, ScaledTexCoord);
      vec4 Color1 = texture(gTextureHeight2, ScaledTexCoord);
      float Delta = gHeight2 - gHeight1;
      float Factor = (Height - gHeight1) / Delta;
      TexColor = mix(Color0, Color1, Factor);
   } 
   else if (Height < gHeight3) {
      // 중상단 구간: 텍스처2와 텍스처3 블렌딩
      vec4 Color0 = texture(gTextureHeight2, ScaledTexCoord);
      vec4 Color1 = texture(gTextureHeight3, ScaledTexCoord);
      float Delta = gHeight3 - gHeight2;
      float Factor = (Height - gHeight2) / Delta;
      TexColor = mix(Color0, Color1, Factor);
   } 
   else if (Height < gHeight4) {
      // 상단 구간: 텍스처3과 텍스처4 블렌딩
      vec4 Color0 = texture(gTextureHeight3, ScaledTexCoord);
      vec4 Color1 = texture(gTextureHeight4, ScaledTexCoord);
      float Delta = gHeight4 - gHeight3;
      float Factor = (Height - gHeight3) / Delta;
      TexColor = mix(Color0, Color1, Factor);
   } 
   else {
      // 최상단 구간: 단일 텍스처
      TexColor = texture(gTextureHeight4, ScaledTexCoord);
   }

   // 디테일 맵 적용 (활성화된 경우)
   if (gIsDetailMap) {
       TexColor *= texture(gDetailMap, ScaledTexCoord);
   }

   return TexColor;
}

//-----------------------------------------------------------------------------
// 노멀 맵 기반 법선 계산
//-----------------------------------------------------------------------------
vec3 CalcNormal()
{    
   // 주변 픽셀의 높이값 샘플링
   float left  = textureOffset(gHeightMap, Tex3, ivec2(-1, 0)).r;
   float right = textureOffset(gHeightMap, Tex3, ivec2( 1, 0)).r;
   float up    = textureOffset(gHeightMap, Tex3, ivec2( 0, 1)).r;
   float down  = textureOffset(gHeightMap, Tex3, ivec2( 0, -1)).r;
   
   // 법선 벡터 계산 (z=0.1f로 경사 강도 조절)
   vec3 normal = normalize(vec3(left - right, up - down, 0.1f));
   return normal;
}

//-----------------------------------------------------------------------------
// 메인 함수
//-----------------------------------------------------------------------------
void main()
{
   // 텍스처 색상과 조명 계산
   vec4 TexColor = BlendTerrainTextures();
   
    // 현재 프래그먼트의 법선 벡터 계산
    vec3 Normal = CalcNormal();

   // 최종 색상 계산
   vec3 shadedColor = TexColor.xyz;
   FragColor = vec4(shadedColor, 1);
}