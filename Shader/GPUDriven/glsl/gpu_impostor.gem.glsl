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
    vec3 viewPos;
    vec3 worldPos;
    vec3 tangent;
    vec3 bitangent;
    vec3 normal;
} gs_out;

layout(std140, binding = 0) uniform CameraBlock {
    mat4 view; 
    mat4 proj; 
    mat4 vp;
    vec3 cameraPos;
} camera;

// ✅ 수정: 너비와 높이를 따로 받음
uniform float billboardWidth;   // BoundingSphereRadius × 2
uniform float billboardHeight;  // ActualHeight

uniform float atlasSize;
uniform float individualSize;
uniform int horizontalFrames;
uniform int verticalFrames;

const float PI = 3.14159265359;

vec3 extractScale(mat4 modelMatrix)
{
    vec3 scaleX = vec3(modelMatrix[0][0], modelMatrix[0][1], modelMatrix[0][2]);
    vec3 scaleY = vec3(modelMatrix[1][0], modelMatrix[1][1], modelMatrix[1][2]);
    vec3 scaleZ = vec3(modelMatrix[2][0], modelMatrix[2][1], modelMatrix[2][2]);
    return vec3(length(scaleX), length(scaleY), length(scaleZ));
}

vec3 getModelForward(mat4 modelMatrix)
{
    return normalize(-vec3(modelMatrix[1][0], modelMatrix[1][1], modelMatrix[1][2]));
}

vec3 getModelRight(mat4 modelMatrix)
{
    return normalize(-vec3(modelMatrix[0][0], modelMatrix[0][1], modelMatrix[0][2]));
}

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
    
    vec3 camPos = camera.cameraPos.xyz; 
    vec3 toCamera = normalize(camPos - worldPosition);
    
    vec3 localViewDir = getLocalViewDirection(modelMatrix, toCamera);
    vec2 instanceAtlasOffset = calculateAtlasOffset(localViewDir);
    
    // ✅ 빌보드 축 계산 (너비와 높이를 따로 적용)
    vec3 worldUp = vec3(0.0, 0.0, 1.0);
    vec3 tempRight = cross(worldUp, toCamera);
    float rightLength = length(tempRight);
    
    if (rightLength < 0.001) {
        tempRight = cross(vec3(1.0, 0.0, 0.0), toCamera);
        rightLength = length(tempRight);
    }
    
    // ✅ 수정: 너비와 높이를 반씩 나눔 (halfWidth, halfHeight)
    vec3 right = (tempRight / rightLength) * (billboardWidth * 0.5);  // ✅ 반너비
    vec3 up = normalize(cross(toCamera, right)) * billboardHeight;     // ✅ 전체 높이
    
    // TBN 계산
    vec3 billboardTangent = normalize(right);
    vec3 billboardBitangent = normalize(up);
    vec3 billboardNormal = toCamera;

    // ✅ Bottom Pivot 기준 빌보드 위치
    vec3 positions[4];
    positions[0] = worldPosition - right;        // Bottom Left
    positions[1] = worldPosition + right;        // Bottom Right
    positions[2] = worldPosition - right + up;   // Top Left
    positions[3] = worldPosition + right + up;   // Top Right

    const vec2 texCoords[4] = vec2[4](
        vec2(0.0, 0.0), vec2(1.0, 0.0),
        vec2(0.0, 1.0), vec2(1.0, 1.0)
    );
    
    for (int i = 0; i < 4; i++) {
        gl_Position = camera.vp * vec4(positions[i], 1.0);
        
        gs_out.texCoord = texCoords[i];
        gs_out.atlasOffset = instanceAtlasOffset;
        gs_out.viewPos = (camera.view * vec4(positions[i], 1.0)).xyz;
        gs_out.worldPos = positions[i];
        
        gs_out.tangent = billboardTangent;
        gs_out.bitangent = billboardBitangent;
        gs_out.normal = billboardNormal;

        EmitVertex();
    }
    
    EndPrimitive();
}