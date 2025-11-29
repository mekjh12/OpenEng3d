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
uniform float aabbSizeModel;
uniform vec3 aabbCenterEntity;
uniform float atlasSize;
uniform float individualSize;
uniform int horizontalFrames;  // HorizontalAngles 값
uniform int verticalFrames;    // VerticalAngles 값

const float PI = 3.14159265359;

// 모델 행렬에서 forward 벡터 추출 (-Y축)
vec3 getModelForward(mat4 modelMatrix)
{
    return normalize(-vec3(modelMatrix[1][0], modelMatrix[1][1], modelMatrix[1][2]));
}

// 모델 행렬에서 right 벡터 추출 (-X축)
vec3 getModelRight(mat4 modelMatrix)
{
    return normalize(-vec3(modelMatrix[0][0], modelMatrix[0][1], modelMatrix[0][2]));
}

// 회전을 고려한 상대 카메라 방향 계산
vec3 getLocalViewDirection(mat4 modelMatrix, vec3 toCamera)
{
    // 모델의 로컬 축 추출
    vec3 modelRight = getModelRight(modelMatrix);
    vec3 modelForward = getModelForward(modelMatrix);
    vec3 modelUp = vec3(0.0, 0.0, 1.0);  // Z축은 항상 위
    
    // 카메라 방향을 모델의 로컬 공간으로 변환
    vec3 localView;
    localView.x = dot(toCamera, modelRight);
    localView.y = dot(toCamera, modelForward);
    localView.z = dot(toCamera, modelUp);
    
    return normalize(localView);
}

// Atlas offset 계산
vec2 calculateAtlasOffset(vec3 localViewDir)
{
    // 1. 수평 각도 계산 (XY 평면)
    float horizontalAngle = atan(localViewDir.y, localViewDir.x);
    
    // 2. 수직 각도 계산
    float verticalAngle = asin(clamp(localViewDir.z, -1.0, 1.0));
    
    // 3. 각도를 0~1 범위로 정규화
    float normalizedH = (horizontalAngle + PI) / (2.0 * PI);
    float normalizedV = (verticalAngle + PI * 0.5) / PI;
    
    // 4. 프레임 인덱스 계산
    int frameX = int(normalizedH * float(horizontalFrames)) % horizontalFrames;
    int frameY = int(normalizedV * float(verticalFrames)) % verticalFrames;
    
    // 5. Atlas UV offset 계산
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
    
    // 빌보드 방향 벡터 계산
    vec3 worldUp = vec3(0.0, 0.0, 1.0);
    vec3 tempRight = cross(worldUp, toCamera);
    float rightLength = length(tempRight);
    
    if (rightLength < 0.001) {
        tempRight = cross(vec3(1.0, 0.0, 0.0), toCamera);
        rightLength = length(tempRight);
    }
    
    vec3 right = (tempRight / rightLength) * aabbSizeModel * 0.5f;
    vec3 up = normalize(cross(toCamera, right)) * aabbSizeModel * 0.5f;
    
    vec3 center = worldPosition + aabbCenterEntity;
    
    // 빌보드 네 모서리 위치
    vec3 positions[4];
    positions[0] = center - right - up;
    positions[1] = center + right - up;
    positions[2] = center - right + up;
    positions[3] = center + right + up;
    
    const vec2 texCoords[4] = vec2[4](
        vec2(0.0, 0.0),
        vec2(1.0, 0.0),
        vec2(0.0, 1.0),
        vec2(1.0, 1.0)
    );
    
    for (int i = 0; i < 4; i++) 
    {
        gl_Position = vp * vec4(positions[i], 1.0);
        gs_out.texCoord = texCoords[i];
        gs_out.atlasOffset = instanceAtlasOffset;
        EmitVertex();
    }
    
    EndPrimitive();
}