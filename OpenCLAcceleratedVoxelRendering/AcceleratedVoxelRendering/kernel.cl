float3 mat_vec_mul(float16 mat, float4 vec)
{
	return (float3)(dot(mat.s0123, vec),dot(mat.s4567, vec),dot(mat.s89AB, vec));
}

void calcRay(const int x, const int y, const int width, const int height, const float16 transformMatrix, float3* ray_origin, float3* ray_direction)
{
	*ray_origin = mat_vec_mul(transformMatrix, (float4)(0,0,0,1));
	*ray_direction = mat_vec_mul(transformMatrix, (float4)((float)x/width*2-1,(float)y/height*2-1,1,1));

	float3 diff = *ray_direction - *ray_origin;
	*ray_direction = diff * native_rsqrt(dot(diff,diff));
}

__kernel void renderPixel(__write_only image2d_t outputImage, const int width, const int height, const float16 transformMatrix)
{
	size_t x = get_global_id(0);
	size_t y = get_global_id(1);

	if(x < width && y < height)
	{
		float3 ray_origin, ray_direction;
		calcRay(x,y,width,height,transformMatrix, &ray_origin, &ray_direction);

		float4 pixel = (float4)(ray_direction,1);
		if(-.1f < ray_direction.x && ray_direction.x < .1f
		 && -.1f < ray_direction.y && ray_direction.y < .1f)
		{
			pixel.x=1;
		}
		

		write_imagef(outputImage, (int2)(x,y), pixel);
	}
}