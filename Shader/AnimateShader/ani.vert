#version 420 core

const int MAX_JOINTS = 128;
const int MAX_WEIGHTS = 4;

layout(location = 0) in vec3 in_position;
layout(location = 1) in vec2 in_textureCoords;
layout(location = 2) in vec3 in_normal;
layout(location = 4) in vec4 in_jointIndices;
layout(location = 5) in vec4 in_weights;

out vec3 pass_normals;
out vec2 pass_textureCoords;
out vec4 pass_weights;

uniform mat4 finalAnimatedBoneMatrix[MAX_JOINTS]; // 뼈대 변환 행렬들
uniform mat4 proj;
uniform mat4 view;
uniform mat4 model;
uniform mat4 pmodel;
uniform vec3 lightDirection;

void main(void)
{	
	vec4 totalLocalPos = vec4(0.0);
	vec4 totalNormal = vec4(0.0);
	
	    // 가중치 정규화 체크
    float weightSum = 0.0;
    for (int i = 0; i < MAX_WEIGHTS; i++)
    {
        weightSum += in_weights[i];
    }

    for (int i = 0; i < MAX_WEIGHTS; i++)
    {
        float weight = in_weights[i];
        if (weight > 0.0) // 0인 가중치는 스킵
        {
            int index = int(in_jointIndices[i]);
            mat4 jointTransform = finalAnimatedBoneMatrix[index];
            mat4 transform = jointTransform * pmodel;
        
            // 정점 변환
            vec4 posePosition = transform * vec4(in_position, 1.0);
            totalLocalPos += posePosition * weight;
        
            // 법선 변환
            mat3 normalMatrix = mat3(transpose(inverse(transform)));
            vec4 worldNormal = vec4(normalMatrix * in_normal, 0.0);
            totalNormal += worldNormal * weight;
        }
    }

    // 가중치가 1이 아닌 경우 정규화
    if (abs(weightSum - 1.0) > 0.001)
    {
        totalLocalPos /= weightSum;
        totalNormal /= weightSum;
    }    
    
    // 최종 변환
    gl_Position = proj * view * model * totalLocalPos;
    pass_normals = normalize(totalNormal.xyz);
    pass_textureCoords = in_textureCoords;
    pass_weights = in_weights;
}