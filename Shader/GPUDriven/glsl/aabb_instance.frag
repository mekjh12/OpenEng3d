#version 450 core
in vec3 vNormal;

uniform vec3 boxColor;
uniform float alpha;
uniform uint currentBatchID;

out vec4 FragColor;

void main() 
{
    vec3 finalColor = boxColor;    
    FragColor = vec4(finalColor, alpha);
}