__kernel void blub(__write_only image2d_t outputImage,  const int frames)
{
	for(int i = 0; i < 256;i++)
	for(int j = 0; j < 256;j++)
	{
		int2 coords = (int2)(i,j);
		//int4 pixel = (int4)((i<<20)+(j<<12),0,0,255);

		float nanana = (1+sin(.1f*frames))*.5f;

		float4 pixel = (float4)(nanana,j/256.0f,i/256.0f*j/256.0f,1);
		write_imagef(outputImage, coords, pixel);
	}
}