#define EPSILON 1e-5f

float3 mat_vec_mul(float16 mat, float4 vec)
{
	return (float3)(dot(mat.s0123, vec),dot(mat.s4567, vec),dot(mat.s89AB, vec));
}

void calcRay(const int x, const int y, const int width, const int height, const float16 transformMatrix, float3* ray_origin, float3* ray_direction)
{
	*ray_origin = mat_vec_mul(transformMatrix, (float4)(0,0,0,1));
	*ray_direction = mat_vec_mul(transformMatrix, (float4)(  ((float)x*2-width)/height  ,(float)y/height*2-1,1,1));

	float3 diff = *ray_direction - *ray_origin;
	*ray_direction = diff * native_rsqrt(dot(diff,diff));
}

bool PointInCube(float3 point, float4 cube)
{
	return 
	(
		cube.x <= point.x + EPSILON && point.x <= cube.x + cube.w + EPSILON &&
        cube.y <= point.y + EPSILON && point.y <= cube.y + cube.w + EPSILON &&
        cube.z <= point.z + EPSILON && point.z <= cube.z + cube.w + EPSILON
	);
}

float CubeRayIntersectPointParameter(float3 ray_origin, float3 ray_direction, float4 cube, bool entryPoint)
{
	float parameterCanidates[6];
	
	/*parameterCanidates[0] = (cube.x          - ray_origin.x) / ray_direction.x;
	parameterCanidates[1] = (cube.x + cube.w - ray_origin.x) / ray_direction.x;

	parameterCanidates[2] = (cube.y          - ray_origin.y) / ray_direction.y;
	parameterCanidates[3] = (cube.y + cube.w - ray_origin.y) / ray_direction.y;

	parameterCanidates[4] = (cube.z          - ray_origin.z) / ray_direction.z;
	parameterCanidates[5] = (cube.z + cube.w - ray_origin.z) / ray_direction.z;*/




	parameterCanidates[0] = cube.x / ray_direction.x - ray_origin.x / ray_direction.x;
	parameterCanidates[1] = cube.x / ray_direction.x + cube.w / ray_direction.x - ray_origin.x / ray_direction.x;

	parameterCanidates[2] = cube.y / ray_direction.y - ray_origin.y / ray_direction.y;
	parameterCanidates[3] = cube.y / ray_direction.y + cube.w / ray_direction.y - ray_origin.y / ray_direction.y;

	parameterCanidates[4] = cube.z / ray_direction.z - ray_origin.z / ray_direction.z;
	parameterCanidates[5] = cube.z / ray_direction.z + cube.w / ray_direction.z - ray_origin.z / ray_direction.z;

	float new_parameter = -1; // -1 == undefined

	for(int i = 0; i < 6; i++) if(parameterCanidates[i] > 0 && PointInCube(ray_origin + parameterCanidates[i] * ray_direction, cube)
			&& (new_parameter == -1 || ((new_parameter < parameterCanidates[i]) != entryPoint))) 
			new_parameter = parameterCanidates[i];

	return new_parameter;
}

void makeSubCubes(float4* cubes, float4 cube)
{
	float h = cube.w / 2;
	cubes[0] = (float4)(cube.x, cube.y, cube.z, h);
	cubes[1] = (float4)(cube.x, cube.y, cube.z+h, h);
	cubes[2] = (float4)(cube.x, cube.y+h, cube.z, h);
	cubes[3] = (float4)(cube.x, cube.y+h, cube.z+h, h);
	
	cubes[4] = (float4)(cube.x+h, cube.y, cube.z, h);
	cubes[5] = (float4)(cube.x+h, cube.y, cube.z+h, h);
	cubes[6] = (float4)(cube.x+h, cube.y+h, cube.z, h);
	cubes[7] = (float4)(cube.x+h, cube.y+h, cube.z+h, h);
}

int readIntAt(__global uchar* octree, int offset)
{
	return
	(((int)octree[offset+0]) << 0) |
	(((int)octree[offset+1]) << 8) |
	(((int)octree[offset+2]) << 16) |
	(((int)octree[offset+3]) << 24);
}


__kernel void renderPixel(__write_only image2d_t outputImage, __global uchar* octree, const int width, const int height, const float16 transformMatrix)
{
	size_t x = get_global_id(0);
	size_t y = get_global_id(1);

	if(x < width && y < height)
	{
		float3 ray_origin, ray_direction;
		float ray_parameter;

		calcRay(x,y,width,height,transformMatrix, &ray_origin, &ray_direction);

		if(PointInCube(ray_origin, (float4)(0,0,0,1)))
			ray_parameter = 0;
		else
			ray_parameter = CubeRayIntersectPointParameter(ray_origin, ray_direction, (float4)(0,0,0,1), true);

		float4 pixel = (float4)(ray_direction+.5f,1);

	

		if(ray_parameter >= 0)
		{
			pixel.xyz=0;
			const char ID_LEAF = 0x42;
			const char ID_BRANCH = 0x43;

			bool stopAdvanceRay = false;


			// advance ray loop
			for(int rayCastStep = 0; rayCastStep < 120 && !stopAdvanceRay; rayCastStep++)
			{
				int current_octree_node = 0;
				float4 current_cube = (float4)(0, 0, 0, 1);
				float3 ray_tip = ray_origin + ray_direction * ray_parameter;

				if(!PointInCube(ray_tip, current_cube)) { pixel.x = 1; break; }
				
				bool stopTraverseOctree = false;

				// traverse octree loop
				for(int depth = 0; depth < 10 && !stopTraverseOctree; depth++)
				{
					if(octree[current_octree_node] == ID_LEAF)
					{
						stopAdvanceRay = true;
						pixel.xyz = 1;
						break;
					}
					else if(octree[current_octree_node] == ID_BRANCH)
					{
						float4 subcubes[8];
						makeSubCubes(subcubes, current_cube);
						for(int i = 0; i < 8; i++)
						{
							if(PointInCube(ray_tip, subcubes[i]))
							{
								int subcube_node = readIntAt(octree, current_octree_node + 1 + 4*i);

								if(subcube_node <= 0)
								{
									float increment = subcubes[i].w/2;
									
									while(increment > EPSILON)
									{
										while(PointInCube(ray_origin + ray_direction * (ray_parameter+increment), subcubes[i]))
											ray_parameter += increment;

										increment /= 2;
									}
									ray_parameter += 2*increment;

									/*float new_ray_parameter = 3 * EPSILON + CubeRayIntersectPointParameter(ray_origin, ray_direction, subcubes[i], false);
									if(new_ray_parameter > ray_parameter)
									{
										ray_parameter = new_ray_parameter;
									}
									else
									{
										stopAdvanceRay = true;
										if(new_ray_parameter == -1)
										pixel.x=1;
										pixel.y=.5f;
									}*/
									//ray_parameter += subcubes[i].w*0.3f;
									stopTraverseOctree = true;
								}
								else
								{
									current_octree_node = subcube_node;
									current_cube = subcubes[i];
								}
								break;
							}
						}
					}
					else 
					{
						stopAdvanceRay = true;
						pixel.y = 1;
						break;
					}
				}
			}
		}
		

		write_imagef(outputImage, (int2)(x,y), pixel);
	}
}