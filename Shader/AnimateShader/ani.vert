#version 420 core

// 상수 정의
const int MAX_JOINTS = 128;    // 최대 조인트 수
const int MAX_WEIGHTS = 4;     // 정점당 최대 가중치 수

// 입력 속성
layout(location = 0) in vec3 in_position;      // 정점 위치
layout(location = 1) in vec2 in_textureCoords; // 텍스처 좌표
layout(location = 2) in vec3 in_normal;        // 정점 법선
layout(location = 4) in vec4 in_jointIndices;  // 조인트 인덱스들
layout(location = 5) in vec4 in_weights;       // 조인트 가중치들

// 출력 속성
out vec3 pass_normals;         // 프래그먼트 셰이더로 전달할 법선
out vec2 pass_textureCoords;   // 프래그먼트 셰이더로 전달할 텍스처 좌표
out vec4 pass_weights;         // 프래그먼트 셰이더로 전달할 가중치

// 유니폼 변수
uniform mat4 finalAnimatedBoneMatrix[MAX_JOINTS]; // 애니메이션된 뼈대 변환 행렬들
uniform mat4 proj;             // 투영 행렬
uniform mat4 view;             // 뷰 행렬
uniform mat4 model;            // 모델 행렬
uniform mat4 mvp;               // 모델-뷰-투영 행렬
uniform vec3 lightDirection;   // 조명 방향
uniform bool isSkinningEnabled; // 스키닝 활성화 여부
uniform int rigidBoneIndex;     // 강체 본 인덱스 (스키닝 비활성화 시 사용)

void main(void)
{	
    vec4 totalLocalPos = vec4(0.0);  // 최종 변환된 정점 위치
    vec4 totalNormal = vec4(0.0);    // 최종 변환된 법선
    
    if (isSkinningEnabled)
    {
        // ---------------------------------------------------------------
        // 스키닝 활성화: 가중치 기반 다중 본 변환
        // ---------------------------------------------------------------
        
        // 가중치 합 계산 (정규화용)
        float weightSum = 0.0;
        for (int i = 0; i < MAX_WEIGHTS; i++)
        {
            weightSum += in_weights[i];
        }
        
        // 각 조인트별 변환 적용
        for (int i = 0; i < MAX_WEIGHTS; i++)
        {
            float weight = in_weights[i];
            if (weight > 0.0) // 0인 가중치는 스킵
            {
                int index = int(in_jointIndices[i]);
                mat4 jointTransform = finalAnimatedBoneMatrix[index];
                mat4 transform = jointTransform;
            
                // 정점 위치 변환
                vec4 posePosition = transform * vec4(in_position, 1.0);
                totalLocalPos += posePosition * weight;
            
                // 법선 변환 (역전치 행렬 사용)
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
    }
    else
    {
        // ---------------------------------------------------------------
        // 스키닝 비활성화: 단일 강체 본 변환
        // ---------------------------------------------------------------
        mat4 jointTransform = finalAnimatedBoneMatrix[rigidBoneIndex];
        mat4 transform = jointTransform;
        
        // 정점 위치 변환
        totalLocalPos = transform * vec4(in_position, 1.0);
        
        // 법선 변환 (역전치 행렬 사용)
        mat3 normalMatrix = mat3(transpose(inverse(transform)));
        totalNormal = vec4(normalMatrix * in_normal, 0.0);
    }
    
    // 최종 변환: 모델-뷰-투영 변환 적용
    gl_Position = mvp * totalLocalPos;
    
    // 프래그먼트 셰이더로 데이터 전달
    pass_normals = normalize(totalNormal.xyz);
    pass_textureCoords = in_textureCoords;
    pass_weights = in_weights;
}