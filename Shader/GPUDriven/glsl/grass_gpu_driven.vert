#version 450 core

// SSBO: LOD별 로컬 템플릿 (binding 0/1/2)
layout(std430, binding = 0) buffer TemplateLOD0Buffer { vec4 templatesLOD0[]; };
layout(std430, binding = 1) buffer TemplateLOD1Buffer { vec4 templatesLOD1[]; };
layout(std430, binding = 2) buffer TemplateLOD2Buffer { vec4 templatesLOD2[]; };

// SSBO: 현재 LOD의 가시 타일 (binding 3 - 렌더링 시 동적 바인딩)
layout(std430, binding = 3) buffer VisibleTilesBuffer { vec4 visibleTiles[]; };

// UBO: 카메라
layout(std140, binding = 0) uniform CameraBlock { mat4 view; mat4 proj; mat4 vp; vec4 cameraPos; } camera;

// Uniforms
uniform sampler2D u_Heightmap;
uniform float u_HeightScale;
uniform vec2 u_TerrainWorldSize;
uniform vec3 u_CameraRight;
uniform vec3 u_CameraUp;
uniform float u_GrassWidth;
uniform float u_GrassHeight;

// 현재 렌더링 중인 LOD 정보 (Uniform으로 전달)
uniform int u_CurrentLOD;        // 0, 1, 2
uniform int u_GrassPerTile;      // 해당 LOD의 풀 개수

// 출력
out vec2 v_TexCoord;
out float v_AO;
out vec2 v_HeightmapUV;

// LOD별 Template 읽기
vec4 GetTemplateData(int lod, int localIndex)
{
    if (lod == 0) return templatesLOD0[localIndex];
    if (lod == 1) return templatesLOD1[localIndex];
    if (lod == 2) return templatesLOD2[localIndex];
    return vec4(0.0);
}

void main()
{
    // 1. 어느 타일의 몇 번째 풀인지 계산 (루프 없이 단순 나눗셈!)
    int tileIndex = gl_InstanceID / u_GrassPerTile;
    int localIndex = gl_InstanceID % u_GrassPerTile;
    
    // 2. 가시 타일 데이터 읽기 (binding 3에서 직접 읽기)
    vec4 tileData = visibleTiles[tileIndex];
    vec2 tileWorldOffset = tileData.xy;
    float tileSize = tileData.z;
    
    // 3. 현재 LOD에 맞는 Template 읽기
    vec4 localData = GetTemplateData(u_CurrentLOD, localIndex);
    vec2 localPos = localData.xy;
    float rotation = localData.z;
    float scale = localData.w;
    
    // 4. 월드 XY 좌표 계산
    vec2 worldXY = tileWorldOffset + localPos;
    
    // 5. 높이맵 샘플링
    vec2 heightUV = (worldXY + u_TerrainWorldSize) / (u_TerrainWorldSize * 2.0);
    float height = texture(u_Heightmap, heightUV).r;
    float worldZ = height * u_HeightScale;
    
    v_HeightmapUV = heightUV;
    
    // 6. 쿼드 버텍스 오프셋
    int vertexID = gl_VertexID % 4;
    vec2 offsets[4] = vec2[4](
        vec2(-0.02,  0.0),
        vec2( 0.02,  0.0),
        vec2(-0.005, 0.3),
        vec2( 0.005, 0.3)
    );
    vec2 offset = offsets[vertexID];
    
    // 7. 회전 적용
    float c = cos(rotation);
    float s = sin(rotation);
    vec2 rotatedOffset = vec2(
        offset.x * c - offset.y * s,
        offset.x * s + offset.y * c
    );
    
    // 8. 빌보드 벡터
    vec3 right = vec3(c, s, 0);
    vec3 up = vec3(0, 0, 1);
    
    // 9. 최종 월드 위치
    vec3 worldPos = vec3(worldXY, worldZ)
                  + right * offset.x * u_GrassWidth * scale
                  + up * offset.y * u_GrassHeight * scale;
    
    // 10. 클립 공간 변환
    gl_Position = camera.vp * vec4(worldPos, 1.0);
    
    // 11. 텍스처 좌표 및 AO
    v_TexCoord = vec2(offset.x + 0.5, offset.y);
    v_AO = mix(0.4, 1.0, offset.y);
}