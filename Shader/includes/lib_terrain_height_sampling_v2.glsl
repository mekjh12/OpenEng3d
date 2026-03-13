/**
* 청크(Chunk) 방식 지형의 높이맵에서 높이 값을 샘플링하는 함수
* 중앙 영역, 엣지, 모서리 케이스를 각각 다르게 처리하여 청크 간 경계가 부드럽게 연결되도록 함
*
* @param texCoord 샘플링할 텍스처 좌표 (UV 좌표, 0.0~1.0 범위)
* @param heightMapHighRes 고해상도 높이맵 텍스처
* @param adjacentHeightMap0 오른쪽에 인접한 청크의 높이맵
* @param adjacentHeightMap1 오른쪽 위 모서리에 인접한 청크의 높이맵
* @param adjacentHeightMap2 위쪽에 인접한 청크의 높이맵
* @param adjacentHeightMap3 왼쪽 위 모서리에 인접한 청크의 높이맵
* @param adjacentHeightMap4 왼쪽에 인접한 청크의 높이맵
* @param adjacentHeightMap5 왼쪽 아래 모서리에 인접한 청크의 높이맵
* @param adjacentHeightMap6 아래쪽에 인접한 청크의 높이맵
* @param adjacentHeightMap7 오른쪽 아래 모서리에 인접한 청크의 높이맵
* @return 샘플링된 높이 값 (일반 영역은 LOD 블렌딩, 엣지는 인접 청크와 혼합)
*/
float SampleHeightForChunk(
    vec2 texCoord, 
    sampler2D heightMapHighRes,
    sampler2D adjacentHeightMap0,
    sampler2D adjacentHeightMap1,
    sampler2D adjacentHeightMap2,
    sampler2D adjacentHeightMap3,
    sampler2D adjacentHeightMap4,
    sampler2D adjacentHeightMap5,
    sampler2D adjacentHeightMap6,
    sampler2D adjacentHeightMap7
    )
{
    // 상수 정의
    const float EDGE_THRESHOLD = 0.001f;
    const float OPPOSITE_EDGE = 0.999f;
    
    // 먼저 빠른 중앙 케이스 체크 (대부분의 픽셀)
    if(texCoord.x >= EDGE_THRESHOLD && texCoord.x <= OPPOSITE_EDGE && 
       texCoord.y >= EDGE_THRESHOLD && texCoord.y <= OPPOSITE_EDGE) {
        // 일반 케이스 - 엣지가 아닌 경우
        return texture(heightMapHighRes, texCoord).r;
    }
    
    // 엣지 케이스: 여기서는 모서리를 포함한 모든 엣지 케이스 처리
    bool isRightEdge = texCoord.x > OPPOSITE_EDGE;
    bool isLeftEdge = texCoord.x < EDGE_THRESHOLD;
    bool isTopEdge = texCoord.y > OPPOSITE_EDGE;
    bool isBottomEdge = texCoord.y < EDGE_THRESHOLD;
    
    // 클램프된 좌표
    vec2 clampedCoord = vec2(
        clamp(texCoord.x, EDGE_THRESHOLD, OPPOSITE_EDGE),
        clamp(texCoord.y, EDGE_THRESHOLD, OPPOSITE_EDGE)
    );

    
    
    // 공통으로 사용할 모서리 좌표 정의
    vec2 topLeft = vec2(EDGE_THRESHOLD, EDGE_THRESHOLD);
    vec2 topRight = vec2(OPPOSITE_EDGE, EDGE_THRESHOLD);
    vec2 bottomRight = vec2(OPPOSITE_EDGE, OPPOSITE_EDGE);
    vec2 bottomLeft = vec2(EDGE_THRESHOLD, OPPOSITE_EDGE);
        
    float orgHeight = texture(heightMapHighRes, clampedCoord).r;
    
    // 모서리 케이스 처리 (약 0.01%정도)
    if(isRightEdge && isTopEdge) {
        // 오른쪽 위 모서리
        return 0.25f * (orgHeight 
                + texture(adjacentHeightMap0, bottomLeft).r
                + texture(adjacentHeightMap1, topLeft).r
                + texture(adjacentHeightMap2, topRight).r);
    }
    if(isLeftEdge && isTopEdge) {
        // 왼쪽 위 모서리
        return 0.25f * (orgHeight 
                + texture(adjacentHeightMap2, topLeft).r
                + texture(adjacentHeightMap3, topRight).r
                + texture(adjacentHeightMap4, bottomRight).r);
    }
    if(isLeftEdge && isBottomEdge) {
        // 왼쪽 아래 모서리
        return 0.25f * (orgHeight 
                + texture(adjacentHeightMap4, topRight).r
                + texture(adjacentHeightMap5, bottomRight).r
                + texture(adjacentHeightMap6, bottomLeft).r);
    }
    if(isRightEdge && isBottomEdge) {
        // 오른쪽 아래 모서리
        return 0.25f * (orgHeight
                + texture(adjacentHeightMap6, bottomRight).r
                + texture(adjacentHeightMap7, bottomLeft).r
                + texture(adjacentHeightMap0, topLeft).r);
    }
    
    // 단일 엣지 케이스 처리
    float adjHeight;
    
    if(isRightEdge) {
        adjHeight = texture(adjacentHeightMap0, vec2(EDGE_THRESHOLD, clampedCoord.y)).r;
    }
    else if(isLeftEdge) {
        adjHeight = texture(adjacentHeightMap4, vec2(OPPOSITE_EDGE, clampedCoord.y)).r;
    }
    else if(isTopEdge) {
        adjHeight = texture(adjacentHeightMap2, vec2(clampedCoord.x, EDGE_THRESHOLD)).r;
    }
    else {
        // isBottomEdge (여기에 도달하면 반드시 아래쪽 엣지임)
        adjHeight = texture(adjacentHeightMap6, vec2(clampedCoord.x, OPPOSITE_EDGE)).r;
    }
    
    return (orgHeight + adjHeight) * 0.5f;
}