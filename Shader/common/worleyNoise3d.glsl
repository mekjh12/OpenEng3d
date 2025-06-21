#version 430

// Based on tileable Cells. By David Hoskins. 2013.
// https://www.shadertoy.com/view/4djGRh

//#define NUM_CELLS	5.0	// Needs to be a multiple of TILES!
#define TILES 		2.0		// Normally set to 1.0 for a creating a tileable texture.

//해쉬로부터 값을 가져온다.
float hash(float n)
{
	return fract( sin(n) * 43758.5453 );
}

// hash based 3d value noise
float noise(in vec3 x)
{
	vec3 p = floor(x);
	vec3 f = fract(x);

	f = f*f*(3.0 - 2.0 * f); // 감마조정
	float n = p.x + p.y*57.0 + 113.0*p.z;
	return mix(
			mix(
				mix(hash(n + 0.0), hash(n + 1.0), f.x),
				mix(hash(n + 57.0), hash(n + 58.0), f.x),
				f.y),
			mix(
				mix(hash(n + 113.0), hash(n + 114.0), f.x),
				mix(hash(n + 170.0), hash(n + 171.0), f.x),
				f.y),
			f.z);
}

// WorleyNoise를 가져온다.
float Cells(in vec3 p, in float numCells)
{
	p *= numCells;
	float d = 1.0e10;
	for (int xo = -1; xo <= 1; xo++)
	{
		for (int yo = -1; yo <= 1; yo++)
		{
            for (int zo = -1; zo <= 1; zo++)
            {
                vec3 tp = floor(p) + vec3(xo, yo, zo);
                tp = p - tp - noise(mod(tp, numCells / TILES));
                d = min(d, dot(tp, tp));
            }
		}
	}

    float f = mix(0.0, 0.6, noise(p * 5.0));
	return smoothstep(-4.0, 6.0, 1.0 - d  * 3.7 + f);
}