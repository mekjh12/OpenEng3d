#version 430 core

const float PI = 3.1415926535897932384626433832795;
const float AMOUNT_LOD0 = 1.0f;
const float AMOUNT_LOD1 = 3.0f;
const float AMOUNT_LOD2 = 6.0f;

uniform sampler2D noiseTexture;
uniform sampler2D heightMapTexture;
uniform float scaled;
uniform int flip;
uniform bool useHeightMap;
uniform bool useGrayMode = false;

in vec2 TexCoord;
out vec4 fragColor;

void main(void)
{
    vec2 uv = TexCoord;
    
    // Y축 flip 적용
    if (flip == 1) {
        uv.y = 1.0 - uv.y;
    }
    
    vec4 heightData = texture(heightMapTexture, uv);
    vec4 waterData = texture(noiseTexture, uv);


    // 그레이 모드일 경우 함수 종료
    if (useGrayMode)
    {
        fragColor = vec4(heightData.x, 0, 0, 1.0);
        return;
    }

    // 물 높이에 따른 색상 결정
    float waterLevel = waterData.r;
    vec3 finalColor = vec3(0.0);

    if (waterLevel < AMOUNT_LOD0) {
		finalColor = vec3(0.0, 0.0, waterLevel);
	} else if (waterLevel < AMOUNT_LOD1) {
		finalColor = vec3(0.0, waterLevel, waterLevel);
    } else if (waterLevel > AMOUNT_LOD2 ) {
		finalColor = vec3(waterLevel, waterLevel, waterLevel);
    } else {
		finalColor = vec3(0.0, waterLevel, 0.0);
    }

    if (useHeightMap)
    {
        vec3 heightColor = heightData.rgb / 255.0f;
        fragColor = vec4(finalColor * scaled + heightColor, 1.0);
	}
    else
    {
        fragColor = vec4(finalColor * scaled, 1.0);
    }
}