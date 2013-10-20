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


void cl_perr(char* context, cl_int err)
{
	const char* msg = clErrorString(err);
	if(msg != NULL) cout << context << ": " << clErrorString(err) << endl;
}

void display()
{
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

	const int textureSize = 512;

	char data[textureSize*textureSize*3];
	for (int x = 0; x < textureSize; x++)
	{
		for (int y = 0; y < textureSize; y++)
		{
			for (int c = 0; c < 3; c++)
			{
				data[(x*textureSize+y)*3+c] = (x+y+50*c)%256;
			}
		}
	}

	glTexParameterf(GL_TEXTURE_2D,  GL_TEXTURE_MAG_FILTER, GL_NEAREST);
    glTexParameterf(GL_TEXTURE_2D,  GL_TEXTURE_MIN_FILTER, GL_NEAREST);
	glTexImage2D(GL_TEXTURE_2D, 0,GL_RGB, textureSize, textureSize, 0, GL_RGB,  GL_UNSIGNED_BYTE, data);
	
	cl_int cl_err;
	cl_platform_id cl_platform;
	cl_device_id cl_device;
	cl_context cl_context;
	cl_command_queue cl_command_queue;
	cl_program cl_program;
	cl_mem cl_buffer;
	cl_kernel cl_kernel;

	
	cl_err = clGetPlatformIDs(1, &cl_platform, NULL);
	cl_perr("clGetPlatformIDs", cl_err);


	cl_err = clGetDeviceIDs(cl_platform, CL_DEVICE_TYPE_GPU, 1, &cl_device,	NULL);
	
	cl_context_properties properties[] = { 
	CL_GL_CONTEXT_KHR, (cl_context_properties)wglGetCurrentContext(), // WGL Context 
	CL_WGL_HDC_KHR, (cl_context_properties)wglGetCurrentDC(),  // WGL HDC
	CL_CONTEXT_PLATFORM, (cl_context_properties)cl_platform,  // OpenCL platform
	0};

	cl_device_id devices[32];
	size_t sharing_devices_count;


	//void* func_ptr = clGetExtensionFunctionAddress("clGetGLContextInfoKHR");
	clGetGLContextInfoKHR_fn *functionPtr = (clGetGLContextInfoKHR_fn*)clGetExtensionFunctionAddress("clGetGLContextInfoKHR");
	
	
	cl_err = (*functionPtr)(properties, CL_CURRENT_DEVICE_FOR_GL_CONTEXT_KHR,32*sizeof(cl_device_id), devices, &sharing_devices_count);
	cl_perr("clGetDeviceIDs", cl_err);


	char message[20000];
	cl_err =  clGetDeviceInfo(cl_device, CL_DEVICE_EXTENSIONS, 20000, message, NULL);	
	cl_perr("clGetDeviceInfo", cl_err);
	cout << "Supported Extensions: " << message << endl;

	char devName[255];
	cl_err = clGetDeviceInfo(cl_device, CL_DEVICE_NAME, 254, devName, NULL);
	cl_perr("clGetDeviceInfo", cl_err);
	cout << "Device Name: " << devName << endl;


	cl_context = clCreateContext (NULL, 1, &cl_device, NULL, NULL, &cl_err);
	cl_perr("clCreateContext", cl_err);
	cl_command_queue = clCreateCommandQueue (cl_context, cl_device, 0, &cl_err);
	cl_perr("clCreateCommandQueue", cl_err);


	ifstream kernel_file("../AcceleratedVoxelRendering/kernel.cl");
	string line;
	stringstream kernel_file_sstream;
	while (getline(kernel_file, line)) kernel_file_sstream << line << endl;

	string kernel_file_string = kernel_file_sstream.str();
	char* kernel_file_c_str = (char*)(kernel_file_string.c_str());
	size_t kernel_file_c_str_size = strlen(kernel_file_c_str);

		
	cl_program = clCreateProgramWithSource(cl_context, 1, (const char**)&kernel_file_c_str, &kernel_file_c_str_size, &cl_err);
	cl_perr("clCreateProgramWithSource", cl_err);

	cl_err = clBuildProgram(cl_program, 1, &cl_device, NULL, NULL, NULL);
	cl_perr("clBuildProgram", cl_err);

	if(cl_err != CL_SUCCESS)
	{
		char message_buffer[1024*16];
		clGetProgramBuildInfo (cl_program, cl_device, CL_PROGRAM_BUILD_LOG, 1024*16-1, message_buffer, NULL);
		cout << message_buffer << endl;
	}
	
	cl_kernel = clCreateKernel (cl_program, "blub", &cl_err);
	cl_perr("clCreateKernel", cl_err);


	const int size = 100;
	float* cl_buffer_data = new float[size];
	for (int i = 0; i < size; i++) cl_buffer_data[i]=i;

	//cl_buffer = clCreateBuffer(cl_context, CL_MEM_READ_WRITE | CL_MEM_COPY_HOST_PTR, size*sizeof(float), cl_buffer_data, &cl_err);

	//cl_buffer = clCreateFromGLTexture2D(cl_context, CL_MEM_READ_WRITE, GL_TEXTURE_2D, 0, textureID, &cl_err);
	cl_buffer=  clCreateFromGLBuffer  (cl_context, CL_MEM_READ_WRITE, textureID, &cl_err);
	cl_perr("clCreateBuffer", cl_err);

	int num = 300;
	cl_err = clSetKernelArg(cl_kernel, 0, sizeof(cl_mem), &cl_buffer);
	cl_err |= clSetKernelArg(cl_kernel, 1, sizeof(size_t), &num);
	cl_perr("clSetKernelArg", cl_err);

	size_t global_work_size = 1;
	size_t local_work_size = 1;

	cl_err = clEnqueueNDRangeKernel (cl_command_queue, cl_kernel, 1, NULL, &global_work_size,  &local_work_size, 0, NULL, NULL);
	cl_perr("clEnqueueNDRangeKernel", cl_err);

	/*cl_err = clEnqueueReadBuffer(cl_command_queue , cl_buffer, CL_TRUE, 0, size*sizeof(float), cl_buffer_data, 0, NULL, NULL);
	cl_perr("clEnqueueReadBuffer", cl_err);

	for (int i = 0; i < size; i++) cout << cl_buffer_data[i] << " ";*/

	glutMainLoop();
}