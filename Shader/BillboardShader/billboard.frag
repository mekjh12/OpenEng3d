#version 420 core

uniform sampler2D gColorMap;
uniform vec3 gCameraPos;

uniform vec3 fogColor;
uniform float fogDensity;
uniform vec4 fogPlane;

in vec2 TexCoord;
in vec4 FragPos;
out vec4 FragColor;

// ================================================================
// 픽셀에 세이더 색상으로부터 안개를 적용하여 반환한다.
// param : shadedColor 세이더한 픽셀의 색상
//         v  정규화되지 않은 뷰벡터 v 
// ================================================================
vec3 ApplyHalfspaceFog(vec3 shadedColor, vec3 fogcolor, vec3 v, float density, float fv, float u1, float u2)
{
    const float kFogEpsilon = 0.0001f;
    float x = min(u2, 0.0f);
    float tau = 0.5f * density * length(v) * (u1 -  x * x / (abs(fv) + kFogEpsilon));
    return mix(fogcolor, shadedColor, exp(tau));
}
                                                                                    
void main()                                                                         
{                                                                                   
    vec4 textureColor4 = texture2D(gColorMap, TexCoord);                                                                                                                         
    if (textureColor4.a < 0.5f) discard;    
       
    float fc = dot(gCameraPos, fogPlane.xyz) + fogPlane.w;
    float fp = dot(FragPos.xyz, fogPlane.xyz) + fogPlane.w;
    vec3 v = gCameraPos - FragPos.xyz;
    float fv = dot(v, fogPlane.xyz);
    float m = (fc<0) ? 1.0f: 0.0f;
    float u1 = m * (fc + fp);
    float u2 = fp * sign(fc);

    vec3 final = ApplyHalfspaceFog(textureColor4.xyz, fogColor, v, fogDensity, fv, u1, u2);
    FragColor = vec4(final, 1.0f);

}
