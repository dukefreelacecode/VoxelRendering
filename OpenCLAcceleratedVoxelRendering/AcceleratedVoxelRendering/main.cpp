#include "stdafx.h"
#include <windows.h>
#include <GL/gl.h>
#include <GL/GLU.h>
#include <gl\glut.h>
#include <iostream>
#include <sstream>
#include "math.h"
#include "octree_node.h"
#include <CL/cl.h>
#include <CL/cl_gl.h>
#include <CL/cl_gl_ext.h>
#include <string>
#include <fstream>
#include <vector>
#include "clErrorString.h"

#pragma OPENCL EXTENSION CL_KHR_gl_sharing : enable
#pragma OPENCL EXTENSION CL_KHR_gl_sharing : require

using namespace std;

const unsigned int window_width = 512, window_height = 512;
const int size = window_width * window_height;

int frameCount = 0;
int windowID;

GLuint textureID;


cl_int cl_err;
cl_platform_id my_cl_platform;
cl_device_id my_cl_device;
cl_context my_cl_context;
cl_command_queue my_cl_command_queue;
cl_program my_cl_program;
cl_mem my_cl_buffer;
cl_kernel my_cl_kernel;

void cl_perr(char* context, cl_int err)
{
	const char* msg = clErrorString(err);
	if(msg != NULL) cout << context << ": " << clErrorString(err) << endl;
}

void display()
{
	
	cl_err = clSetKernelArg(my_cl_kernel, 0, sizeof(cl_mem), &my_cl_buffer);
	cl_err |= clSetKernelArg(my_cl_kernel, 1, sizeof(int), &frameCount);
	cl_perr("clSetKernelArg", cl_err);

	size_t global_work_size = 1;
	size_t local_work_size = 1;

	cl_err = clEnqueueNDRangeKernel (my_cl_command_queue, my_cl_kernel, 1, NULL, &global_work_size,  &local_work_size, 0, NULL, NULL);
	cl_perr("clEnqueueNDRangeKernel", cl_err);
	clFinish(my_cl_command_queue);

    glBegin(GL_QUADS);
    glTexCoord2f(0, 1);
    glVertex3f(-1, 1, 0.0f);
    glTexCoord2f(1, 1);
    glVertex3f( 1, 1, 0.0f);
    glTexCoord2f(1, 0);
    glVertex3f( 1,-1, 0.0f);
    glTexCoord2f(0, 0);
    glVertex3f(-1, -1, 0.0f);
    glEnd();
    glFinish();

	glutSwapBuffers();
	Sleep(20);
	glutPostRedisplay();
	frameCount++;
}

void keyboard(unsigned char key, int x, int y)
{
	if(key == 27)
	{
		glutDestroyWindow(windowID);
		exit(EXIT_SUCCESS);
	}
}

int main(int argc, char** argv) 
{
	glutInit(&argc, argv);

	glutInitDisplayMode(GLUT_RGB | GLUT_DOUBLE | GLUT_DEPTH);
	glutInitWindowSize(window_width, window_height);
	glutInitWindowPosition(200,50);
	windowID = glutCreateWindow("OpenGL glDrawPixels demo");

	glutDisplayFunc(display);
	//glutReshapeFunc(reshape);
	//glutMouseFunc(mouse_button);
	//glutMotionFunc(mouse_motion);
	glutKeyboardFunc(keyboard);
	//glutIdleFunc(idle);
  
	//glEnable(GL_DEPTH_TEST);
	glClearColor(0.0, 0.0, 0.0, 1.0);
	//glPointSize(2);



	glEnable(GL_TEXTURE_2D);
	glGenTextures(1, &textureID); 	
	glBindTexture(GL_TEXTURE_2D, textureID);

	const int textureSize = 256;

	char data[textureSize*textureSize*4];
	for (int x = 0; x < textureSize; x++)
	{
		for (int y = 0; y < textureSize; y++)
		{
			for (int c = 0; c < 4; c++)
			{
				data[(x*textureSize+y)*4+c] = (x+y+50*c)%256;
			}
		}
	}

	glTexParameterf(GL_TEXTURE_2D,  GL_TEXTURE_MAG_FILTER, GL_NEAREST);
    glTexParameterf(GL_TEXTURE_2D,  GL_TEXTURE_MIN_FILTER, GL_NEAREST);
	glTexImage2D(GL_TEXTURE_2D, 0,GL_RGBA, textureSize, textureSize, 0, GL_RGBA, GL_UNSIGNED_BYTE, data);
	
	

	// ==== OpenCL Platform + Device ====

	cl_err = clGetPlatformIDs(1, &my_cl_platform, NULL);
	cl_perr("clGetPlatformIDs", cl_err);


	cl_err = clGetDeviceIDs(my_cl_platform, CL_DEVICE_TYPE_GPU, 1, &my_cl_device,	NULL);

	char message[20000];
	cl_err =  clGetDeviceInfo(my_cl_device, CL_DEVICE_EXTENSIONS, 20000, message, NULL);	
	cl_perr("clGetDeviceInfo", cl_err);
	cout << "Supported Extensions: " << message << endl;

	char devName[255];
	cl_err = clGetDeviceInfo(my_cl_device, CL_DEVICE_NAME, 254, devName, NULL);
	cl_perr("clGetDeviceInfo", cl_err);
	cout << "Device Name: " << devName << endl;


	// ===== OpenCL Context =========

	cl_context_properties properties[] = 
	{ 
		CL_GL_CONTEXT_KHR, (cl_context_properties)wglGetCurrentContext(), // WGL Context 
		CL_WGL_HDC_KHR, (cl_context_properties)wglGetCurrentDC(),  // WGL HDC
		CL_CONTEXT_PLATFORM, (cl_context_properties)my_cl_platform,  // OpenCL platform
	0};

	my_cl_context = clCreateContext (properties, 1, &my_cl_device, NULL, NULL, &cl_err);
	cl_perr("clCreateContext", cl_err);
	my_cl_command_queue = clCreateCommandQueue (my_cl_context, my_cl_device, 0, &cl_err);
	cl_perr("clCreateCommandQueue", cl_err);


	ifstream kernel_file("../AcceleratedVoxelRendering/kernel.cl");
	string line;
	stringstream kernel_file_sstream;
	while (getline(kernel_file, line)) kernel_file_sstream << line << endl;

	string kernel_file_string = kernel_file_sstream.str();
	char* kernel_file_c_str = (char*)(kernel_file_string.c_str());
	size_t kernel_file_c_str_size = strlen(kernel_file_c_str);

		
	my_cl_program = clCreateProgramWithSource(my_cl_context, 1, (const char**)&kernel_file_c_str, &kernel_file_c_str_size, &cl_err);
	cl_perr("clCreateProgramWithSource", cl_err);

	cl_err = clBuildProgram(my_cl_program, 1, &my_cl_device, NULL, NULL, NULL);
	cl_perr("clBuildProgram", cl_err);

	if(cl_err != CL_SUCCESS)
	{
		char message_buffer[1024*16];
		clGetProgramBuildInfo (my_cl_program, my_cl_device, CL_PROGRAM_BUILD_LOG, 1024*16-1, message_buffer, NULL);
		cout << message_buffer << endl;
	}
	
	my_cl_kernel = clCreateKernel (my_cl_program, "blub", &cl_err);
	cl_perr("clCreateKernel", cl_err);


	const int size = 100;
	float* cl_buffer_data = new float[size];
	for (int i = 0; i < size; i++) cl_buffer_data[i]=i;

	my_cl_buffer = clCreateFromGLTexture2D(my_cl_context, CL_MEM_WRITE_ONLY, GL_TEXTURE_2D, 0, textureID, &cl_err);

	cl_perr("clCreateBuffer", cl_err);


	/*cl_err = clEnqueueReadBuffer(cl_command_queue , cl_buffer, CL_TRUE, 0, size*sizeof(float), cl_buffer_data, 0, NULL, NULL);
	cl_perr("clEnqueueReadBuffer", cl_err);

	for (int i = 0; i < size; i++) cout << cl_buffer_data[i] << " ";*/

	glutMainLoop();
}