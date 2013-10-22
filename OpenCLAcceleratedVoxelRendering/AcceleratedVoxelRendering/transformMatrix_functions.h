#ifndef transformMatrix_functions_h_
#define transformMatrix_functions_h_

#include <math.h>
#include <CL/cl.h>


cl_float16 transformMatrix_multiply(cl_float16 left, cl_float16 right);
cl_float16 transformMatrix_identity();
cl_float16 transformMatrix_translate(cl_float x,cl_float y,cl_float z);
cl_float16 transformMatrix_scale(cl_float x,cl_float y,cl_float z);
cl_float16 transformMatrix_rotX(float radians);
cl_float16 transformMatrix_rotY(float radians);
cl_float16 transformMatrix_rotZ(float radians);

#endif