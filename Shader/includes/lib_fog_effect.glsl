vec3 ApplyFog(vec3 shadedColor, vec3 fogcolor, vec3 v, float density)
{
   float f = exp(-density * length(v));
   return mix(fogcolor, shadedColor, f);
}

// ----------------------------------------------------------------------------------------------------------
// 반평면 안개
// ----------------------------------------------------------------------------------------------------------
vec3 ApplyHalfspaceFog(vec3 shadedColor, vec3 fogcolor, vec3 v, float density, float fv, float u1, float u2)
{
   const float kFogEpsilon = 0.0001f;
   float x = min(u2, 0.0f);
   float tau = 0.5f * density * length(v) * (u1 - x * x / (abs(fv) + kFogEpsilon));
   return mix(fogcolor, shadedColor, exp(tau));
}

// ----------------------------------------------------------------------------------------------------------
// 반평면 안개 효과 파라미터 계산 및 적용 함수
// ----------------------------------------------------------------------------------------------------------
vec3 CalculateAndApplyFog(vec3 originalColor, vec3 fogColor, float fogDensity, vec3 camPos, vec3 fragPos, vec4 fogPlane)
{
    // 반평면 안개 파라미터 계산
    float fc = dot(camPos, fogPlane.xyz) + fogPlane.w;    // 카메라-안개평면 거리
    float fp = dot(fragPos, fogPlane.xyz) + fogPlane.w;   // 프래그먼트-안개평면 거리
    vec3 v = camPos - fragPos;                            // 시점 벡터
    float fv = dot(v, fogPlane.xyz);                      // 시점 벡터와 안개 평면의 내적
    float m = (fc < 0) ? 1.0f : 0.0f;                     // 카메라가 안개 평면 아래에 있는지 여부
    float u1 = m * (fc + fp);
    float u2 = fp * sign(fc);
    
    // 최종적으로 안개 효과 적용
    return ApplyHalfspaceFog(originalColor, fogColor, v, fogDensity, fv, u1, u2);
}

// ----------------------------------------------------------------------------------------------------------
// 거리 기반 안개 계산 함수 (반평면 안개가 적용된 색상에 거리 제한 추가)
// ----------------------------------------------------------------------------------------------------------
vec3 ApplyDistanceBasedFog(vec3 originalColor, vec3 fogColor, float fogDensity, 
    vec3 fragPos, vec4 fogPlane, vec3 camPos, vec3 fogCenter, float fogMinRadius, float fogMaxRadius)
{
    // 프래그먼트와 안개 중심점 사이의 거리 계산
    float distToFogCenter = distance(fragPos, fogCenter);
    
    // 거리에 따른 강도 계수 계산
    float distanceFactor = 1.0 - smoothstep(fogMinRadius, fogMaxRadius, distToFogCenter);
    
    // 거리 인자가 0 이하면 안개 효과가 없음 (원본 색상 반환)
    if (distanceFactor <= 0.0)
        return originalColor;
    
    // 이미 반평면 안개가 적용된 색상에 거리 기반 제한을 적용
    // 거리 팩터가 1.0이면 원래 안개 효과 그대로, 0.0에 가까울수록 원본 색상에 가까워짐
    return mix(originalColor, originalColor, distanceFactor);
}

