#include "transformMatrix_functions.h"

cl_float16 transformMatrix_multiply(cl_float16 left, cl_float16 right)
{
	cl_float16 result;

	for (int i = 0; i < 4; i++)
	{
		for (int j = 0; j < 4; j++)
		{
			result.s[4*i+j] = 0;

			for (int k = 0; k < 4; k++)
			{
				result.s[4*i+j] += left.s[4*i+k] * right.s[4*k+j];
			}
		}
	}

	return result;
}

cl_float16 transformMatrix_identity()
{
	cl_float16 m = {
		1, 0, 0, 0,
		0, 1, 0, 0, 
		0, 0, 1, 0,
		0, 0, 0, 1,
	};
	return m;
}

cl_float16 transformMatrix_translate(cl_float x,cl_float y,cl_float z)
{
	cl_float16 m = {
		1, 0, 0, x,
		0, 1, 0, y, 
		0, 0, 1, z,
		0, 0, 0, 1,
	};
	return m;
}

cl_float16 transformMatrix_scale(cl_float x,cl_float y,cl_float z)
{
	cl_float16 m = {
		x, 0, 0, 0,
		0, y, 0, 0, 
		0, 0, z, 0,
		0, 0, 0, 1,
	};
	return m;
}

cl_float16 transformMatrix_rotX(float radians)
{
	float s = sinf(radians);
	float c = cosf(radians);

	cl_float16 m = {
		1, 0, 0, 0,
		0, c,-s, 0, 
		0, s, c, 0,
		0, 0, 0, 1,
	};
	return m;
}


cl_float16 transformMatrix_rotY(float radians)
{
	float s = sinf(radians);
	float c = cosf(radians);

	cl_float16 m = {
		c, 0, s, 0,
		0, 1, 0, 0, 
	   -s, 0, c, 0,
		0, 0, 0, 1,
	};
	return m;
}


cl_float16 transformMatrix_rotZ(float radians)
{
	float s = sinf(radians);
	float c = cosf(radians);

	cl_float16 m = {
		c,-s, 0, 0,
		s, c, 0, 0, 
		0, 0, 1, 0,
		0, 0, 0, 1,
	};
	return m;
}
