#version 450 core
layout(points) in;
layout(triangle_strip, max_vertices = 36) out;

uniform mat4 vp;

in VS_OUT {
    vec3 aabbMin;
    vec3 aabbMax;
    uint batchID;
} gs_in[];

out vec3 vColor;
out vec3 vNormal;

void main() 
{
    vec3 minPos = gs_in[0].aabbMin;
    vec3 maxPos = gs_in[0].aabbMax;

    // AABB의 8개 꼭짓점 계산 (이미 월드 공간)
    vec3 v[8];
    v[0] = vec3(minPos.x, minPos.y, minPos.z);
    v[1] = vec3(maxPos.x, minPos.y, minPos.z);
    v[2] = vec3(maxPos.x, maxPos.y, minPos.z);
    v[3] = vec3(minPos.x, maxPos.y, minPos.z);
    v[4] = vec3(minPos.x, minPos.y, maxPos.z);
    v[5] = vec3(maxPos.x, minPos.y, maxPos.z);
    v[6] = vec3(maxPos.x, maxPos.y, maxPos.z);
    v[7] = vec3(minPos.x, maxPos.y, maxPos.z);
    
    // ✅ VP만 적용 (model 제거)
    // 전면 (Front face: -Z)
    vNormal = vec3(0, 0, -1);
    gl_Position = vp * vec4(v[0], 1.0);  // ✅ 수정
    EmitVertex();
    gl_Position = vp * vec4(v[1], 1.0);
    EmitVertex();
    gl_Position = vp * vec4(v[3], 1.0);
    EmitVertex();
    gl_Position = vp * vec4(v[2], 1.0);
    EmitVertex();
    EndPrimitive();
    
    // 후면 (Back face: +Z)
    vNormal = vec3(0, 0, 1);
    gl_Position = vp * vec4(v[5], 1.0);
    EmitVertex();
    gl_Position = vp * vec4(v[4], 1.0);
    EmitVertex();
    gl_Position = vp * vec4(v[6], 1.0);
    EmitVertex();
    gl_Position = vp * vec4(v[7], 1.0);
    EmitVertex();
    EndPrimitive();
    
    // 좌면 (Left face: -X)
    vNormal = vec3(-1, 0, 0);
    gl_Position = vp * vec4(v[4], 1.0);
    EmitVertex();
    gl_Position = vp * vec4(v[0], 1.0);
    EmitVertex();
    gl_Position = vp * vec4(v[7], 1.0);
    EmitVertex();
    gl_Position = vp * vec4(v[3], 1.0);
    EmitVertex();
    EndPrimitive();
    
    // 우면 (Right face: +X)
    vNormal = vec3(1, 0, 0);
    gl_Position = vp * vec4(v[1], 1.0);
    EmitVertex();
    gl_Position = vp * vec4(v[5], 1.0);
    EmitVertex();
    gl_Position = vp * vec4(v[2], 1.0);
    EmitVertex();
    gl_Position = vp * vec4(v[6], 1.0);
    EmitVertex();
    EndPrimitive();
    
    // 하면 (Bottom face: -Y)
    vNormal = vec3(0, -1, 0);
    gl_Position = vp * vec4(v[4], 1.0);
    EmitVertex();
    gl_Position = vp * vec4(v[5], 1.0);
    EmitVertex();
    gl_Position = vp * vec4(v[0], 1.0);
    EmitVertex();
    gl_Position = vp * vec4(v[1], 1.0);
    EmitVertex();
    EndPrimitive();
    
    // 상면 (Top face: +Y)
    vNormal = vec3(0, 1, 0);
    gl_Position = vp * vec4(v[3], 1.0);
    EmitVertex();
    gl_Position = vp * vec4(v[2], 1.0);
    EmitVertex();
    gl_Position = vp * vec4(v[7], 1.0);
    EmitVertex();
    gl_Position = vp * vec4(v[6], 1.0);
    EmitVertex();
    EndPrimitive();
}