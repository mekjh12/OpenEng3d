#version 450 core
layout(points) in;
layout(line_strip, max_vertices = 6) out;

uniform mat4 vp;
uniform float axisLength;

out vec3 vColor;

void main() 
{
    vec3 origin = vec3(0.0, 0.0, 0.0);
    
    // X축 (빨간색)
    vColor = vec3(1.0, 0.0, 0.0);
    gl_Position = vp * vec4(origin, 1.0);
    EmitVertex();
    gl_Position = vp * vec4(origin + vec3(axisLength, 0.0, 0.0), 1.0);
    EmitVertex();
    EndPrimitive();
    
    // Y축 (초록색)
    vColor = vec3(0.0, 1.0, 0.0);
    gl_Position = vp * vec4(origin, 1.0);
    EmitVertex();
    gl_Position = vp * vec4(origin + vec3(0.0, axisLength, 0.0), 1.0);
    EmitVertex();
    EndPrimitive();
    
    // Z축 (파란색)
    vColor = vec3(0.0, 0.0, 1.0);
    gl_Position = vp * vec4(origin, 1.0);
    EmitVertex();
    gl_Position = vp * vec4(origin + vec3(0.0, 0.0, axisLength), 1.0);
    EmitVertex();
    EndPrimitive();
}