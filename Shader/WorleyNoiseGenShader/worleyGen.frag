#version 430

in vec3 pos;
out vec4 a_colour;

uniform int numCellsPerAxis;

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

// samplePos은 0과 1사이의 값이다.
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

void main()
{	
	vec3 position = pos * 0.4999f + vec3(0.5f);
	float intensity = worley(position, numCellsPerAxis);
	float d = (1-(abs(sqrt(intensity))));
	d = pow(d, 56);
	a_colour = vec4(1, 1, 1, 1-d);
}

