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

	parameterCanidates[0] = (cube.x          - ray_origin.x) / ray_direction.x;
	parameterCanidates[1] = (cube.x + cube.w - ray_origin.x) / ray_direction.x;

	parameterCanidates[2] = (cube.y          - ray_origin.y) / ray_direction.y;
	parameterCanidates[3] = (cube.y + cube.w - ray_origin.y) / ray_direction.y;

	parameterCanidates[4] = (cube.z          - ray_origin.z) / ray_direction.z;
	parameterCanidates[5] = (cube.z + cube.w - ray_origin.z) / ray_direction.z;

	float new_parameter = -1; // -1 == undefined

	for(int i = 0; i < 6; i++)
	{
		if(
			PointInCube(ray_origin + parameterCanidates[i] * ray_direction, cube)
			&& (new_parameter == -1 || ((new_parameter < parameterCanidates[i]) != entryPoint))
		)
		{
			new_parameter = parameterCanidates[i];
		}
	}

	return new_parameter;
}

__kernel void renderPixel(__write_only image2d_t outputImage, const int width, const int height, const float16 transformMatrix)
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

		float4 pixel = (float4)(ray_direction,1);
		/*if(-.1f < ray_direction.x && ray_direction.x < .1f
		 && -.1f < ray_direction.y && ray_direction.y < .1f)
		{
			pixel.x=1;
		}*/
		if(ray_parameter >= 0)
		{
			pixel.xyz=1;
		}

		write_imagef(outputImage, (int2)(x,y), pixel);
	}
}