#version 450 core

in vec3 vColor;
uniform vec3 normalColor;

out vec4 fragColor;

void main() 
{
    // uniform 색상이 설정되어 있으면 사용
    vec3 finalColor = normalColor;
    
    // normalColor가 (0,0,0)이면 geometry shader의 색상 사용
    if (dot(normalColor, normalColor) < 0.01) {
        finalColor = vColor;
    }
    
    fragColor = vec4(finalColor, 1.0);
}