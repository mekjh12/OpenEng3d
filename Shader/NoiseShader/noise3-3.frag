#version 430

layout(binding = 0) uniform sampler3D LowFrequency3DTexture;
layout(binding = 1) uniform sampler3D HighFrequency3DTexture;
layout(binding = 2) uniform sampler2D weathermap;
layout(binding = 3) uniform sampler2D curlNoise;

in vec3 fragPos;
layout (location = 0) out float fragColor;
layout (location = 1) out float depthColor;

uniform mat4 view;
uniform vec3 ray_origin;
uniform vec2 viewport_size;
uniform float focal_length;	
uniform float aspect_ratio;

uniform float frequency;
uniform float amplitude;
uniform int octaves;
uniform float persistence;
uniform int numCells;
uniform int noiseType;
uniform float stepLength;
uniform float cloudtype;
uniform int debug;
uniform bool highQuality;

uniform vec3 cloudPosition;
uniform vec3 cloudSize;



//#include<./../common/boxIntersectFuncs.glsl>
struct Ray { vec3 origin; vec3 direction;};
struct AABB {vec3 top; 	vec3 bottom;};
void ray_box_intersection(Ray ray, AABB box, out float t_0, out float t_1);

// ** Remap function defined by the author of paper
float Remap(float original_value, float original_min, float original_max, float new_min, float new_max)
{
    return new_min + (((original_value - original_min)/(original_max - original_min)) * (new_max - new_min));
}

// ** 구름의 유형에 따라 그라디언트 영역을 가져온다.
vec4 mixGradients(const float cloudType){

	const vec4 STRATUS_GRADIENT = vec4(0.02f, 0.05f, 0.09f, 0.11f);
	const vec4 STRATOCUMULUS_GRADIENT = vec4(0.02f, 0.2f, 0.48f, 0.625f);
	const vec4 CUMULUS_GRADIENT = vec4(0.01f, 0.0625f, 0.78f, 1.0f); 
	
	// these fractions would need to be altered if cumulonimbus are added to the same pass
	float stratus = 1.0f - clamp(cloudType * 2.0f, 0.0, 1.0);
	float stratocumulus = 1.0f - abs(cloudType - 0.5f) * 2.0f;
	float cumulus = clamp(cloudType - 0.5f, 0.0, 1.0) * 2.0f;
	return STRATUS_GRADIENT * stratus + STRATOCUMULUS_GRADIENT * stratocumulus + CUMULUS_GRADIENT * cumulus;
}

// ** 구름의 유형에 따라 해당 높이에서의 구름밀도를 가져온다.
float DensityHeightGradient(const float heightFrac, const float cloudType) 
{
	vec4 cloudGradient = mixGradients(cloudType);
	return smoothstep(cloudGradient.x, cloudGradient.y, heightFrac) - smoothstep(cloudGradient.z, cloudGradient.w, heightFrac);
}

// **
float GetHeightFractionForPoint(float inPosition, float sky_b_radius, float sky_t_radius)
{ // get global fractional position in cloud zone
	float height_fraction = (inPosition -  sky_b_radius) / (sky_t_radius - sky_b_radius); 
	return clamp(height_fraction, 0.0, 1.0);
}

// ** 
float SampleCloudDensity(vec3 samplepoint, bool highQuality)
{
	float perlin_worley = texture3D(LowFrequency3DTexture, samplepoint).r;
	float worley1 = texture3D(LowFrequency3DTexture, samplepoint).g;
	float worley2 = texture3D(LowFrequency3DTexture, samplepoint).b;
	float worley3 = texture3D(LowFrequency3DTexture, samplepoint).a;

	float height_fraction = samplepoint.z;//GetHeightFractionForPoint((samplepoint.z), 0.0f, 1.0f);
	vec4 low_frequency_noises = vec4(perlin_worley, worley1, worley2, worley3);

    float low_freq_FBM = low_frequency_noises.g * 0.625 + 
                         low_frequency_noises.b * 0.250 +
                         low_frequency_noises.a * 0.125;

	 // Remap with worley noise
	float base_cloud = Remap(low_frequency_noises.r, -(1.0 - low_freq_FBM), 1.0, 0.0, 1.0);

	// high quality
	if(highQuality)
	{
		vec2 whisp = texture(curlNoise, samplepoint.xy).xy;
		vec2 p = samplepoint.xy + whisp * (1.0f, height_fraction);
		vec3 hn = texture(HighFrequency3DTexture, vec3(p.x, p.y, samplepoint.z), 0).xyz;

		float hfbm = hn.r*0.625f + hn.g*0.25f + hn.b*0.125f;
		hfbm = mix(hfbm, 1.0f-hfbm, clamp(height_fraction, 0.0f, 1.0f));
		base_cloud = Remap(base_cloud, hfbm*0.2f, 1.0f, 0.0f, 1.0f);
	}

	vec4 weatherMap =  texture(weathermap, samplepoint.xy);
	float g = DensityHeightGradient(height_fraction, weatherMap.b);
	base_cloud *= g;

	float cloud_coverage = weatherMap.r;
	float base_cloud_with_coverage = Remap(base_cloud, cloud_coverage, 1.0f, 0.0f, 1.0f);
	base_cloud_with_coverage *= cloud_coverage;

	base_cloud *= base_cloud_with_coverage;

	//float cloud_coverage = smoothstep(0.6, 1.3, weatherMap.r);

	//cloud_coverage = weatherMap.r;
	//base_cloud = Remap(base_cloud*g, 1.0-cloud_coverage, 1.0, 0.0, 1.0); 
	//base_cloud *= cloud_coverage;


	return clamp(base_cloud, 0.0, 1.0);
}


//-----------------------------------------------------------------------------
// Main
//-----------------------------------------------------------------------------
void main()
{
	vec3 top = cloudPosition + cloudSize;
	vec3 bottom = cloudPosition - cloudSize;
	
	// Ray march 
	vec3 ray_direction;
	ray_direction.xy = (2.0f * gl_FragCoord.xy / viewport_size) - 1.0f; // screen-space coordinate
	ray_direction.x *= aspect_ratio;									// 화면의 aspect를 적용한다.
	ray_direction.z = focal_length;										// view-space coordinate
	ray_direction = (vec4(ray_direction, 1) * view).xyz;				// p^T = r^T * V

	// ray intercsion
	float t_0, t_1;														// 교차점을 찾는다.
	Ray casting_ray = Ray(ray_origin, ray_direction);					
	AABB bounding_box = AABB(top, bottom);
	ray_box_intersection(casting_ray, bounding_box, t_0, t_1);
	vec3 ray_start_world = ray_origin + ray_direction * t_0;
	vec3 ray_stop_world = ray_origin + ray_direction * t_1;
	vec3 ray_start = (ray_start_world - bottom) / (top - bottom); // (0,0,0)--(1,1,1)
	vec3 ray_stop =  (ray_stop_world - bottom) / (top - bottom); // (0,0,0)--(1,1,1)

	float step_length = stepLength;
	vec3 ray = ray_stop - ray_start;
	float ray_length = length(ray);
    vec3 step_vector = step_length * ray / ray_length;

	vec3 samplePoint = ray_start;
	vec4 colour = vec4(0.0);
	
	float T = 1.0f;
	vec4 color = vec4(.0);
	//float absorption = 80;
	int num = 0;
	float maxIternation = 1.0f / step_length;

    while (ray_length > 0 && num < 1.76f*maxIternation) 
	{
		float density = SampleCloudDensity(samplePoint, highQuality);

		if(density>0.)
		{
			float tmp = density / 64.0f; // step_length를 조정함.
			//T *= 1.0f - tmp * absorption;
			T *= exp(-density);
			if(T <= 0.01f) break;
			color += vec4(1.)*45*tmp*T; // alpha값도 조정함
		}

        ray_length -= step_length;
        samplePoint += step_vector;
		num++;
    }
	
	if (color.a < 0.01f) 
	{
		depthColor = 1.0f;
		fragColor = 1.0f;
	}
	else
	{
		depthColor = fragPos.z / 500.0f;
		fragColor = color.a;
	}
}

