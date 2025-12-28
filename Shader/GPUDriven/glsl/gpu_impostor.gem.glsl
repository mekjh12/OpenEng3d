#version 450 core
layout(points) in;
layout(triangle_strip, max_vertices = 4) out;

in VS_OUT 
{
    vec3 worldPosition;
    mat4 modelMatrix;
} gs_in[];

out GS_OUT 
{
    vec2 texCoord;
    vec2 atlasOffset;
    vec3 viewPos;  // ✅ 추가
} gs_out;

uniform mat4 vp;
uniform mat4 view;  // ✅ 추가
uniform vec3 cameraPosition;
uniform float aabbSphereRadius;
uniform float atlasSize;
uniform float individualSize;
uniform int horizontalFrames;
uniform int verticalFrames;

const float PI = 3.14159265359;

// 모델 행렬에서 스케일 추출
vec3 extractScale(mat4 modelMatrix)
{
    vec3 scaleX = vec3(modelMatrix[0][0], modelMatrix[0][1], modelMatrix[0][2]);
    vec3 scaleY = vec3(modelMatrix[1][0], modelMatrix[1][1], modelMatrix[1][2]);
    vec3 scaleZ = vec3(modelMatrix[2][0], modelMatrix[2][1], modelMatrix[2][2]);
    
    return vec3(length(scaleX), length(scaleY), length(scaleZ));
}

// 모델 행렬에서 forward 벡터 추출 (-Y축, 정규화)
vec3 getModelForward(mat4 modelMatrix)
{
    return normalize(-vec3(modelMatrix[1][0], modelMatrix[1][1], modelMatrix[1][2]));
}

// 모델 행렬에서 right 벡터 추출 (-X축, 정규화)
vec3 getModelRight(mat4 modelMatrix)
{
    return normalize(-vec3(modelMatrix[0][0], modelMatrix[0][1], modelMatrix[0][2]));
}

// 회전을 고려한 상대 카메라 방향 계산
vec3 getLocalViewDirection(mat4 modelMatrix, vec3 toCamera)
{
    vec3 modelRight = getModelRight(modelMatrix);
    vec3 modelForward = getModelForward(modelMatrix);
    vec3 modelUp = vec3(0.0, 0.0, 1.0);
    
    vec3 localView;
    localView.x = dot(toCamera, modelRight);
    localView.y = dot(toCamera, modelForward);
    localView.z = dot(toCamera, modelUp);
    
    return normalize(localView);
}

// Atlas offset 계산
vec2 calculateAtlasOffset(vec3 localViewDir)
{
    float horizontalAngle = atan(localViewDir.y, localViewDir.x);
    float verticalAngle = asin(clamp(localViewDir.z, -1.0, 1.0));
    
    float normalizedH = (horizontalAngle + PI) / (2.0 * PI);
    float normalizedV = (verticalAngle + PI * 0.5) / PI;
    
    int frameX = int(normalizedH * float(horizontalFrames)) % horizontalFrames;
    int frameY = int(normalizedV * float(verticalFrames)) % verticalFrames;
    
    float frameSize = individualSize / atlasSize;
    return vec2(float(frameX) * frameSize, float(frameY) * frameSize);
}


void main() 
{
    vec3 worldPosition = gs_in[0].worldPosition;
    mat4 modelMatrix = gs_in[0].modelMatrix;
    
    // 카메라 방향 계산
    vec3 toCamera = normalize(cameraPosition - worldPosition);
    
    // 모델의 회전을 고려한 로컬 뷰 방향 계산
    vec3 localViewDir = getLocalViewDirection(modelMatrix, toCamera);
    
    // 인스턴스별 atlas offset 계산
    vec2 instanceAtlasOffset = calculateAtlasOffset(localViewDir);
    
    // 빌보드 방향 벡터 계산 (Z-up 기준)
    vec3 worldUp = vec3(0.0, 0.0, 1.0);
    vec3 tempRight = cross(worldUp, toCamera);
    float rightLength = length(tempRight);
    
    // 카메라가 정확히 위/아래를 볼 때 대비
    if (rightLength < 0.001) 
    {
        tempRight = cross(vec3(1.0, 0.0, 0.0), toCamera);
        rightLength = length(tempRight);
    }
    
    vec3 right = (tempRight / rightLength) * aabbSphereRadius;
    vec3 up = normalize(cross(toCamera, right)) * aabbSphereRadius;
    
    // 빌보드 네 모서리 위치
    vec3 positions[4];
    positions[0] = worldPosition - right;  // 좌하
    positions[1] = worldPosition + right;  // 우하
    positions[2] = worldPosition - right + 2*up;  // 좌상
    positions[3] = worldPosition + right + 2*up;  // 우상

    const vec2 texCoords[4] = vec2[4](
        vec2(0.0, 0.0),
        vec2(1.0, 0.0),
        vec2(0.0, 1.0),
        vec2(1.0, 1.0)
    );
    
    // Triangle Strip으로 쿼드 생성
    for (int i = 0; i < 4; i++) 
    {
        gl_Position = vp * vec4(positions[i], 1.0);
        gs_out.texCoord = texCoords[i];
        gs_out.atlasOffset = instanceAtlasOffset;
        gs_out.viewPos = (view * vec4(positions[i], 1.0)).xyz;  // ✅ 추가
        EmitVertex();
    }
    
    EndPrimitive();
}