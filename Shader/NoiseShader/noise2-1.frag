#version 430

layout(std430, binding=0) buffer shader_data
{
	float point[];
};

uniform vec2 viewport_size;
uniform float amplitude;
uniform float frequency;
uniform int octaves;
uniform float persistence;
uniform int interpolationMode;

in vec3 WorldPos;

layout(location = 0) out vec4 FragColor;

float random(in vec2 st)
{
	return 2.0f * fract(sin(dot(st.xy, vec2(12.9898,78.233)))*43758.5453123) - 1.5f;
}


float noise(in vec2 st)
{
	vec2 i = floor(st);
	vec2 f = fract(st);

	float a = random(i);
	float b = random(i + vec2(1,0));
	float c = random(i + vec2(0,1));
	float d = random(i + vec2(1,1));

	vec2 u = f * f * (3.0 - 2.0 * f);

	return mix(a, b, u.x) + (c-a)*u.y * (1.0f-u.x) + (d-b)*u.x*u.y;
}

float fbm(in vec2 st)
{
	float total = 0.0f;
	float amp = amplitude;
	float fre = frequency;
	for(int i=0; i<octaves; i++)
	{
		total += amp * abs(noise(st * fre));
		fre *= 2;
		amp *= persistence;
	}
	return total;
}

void main()
{
	vec2 st =  gl_FragCoord.xy / viewport_size;
	st.x *= viewport_size.x / viewport_size.y;

	vec3 color = vec3(0.0);
	color += fbm(st*3.0);
    FragColor = vec4(color, 1);
}