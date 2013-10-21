__kernel void blub(__write_only image2d_t outputImage,  const int frames,  const int width,  const int height)
{
	size_t x = get_global_id(0);
	size_t y = get_global_id(1);

	if(x < 1920 && y < 1080)
	{
		int2 coords = (int2)(x,y);
		float nanana = (1+sin(.01f*frames))*.5f;

		float4 pixel = (float4)(nanana,(float)x/width,(float)y/height*(float)x/width,1);
		write_imagef(outputImage, coords, pixel);
	}
}