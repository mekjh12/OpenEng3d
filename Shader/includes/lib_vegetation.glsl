//-----------------------------------------------------------------------------
// 픽셀 쉐이더 기반 나무 렌더링 (Procedural Impostor)
//-----------------------------------------------------------------------------
vec3 RenderVegetation(vec3 terrainColor, vec2 texCoord, sampler2D vegetationMap)
{
    // 중심 픽셀의 나무 밀도
    float treeSize = texture(vegetationMap, texCoord).r;
    
    // 나무가 존재하는 픽셀만 색상 변경
    if (treeSize > 0.0f)
    {
        vec3 treeColor = mix(vec3(0.1, 0.5, 0.1), vec3(0.2, 0.8, 0.2), treeSize);
        return mix(terrainColor, treeColor, 0.2f); // 지형과 부드럽게 블렌딩
    }
    
    return terrainColor; // 나무가 없으면 원래 지형 색상 유지
}