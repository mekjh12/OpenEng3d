#version 430
layout (points) in;
layout (triangle_strip, max_vertices = 4) out;

in VS_OUT {
    vec3 worldPosition;
    mat4 modelMatrix;
} gs_in[];

out GS_OUT {
    vec2 texCoord;
    vec2 atlasOffset;
} gs_out;

uniform mat4 vp;
uniform vec3 cameraPosition;
uniform float atlasSize;
uniform float individualSize;
uniform int horizontalFrames;
uniform int verticalFrames;
uniform float aabbSphereRadius;
uniform vec3 aabbCenterPosition;
uniform vec2 verticalBoundAngle;

const float PI = 3.14159265359;

// 모델 행렬에서 스케일 추출
vec3 extractScale(mat4 modelMatrix)
{
    vec3 scaleX = vec3(modelMatrix[0][0], modelMatrix[0][1], modelMatrix[0][2]);
    vec3 scaleY = vec3(modelMatrix[1][0], modelMatrix[1][1], modelMatrix[1][2]);
    vec3 scaleZ = vec3(modelMatrix[2][0], modelMatrix[2][1], modelMatrix[2][2]);
    
    return vec3(length(scaleX), length(scaleY), length(scaleZ));
}

// 모델 행렬에서 회전 행렬 추출 (스케일 제거)
mat3 getRotationMatrix(mat4 modelMatrix, vec3 scale)
{
    mat3 rotation;
    rotation[0] = vec3(modelMatrix[0][0], modelMatrix[0][1], modelMatrix[0][2]) / scale.x;
    rotation[1] = vec3(modelMatrix[1][0], modelMatrix[1][1], modelMatrix[1][2]) / scale.y;
    rotation[2] = vec3(modelMatrix[2][0], modelMatrix[2][1], modelMatrix[2][2]) / scale.z;
    return rotation;
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
    verticalAngle *= 180.0 / PI;

    float normalizedH = (horizontalAngle + PI) / (2.0 * PI);
    float normalizedV = (verticalAngle - verticalBoundAngle.x) / (verticalBoundAngle.y - verticalBoundAngle.x);
    
    int frameX = int(normalizedH * float(horizontalFrames)) % horizontalFrames;
    int frameY = int(normalizedV * float(verticalFrames)) % verticalFrames;
    
    if (frameY < 0) frameY = 0;

    float frameSize = individualSize / atlasSize;
    return vec2(float(frameX) * frameSize, float(frameY) * frameSize);
}

void main()
{
    vec3 worldPosition = gs_in[0].worldPosition;
    mat4 modelMatrix = gs_in[0].modelMatrix;
    
    // 1. 모델 행렬에서 스케일 추출
    vec3 modelScale = extractScale(modelMatrix);
    float scaled = modelScale.x; // x축 스케일 사용 (균일 스케일 가정)
    
    // 2. 회전 행렬 추출
    mat3 rotationMatrix = getRotationMatrix(modelMatrix, modelScale);
    
    // 3. AABB 중심을 월드 공간으로 변환 (스케일 → 회전 → 이동)
    vec3 scaledCenter = aabbCenterPosition * scaled;
    vec3 rotatedCenter = rotationMatrix * scaledCenter;
    vec3 worldCenter = worldPosition + rotatedCenter;
    
    // 4. 카메라 방향 계산 (월드 중심 기준)
    vec3 toCamera = normalize(cameraPosition - worldCenter);
    
    // 5. 모델의 회전을 고려한 로컬 뷰 방향 계산 및 atlas offset 계산
    vec3 localViewDir = getLocalViewDirection(modelMatrix, toCamera);    
    vec2 instanceAtlasOffset = calculateAtlasOffset(localViewDir);
    
    // 6. 빌보드 방향 벡터 계산
    vec3 worldUp = vec3(0.0, 0.0, 1.0);
    vec3 tempRight = cross(worldUp, toCamera);
    float rightLength = length(tempRight);
    
    if (rightLength < 0.001) {
        tempRight = cross(vec3(1.0, 0.0, 0.0), toCamera);
        rightLength = length(tempRight);
    }
    
    float scaledAABBRadius = aabbSphereRadius * scaled;
    vec3 right = (tempRight / rightLength) * scaledAABBRadius;
    vec3 up = normalize(cross(toCamera, right)) * scaledAABBRadius;
    
    // 7. 빌보드 네 모서리 위치 (worldCenter 기준)
    vec3 positions[4];
    positions[0] = worldCenter - right - up;
    positions[1] = worldCenter + right - up;
    positions[2] = worldCenter - right + up;
    positions[3] = worldCenter + right + up;
    
    const vec2 texCoords[4] = vec2[4](
        vec2(0.0, 0.0),
        vec2(1.0, 0.0),
        vec2(0.0, 1.0),
        vec2(1.0, 1.0)
    );
    
    // 8. 정점 출력
    for (int i = 0; i < 4; i++) 
    {
        gl_Position = vp * vec4(positions[i], 1.0);
        gs_out.texCoord = texCoords[i];
        gs_out.atlasOffset = instanceAtlasOffset;
        EmitVertex();
    }
    
    EndPrimitive();
}