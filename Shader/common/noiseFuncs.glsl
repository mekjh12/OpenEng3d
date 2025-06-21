#version 430

//-----------------------------------------------------------------------------
// Maths Utils
//-----------------------------------------------------------------------------

// Hash without Sine linked to https://www.shadertoy.com/view/4djSRW
float hash(float p) { 
	p = fract(p * 0.011); 
	p *= p + 7.5; 
	p *= p + p; 
	return fract(p); 
}

// 
float hash(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.13); 
	p3 += dot(p3, p3.yzx + 3.333); 
	return fract((p3.x + p3.y) * p3.z); 
}

// 
float noise(vec3 x) {
    const vec3 step = vec3(110, 241, 171);

    vec3 i = floor(x);
    vec3 f = fract(x);
 
    // For performance, compute the base input to a 1D hash from the integer part of the argument and the 
    // incremental change to the 1D based on the 3D -> 1D wrapping
    float n = dot(i, step);

    vec3 u = f * f * (3.0 - 2.0 * f);
    return mix(mix(mix( hash(n + dot(step, vec3(0, 0, 0))), hash(n + dot(step, vec3(1, 0, 0))), u.x),
                   mix( hash(n + dot(step, vec3(0, 1, 0))), hash(n + dot(step, vec3(1, 1, 0))), u.x), u.y),
               mix(mix( hash(n + dot(step, vec3(0, 0, 1))), hash(n + dot(step, vec3(1, 0, 1))), u.x),
                   mix( hash(n + dot(step, vec3(0, 1, 1))), hash(n + dot(step, vec3(1, 1, 1))), u.x), u.y), u.z);
}

float fbm(vec3 x, int octaves) 
{
	float v = 0.0;
	float a = 0.5;
	vec3 shift = vec3(100);
	for (int i = 0; i < octaves; i++) 
	{
		v += a * noise(x);
		x = x * 2.0 + shift;
		a *= 0.5;
	}
	return v;
}


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


float fbm(in vec2 st, in float amplitude, in float persistence, in float frequency, in int octaves)
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