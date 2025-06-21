#version 430

in vec2 texCoords;
in vec4 fcolor;
in vec3 fragPos;
in vec4 finalPosition;

layout (location = 0) out vec4 out_Color;
layout (location = 1) out float out_Depth;

uniform sampler2D modelTexture;
uniform bool isTextured;
uniform bool isAttribColored;
uniform vec4 color;
uniform vec3 camPos;
uniform vec3 fogColor;
uniform float fogDensity;
uniform vec4 fogPlane;
uniform bool isFogEnable;


// ================================================================
// 픽셀에 세이더 색상으로부터 안개를 적용하여 반환한다.
// param : shadedColor 세이더한 픽셀의 색상
//         v  정규화되지 않은 뷰벡터 v 
// ================================================================
vec3 ApplyFog(vec3 shadedColor, vec3 fogcolor, vec3 v, float density)
{
	float f = exp(-density * length(v));
	return mix(fogcolor, shadedColor, f);
}

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

void main(void)
{
	vec4 textureColor4 = texture(modelTexture, texCoords);

	if (textureColor4.a < 0.05f) discard;

	if (isFogEnable) 
	{
		float fc = dot(camPos, fogPlane.xyz) + fogPlane.w;
		float fp = dot(fragPos.xyz, fogPlane.xyz) + fogPlane.w;
		vec3 v = camPos - fragPos.xyz;
		float fv = dot(v, fogPlane.xyz);
		float m = (fc<0) ? 1.0f: 0.0f;
		float u1 = m * (fc + fp);
		float u2 = fp * sign(fc);

		vec3 final = ApplyHalfspaceFog(textureColor4.xyz, fogColor, v, fogDensity, fv, u1, u2);
		out_Color = isTextured ? color * vec4(final,1) : color;
		if (isAttribColored) out_Color = fcolor;
	}
	else
	{
		out_Color = textureColor4;
		return;
	}

	//gl_FragDepth = fragPos.z / 1000.0f;
}