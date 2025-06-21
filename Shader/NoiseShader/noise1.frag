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

float SmoothedNoise1D(int x)
{
	int len = point.length();
	x += len * 1000; // x는 항상 양수로 처리한다.
	x = x % len;
	return point[x - 1] * 0.25f + point[x] * 0.5f + point[x + 1] * 0.25f;
}

float LinearInterpolation1D(float x, float y, float t)
{
	return x * (1.0f - t) + y * t;
}

float CosineInterpolation1D(float x, float y, float t)
{
	float ft = t * 3.14159227f;
	float f = (1-cos(ft)) * 0.5f;
	return x * (1.0f - f) + y * f;
}

float InterpolaredNoise1D(float x)
{
	int ix = int(floor(x));
	float fx = x-ix;

	if (x<0)
	{
		ix = int(x)-1;
		fx = x - ix; 
	}
	else
	{
		ix = int(floor(x));
		fx = x - ix;
	}

	float v1 = SmoothedNoise1D(ix);
	float v2 = SmoothedNoise1D(ix+1);

	if (interpolationMode==0)
	{
		return LinearInterpolation1D(v1, v2, fx);
	}
	else
	{
		return CosineInterpolation1D(v1, v2, fx);
	}
}

float PerlinNoise1D(float x)
{
	float total = 0.0f;
	float freq = frequency;
	float amp = amplitude;
	for (int i=0; i<octaves; i++)
	{
		freq *= 2.0f;
		amp *= persistence;
		total += InterpolaredNoise1D(x*freq) * amp;
	}

	return total;
}

void main()
{
	vec2 py = vec2(0);

	uint N = point.length();
	int n = int((N-1)*0.5f);
	// (-1,-1)--(1,1)
	vec2 screenCoord =  2.0f * (gl_FragCoord.xy / viewport_size) - vec2(1.0f);
	py.y = PerlinNoise1D(n * screenCoord.x);

    FragColor = vec4(0, 0, 0, 1);
	if (int(200.0f * py.y)==int(200.0f * screenCoord.y))
	{
		FragColor = vec4(1,1,0,1);
	}
}