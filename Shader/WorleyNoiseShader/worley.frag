#version 430

in vec3 pos;
out vec4 a_colour;

uniform int numCellsPerAxis;
uniform vec3 ray_origin;
uniform sampler3D volume;
uniform vec2 viewport_size;
uniform float focal_length;
uniform float aspect_ratio;
uniform float gamma;
uniform float step_length;
uniform float densityPower;
uniform float absorption;
uniform vec3 centerPosition;
uniform vec3 boundSize;
uniform bool inSide;

uniform mat4 model;
uniform mat4 proj;
uniform mat4 view;

const ivec3 offsets[] = 
{
	// center
	ivec3(0,0,0),
	// front face
	ivec3(0,0,1), ivec3(-1,1,1), ivec3(-1,0,1), ivec3(-1,-1,1), ivec3(0,1,1), 
	ivec3(0,-1,1), ivec3(1,1,1), ivec3(1,0,1), ivec3(1,-1,1),
	// back face
	ivec3(0,0,-1), ivec3(-1,1,-1), ivec3(-1,0,-1), ivec3(-1,-1,-1), ivec3(0,1,-1), 
	ivec3(0,-1,-1), ivec3(1,1,-1), ivec3(1,0,-1), ivec3(1,-1,-1),
	// ring around center
	ivec3(-1,1,0), ivec3(-1,0,0), ivec3(-1,-1,0), ivec3(0,1,0), 
	ivec3(0,-1,0), ivec3(1,1,0), ivec3(1,0,0), ivec3(1,-1,0),
};

layout(std430) buffer shader_data
{
	float point[];
};

struct Ray { vec3 origin; vec3 direction;};
struct AABB {vec3 top; 	vec3 bottom;};

// Slab method for ray-box intersection
void ray_box_intersection(Ray ray, AABB box, out float t_0, out float t_1)
{
	vec3 direction_inv = 1.0 / ray.direction;
	vec3 t_top = direction_inv * (box.top - ray.origin);
	vec3 t_bottom = direction_inv * (box.bottom - ray.origin);
	vec3 t_min = min(t_top, t_bottom);
	vec2 t = max(t_min.xx, t_min.yz);
	t_0 = max(0.0, max(t.x, t.y));
	vec3 t_max = max(t_top, t_bottom);
	t = min(t_max.xx, t_max.yz);
	t_1 = min(t.x, t.y);
}

float maxComponent(vec3 vec) {	return max(vec.x, max(vec.y, vec.z)); }
float minComponent(vec3 vec) {	return min(vec.x, min(vec.y, vec.z)); }

float worley(vec3 samplePos, int numCells)
{
	vec3 cellVec = samplePos * numCells;
	ivec3 cellID = ivec3(floor(cellVec.x), floor(cellVec.y), floor(cellVec.z));
	float minSqrDst = 1.0f;

	for (int cellOffsetIndex=0; cellOffsetIndex<27; cellOffsetIndex++)
	{
		ivec3 adjID = cellID + offsets[cellOffsetIndex];
		if (minComponent(adjID) == -1 || maxComponent(adjID) == numCells)
		{
			ivec3 wrappedID = ivec3((adjID.x + numCells)% numCells, 
									(adjID.y + numCells)% numCells, 
									(adjID.z + numCells)% numCells);
			int adjCellIndex = 3 * (wrappedID.x + numCells * (wrappedID.y + numCells * wrappedID.z));
			vec3 wrappedPoint = vec3(point[adjCellIndex],point[adjCellIndex+1],point[adjCellIndex+2]);
			for (int wrapOffsetIndex=0; wrapOffsetIndex<27; wrapOffsetIndex++)
			{
				vec3 sampleOffset = (samplePos - (wrappedPoint + offsets[wrapOffsetIndex]));
				minSqrDst = min(minSqrDst, dot(sampleOffset, sampleOffset));
			}
		}
		else
		{
			int adjCellIndex = 3 * (adjID.x + numCells * (adjID.y + numCells * adjID.z));
			vec3 sampleOffset = samplePos - vec3(point[adjCellIndex],point[adjCellIndex+1],point[adjCellIndex+2]);
			minSqrDst = min(minSqrDst, dot(sampleOffset, sampleOffset));
		}
	}

	return minSqrDst;
}

// A very simple colour transfer function
vec4 colour_transfer(float intensity)
{
    vec3 high = vec3(1.0, 1.0, 0.0);
    vec3 low = vec3(0.0, 0.0, 0.0);
    float alpha = (exp(intensity) - 1.0) / (exp(1.0) - 1.0);
    return vec4(intensity * high + (1.0 - intensity) * low, alpha);
}

void main()
{
	// 셀블럭ID를 찾는다.
	//vec3 position = pos * 0.499999f + vec3(0.5f);
	//vec3 samplePos = position * numCellsPerAxis;
	//ivec3 cellID = ivec3(floor(samplePos.x), floor(samplePos.y), floor(samplePos.z));
	//int idx = 3*(cellID.x + numCellsPerAxis * (cellID.y + numCellsPerAxis * cellID.z));
	//vec3 cellSeed = vec3(point[idx],point[idx+1],point[idx+2]);
	//a_colour = vec4(point[idx],point[idx+1],point[idx+2], 1.0f);


	vec3 top = centerPosition + boundSize;
	vec3 bottom= centerPosition - boundSize;
	vec3 background_colour = vec3(0.8f,0.8f,0.1f);

	vec3 ray_direction;
	ray_direction.xy = (2.0f * gl_FragCoord.xy / viewport_size) - 1.0f; // screen-space coordinate
	ray_direction.x *= aspect_ratio;
	ray_direction.z = focal_length; // view-space coordinate
	ray_direction = (vec4(ray_direction, 1) * view).xyz; // p^T = r^T * V

	float t_0, t_1;
	Ray casting_ray = Ray(ray_origin, ray_direction);
	AABB bounding_box = AABB(top, bottom);
	ray_box_intersection(casting_ray, bounding_box, t_0, t_1);

	// range (0,0,0)-(1,1,1)
	vec3 ray_start = (ray_origin + ray_direction * t_0 - bottom) / (top - bottom);
	vec3 ray_stop = (ray_origin + ray_direction * t_1 - bottom) / (top - bottom);

	vec3 ray = ray_stop - ray_start;
	float ray_length = length(ray);
    vec3 step_vector = step_length * ray / ray_length;

	vec3 position = ray_start;
    vec4 colour = vec4(0.0);

	float fcolor = 0.0f;

	//float dist = ray_length;
	//float result = exp(-dist * absorption); // beer's law
	//a_colour = vec4(0.43f, 0.73f, 0.57f, result);

    // Ray march until reaching the end of the volume, or colour saturation
    while (ray_length > 0 && colour.a < 1.0) 
	{
		float intensity = worley(position, numCellsPerAxis);
		fcolor += intensity;

        ray_length -= step_length;
        position += step_vector;
    }
	
	vec4 f = colour_transfer(fcolor);
	a_colour = vec4(f.xyz, 1-abs(f.a));
	//a_colour = vec4(1, 1, 1, 1-fcolor);
	

	//vec3 position = pos * 0.4999f + vec3(0.5f);
	//float dist = worley(position, numCellsPerAxis);
	//float d = 1-(abs(sqrt(dist)));
	//d = pow(d, 12);
	//a_colour = vec4(1, 1, 1, 1-d);
}

