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


float Noise(int x, int y)
{
	int n = int(sqrt(point.length()));
	x += 100*n;
	x %= n;
	y += 100*n;
	y %= n;
	return point[x + y*n];
}

float SmoothedNoise2D(int x, int y)
{
	int len = point.length();
	x += len * 1000; // x는 항상 양수로 처리한다.
	x = x % len;
	float corners = (Noise(x-1, y-1) + Noise(x+1, y-1) + Noise(x-1, y+1) + Noise(x+1, y+1))/16.0f;
	float sides = (Noise(x-1, y) + Noise(x+1, y) + Noise(x, y-1) + Noise(x, y+1))/8.0f;
	float center = Noise(x, y) / 4.0f;
	return corners + sides + center;
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

float InterpolaredNoise2D(float x, float y)
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

	int iy = int(floor(y));
	float fy = y-iy;

	if (y<0)
	{
		iy = int(y)-1;
		fy = y - iy; 
	}
	else
	{
		iy = int(floor(y));
		fy = y - iy;
	}

	float v1 = SmoothedNoise2D(ix, iy);
	float v2 = SmoothedNoise2D(ix+1, iy);
	float v3 = SmoothedNoise2D(ix, iy+1);
	float v4 = SmoothedNoise2D(ix+1, iy+1);

	float i1 = LinearInterpolation1D(v1, v2, fx);
	float i2 = LinearInterpolation1D(v3, v4, fx);

	return LinearInterpolation1D(i1, i2, fy);
}

float PerlinNoise2D(float x, float y)
{
	float total = 0.0f;
	float freq = frequency;
	float amp = amplitude;
	for (int i=0; i<octaves; i++)
	{
		freq *= 2.0f;
		amp *= persistence;
		total += InterpolaredNoise2D(x*freq, y*freq) * amp;
	}

	return total;
}

void main()
{
	vec2 py = vec2(0);
	int n = 13;
	// (-1,-1)--(1,1)
	vec2 screenCoord =  2.0f * (gl_FragCoord.xy / viewport_size) - vec2(1.0f);
	float noise = PerlinNoise2D(n * screenCoord.x, n * screenCoord.y);

    FragColor = vec4(noise, noise, noise, 1);
}