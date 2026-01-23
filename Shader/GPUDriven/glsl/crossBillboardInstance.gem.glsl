#version 450 core
layout(points) in;
layout(triangle_strip, max_vertices = 12) out;

// 카메라 Uniform Block
layout(std140, binding = 0) uniform CameraBlock {
    mat4 view;
    mat4 proj;
    mat4 vp;
    vec4 cameraPos;
} camera;

// VS에서 넘어온 데이터
in VS_OUT {
    vec3 worldPos;
    vec3 color;
    float horizontalSize;
    float verticalSize;
    mat4 transform;
    mat4 normalMat;
} gs_in[];

// FS로 보낼 데이터
out vec2 fTexCoord;
out flat int fPlaneIndex;
out vec3 vColor;
out vec3 vViewPos;
out vec3 vNormal;
out vec3 vTangent;    // T
out vec3 vBitangent;  // B (Main Bitangent)
out vec3 vWorldPos;

// 아틀라스 UV 오프셋 계산
vec2 GetAtlasOffset(int planeIndex)
{
    if (planeIndex < 3)
    {
        // 4열 그리드라고 가정 (0.0, 0.25, 0.5 ...)
        return vec2(float(planeIndex) * 0.25, 0.0);
    }
    return vec2(0.0, 0.0);
}

// 아틀라스 UV 크기 계산
vec2 GetAtlasSize(int planeIndex)
{
    return vec2(0.25, 1.0);
}

// 쿼드 방출 함수 (최적화: 이미 변환된 벡터들을 인자로 받음)
void EmitQuad(vec3 center, vec3 right, vec3 up, float hSize, float vSize, 
              vec3 color, int planeIndex, vec3 normal) 
{
    vec3 halfRight = right * hSize;
    vec3 halfUp = up * vSize;
    
    vec2 uvOffset = GetAtlasOffset(planeIndex);
    vec2 uvSize = GetAtlasSize(planeIndex);
    
    // Tangent와 Bitangent는 쿼드의 Right, Up 벡터와 동일
    vec3 T = right; 
    vec3 B = up;   

    // 1. 좌하 (Left-Bottom)
    vec3 pos0 = center - halfRight;
    vWorldPos = pos0;
    vViewPos = (camera.view * vec4(pos0, 1.0)).xyz;
    vColor = color;
    vNormal = normal;
    vTangent = T;
    vBitangent = B;
    fTexCoord = uvOffset;
    fPlaneIndex = planeIndex;
    gl_Position = camera.vp * vec4(pos0, 1.0);
    EmitVertex();
    
    // 2. 우하 (Right-Bottom)
    vec3 pos1 = center + halfRight;
    vWorldPos = pos1;
    vViewPos = (camera.view * vec4(pos1, 1.0)).xyz;
    vColor = color;
    vNormal = normal;
    vTangent = T;
    vBitangent = B;
    fTexCoord = uvOffset + vec2(uvSize.x, 0.0);
    fPlaneIndex = planeIndex;
    gl_Position = camera.vp * vec4(pos1, 1.0);
    EmitVertex();
    
    // 3. 좌상 (Left-Top)
    vec3 pos2 = center - halfRight + (2.0 * halfUp); // 중심이 바닥 기준이므로 위로 2배
    vWorldPos = pos2;
    vViewPos = (camera.view * vec4(pos2, 1.0)).xyz;
    vColor = color;
    vNormal = normal;
    vTangent = T;
    vBitangent = B;
    fTexCoord = uvOffset + vec2(0.0, uvSize.y);
    fPlaneIndex = planeIndex;
    gl_Position = camera.vp * vec4(pos2, 1.0);
    EmitVertex();
    
    // 4. 우상 (Right-Top)
    vec3 pos3 = center + halfRight + (2.0 * halfUp);
    vWorldPos = pos3;
    vViewPos = (camera.view * vec4(pos3, 1.0)).xyz;
    vColor = color;
    vNormal = normal;
    vTangent = T;
    vBitangent = B;
    fTexCoord = uvOffset + uvSize;
    fPlaneIndex = planeIndex;
    gl_Position = camera.vp * vec4(pos3, 1.0);
    EmitVertex();
    
    EndPrimitive();
}

void main() 
{
    // 입력 데이터 (Points는 1개만 들어옴)
    vec3 worldPos = gs_in[0].worldPos;
    vec3 color = gs_in[0].color;
    float hSize = gs_in[0].horizontalSize;
    float vSize = gs_in[0].verticalSize;
    
    // 행렬 추출 (최적화)
    mat3 worldRot = mat3(gs_in[0].transform); 
    mat3 normMat = mat3(gs_in[0].normalMat);
    
    // 각도 설정 (0도, 60도, 120도)
    float angles[3] = float[3](0.0, 60.0, 120.0);
    
    for (int i = 0; i < 3; i++)
    {
        float angleRad = radians(angles[i]);
        float c = cos(angleRad);
        float s = sin(angleRad);
        
        // 1. 로컬 축 생성
        // Right: 회전 적용 (X, Y 평면 회전)
        vec3 localRight = vec3(c, s, 0.0);
        // Up: 항상 Z축 위쪽 (Billboard 식물 가정)
        vec3 localUp = vec3(0.0, 0.0, 1.0);
        
        // 2. 월드 공간으로 변환 (Tangent, Bitangent 용)
        vec3 worldRight = normalize(worldRot * localRight);
        vec3 worldUp    = normalize(worldRot * localUp);
        
        // 3. 노말 계산
        // 로컬 노말: Right x Up (CCW 기준, 앞면)
        vec3 localNormal = cross(localRight, localUp);
        
        // 월드 노말: Normal Matrix 적용 (비균등 스케일 대응)
        vec3 worldNormal = normalize(normMat * localNormal);
        
        // 4. 쿼드 생성 함수 호출
        EmitQuad(worldPos, worldRight, worldUp, hSize, vSize, color, i, worldNormal);
    }
}