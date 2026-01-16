#version 430 core
uniform sampler2D noiseTexture;

in vec2 TexCoord;
out vec4 fragColor;

uniform bool useColorMap = false;
uniform bool flip = false;

void main(void)
{
    float noise = texture(noiseTexture, TexCoord).r;
    
    if (flip) {
        noise = 1.0 - noise;	
	}

    if (!useColorMap){
        // 그레이스케일 표시
        fragColor = vec4(noise, noise, noise, 1.0);
    } else {
        // 또는 컬러맵 적용 (물-모래-풀-산-눈)
        if (noise < 0.3) 
            fragColor = vec4(30.0/255.0, 100.0/255.0, 200.0/255.0, 1.0); // 물
        else if (noise < 0.4) 
            fragColor = vec4(220.0/255.0, 200.0/255.0, 120.0/255.0, 1.0); // 모래
        else if (noise < 0.7) 
            fragColor = vec4(50.0/255.0, 150.0/255.0, 50.0/255.0, 1.0); // 풀
        else if (noise < 0.9) 
            fragColor = vec4(100.0/255.0, 80.0/255.0, 60.0/255.0, 1.0); // 산
        else 
            fragColor = vec4(1.0, 1.0, 1.0, 1.0); // 눈
    }    
}