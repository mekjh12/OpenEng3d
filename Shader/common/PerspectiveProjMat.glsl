#version 430

/**
* 비선형 깊이값을 선형 깊이값으로 변환
* 투영 행렬이 perspective일 때 사용
* 
* @param depth 비선형 깊이값 [0,1]
* @param near 근평면 거리
* @param far 원평면 거리
* @return 선형화된 깊이값
*/
float LinearizeDepth(float depth, float near, float far)
{
    // NDC 공간으로 역변환 [-1,1]
    float z = depth * 2.0 - 1.0;
    
    // 선형 깊이값으로 변환
    return (2.0 * near * far) / (far + near - z * (far - near));    
}

/**
* 지정된 밉맵 레벨에서 텍스처의 오프셋 샘플을 가져온다
*
* @param tex 샘플링할 텍스처
* @param uv 샘플링 기준 UV좌표
* @param level 밉맵 레벨
* @param offset 텍셀 오프셋
* @return 오프셋 위치의 텍셀 색상값
*/
vec4 textureOffsetLod(sampler2D tex, vec2 uv, int level, ivec2 offset)
{
    // 현재 밉맵 레벨의 텍셀 크기
    vec2 texelSize = 1.0 / textureSize(tex, level);
    
    // 오프셋 적용된 UV좌표
    vec2 offsetUV = uv + (offset * texelSize);
    
    // 오프셋 위치에서 샘플링
    return textureLod(tex, offsetUV, level);
}
