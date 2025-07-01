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

uniform mat4 jointTransforms[MAX_JOINTS]; // 뼈대 변환 행렬들
uniform mat4 proj;
uniform mat4 view;
uniform mat4 model;
uniform mat4 pmodel;
uniform vec3 lightDirection;

uniform bool isOnlyOneJointWeight; // 하나의 뼈대에 가중치가 하나만 있는 경우
uniform int jointIndex; // 하나의 뼈대에 가중치가 하나만 있는 경우의 뼈대 인덱스

void main(void)
{	
	vec4 totalLocalPos = vec4(0.0);
	vec4 totalNormal = vec4(0.0);
	
	// 하나의 뼈대에 가중치가 하나만 있는 경우
	if (isOnlyOneJointWeight)
	{
		mat4 jointTransform = jointTransforms[jointIndex];
		mat4 transform = jointTransform * pmodel;
		mat3 T = mat3(transpose(inverse(transform)));

		vec4 posePosition = transform * vec4(in_position, 1.0);
		totalLocalPos = posePosition;

		vec4 worldNormal = vec4(T * in_normal, 0.0);
		totalNormal = normalize(worldNormal);
	}
	// 여러 뼈대에 가중치가 있는 경우
	else
	{
		for (int i=0; i< MAX_WEIGHTS; i++)
		{
			int index = int(in_jointIndices[i]);
			//if (index == 0) continue;
			mat4 jointTransform = jointTransforms[index];

			mat4 transform = jointTransform * pmodel;
			mat3 T = mat3(transpose(inverse(transform)));

			vec4 posePosition = transform * vec4(in_position, 1.0);
			totalLocalPos += posePosition * in_weights[i];
		
			vec4 worldNormal = vec4(T * in_normal, 0.0);
			totalNormal += worldNormal * in_weights[i];
		}
	}

	// 최종 위치 계산
	gl_Position = proj * view * model * totalLocalPos;

	pass_normals = totalNormal.xyz;
	pass_textureCoords = in_textureCoords;
	pass_weights = in_weights;
}