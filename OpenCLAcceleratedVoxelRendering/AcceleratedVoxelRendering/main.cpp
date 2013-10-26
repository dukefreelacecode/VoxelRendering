#include "stdafx.h"
#include <windows.h>
#include <GL/gl.h>
#include <GL/GLU.h>
#include <gl\glut.h>
#include <iostream>
#include <CL/cl.h>
#include <CL/cl_gl.h>
#include <time.h>
#include "clErrorString.h"
#include "transformMatrix_functions.h"

#ifndef M_PI
#define M_PI 3.14159265f
#endif

#pragma OPENCL EXTENSION CL_KHR_gl_sharing : enable
#pragma OPENCL EXTENSION CL_KHR_gl_sharing : require

using namespace std;


int frameCount = 0;
int windowID;

GLuint textureID;

int screen_width,screen_height;

time_t startTime;

cl_int cl_err;
cl_platform_id my_cl_platform;
cl_device_id my_cl_device;
cl_context my_cl_context;
cl_command_queue my_cl_command_queue;
cl_program my_cl_program;
cl_mem my_cl_rendertexture_buffer;
cl_mem my_cl_octree_buffer;
cl_kernel my_cl_kernel;

cl_event AcquireTextureDone;
cl_event RenderTextureDone;
cl_event ReleaseTextureDone;


float pitch;
float yaw;
cl_float3 camPosition;
cl_float16 transformMatrix;

void rebuild_transformMatrix()
{
	transformMatrix = transformMatrix_identity();
	transformMatrix = transformMatrix_multiply(transformMatrix, transformMatrix_translate(camPosition.s[0], camPosition.s[1], camPosition.s[2]));
	transformMatrix = transformMatrix_multiply(transformMatrix, transformMatrix_rotY(yaw));
	transformMatrix = transformMatrix_multiply(transformMatrix, transformMatrix_rotX(pitch));	
}

void cl_perr(char* context, cl_int err)
{
	const char* msg = clErrorString(err);
	if(msg != NULL) cout << context << ": " << clErrorString(err) << endl;
}

void ManageMouseInput()
{
	POINT p;
	if (GetCursorPos(&p))
	{
		yaw += (p.x-screen_width/2)*0.005f;
		pitch += (p.y-screen_height/2)*0.005f;
		SetCursorPos(screen_width/2, screen_height/2);
		rebuild_transformMatrix();
		if(pitch > M_PI/2) pitch = M_PI/2;
		if(pitch < -M_PI/2) pitch = -M_PI/2;
	}
}

void ManageKeyboardInput()
{
	if(GetKeyState(VK_ESCAPE)<0) // escape
	{
		glutDestroyWindow(windowID);
		exit(EXIT_SUCCESS);
	}

	float _cos = cos(yaw);
	float _sin = sin(yaw);
	const float moveSpeed = 0.01f;

	if(GetKeyState('W')<0) { camPosition.s[0] += moveSpeed * _sin ; camPosition.s[2] += moveSpeed * _cos; }
	if(GetKeyState('S')<0) { camPosition.s[0] += moveSpeed * -_sin ; camPosition.s[2] += moveSpeed * -_cos; }
	if(GetKeyState('A')<0) { camPosition.s[0] += moveSpeed * -_cos ; camPosition.s[2] += moveSpeed * _sin; }
	if(GetKeyState('D')<0) { camPosition.s[0] += moveSpeed * _cos ; camPosition.s[2] += moveSpeed * -_sin; }
	if(GetKeyState('E')<0) { camPosition.s[1] += moveSpeed; }
	if(GetKeyState('Q')<0) { camPosition.s[1] -= moveSpeed; }

	rebuild_transformMatrix();
}

void display()
{
	ManageMouseInput();
	ManageKeyboardInput();

	cl_err = clEnqueueAcquireGLObjects(my_cl_command_queue, 1, &my_cl_rendertexture_buffer, NULL, NULL, &AcquireTextureDone);
	cl_perr("clEnqueueAcquireGLObjects", cl_err);

	
	cl_err  = clSetKernelArg(my_cl_kernel, 0, sizeof(cl_mem), &my_cl_rendertexture_buffer);
	cl_err |= clSetKernelArg(my_cl_kernel, 1, sizeof(cl_mem), &my_cl_octree_buffer);
	cl_err |= clSetKernelArg(my_cl_kernel, 2, sizeof(int), &screen_width);
	cl_err |= clSetKernelArg(my_cl_kernel, 3, sizeof(int), &screen_height);
	cl_err |= clSetKernelArg(my_cl_kernel, 4, sizeof(cl_float16), &transformMatrix);
	cl_perr("clSetKernelArg", cl_err);

	
	size_t local_work_size[] = {16, 16};
	size_t global_work_size[] = { (screen_width/local_work_size[0]+1)*local_work_size[0], (screen_height/local_work_size[1]+1)*local_work_size[1] };

	cl_err = clEnqueueNDRangeKernel (my_cl_command_queue, my_cl_kernel, 2, NULL, global_work_size,  local_work_size, 1, &AcquireTextureDone, &RenderTextureDone);
	cl_perr("clEnqueueNDRangeKernel", cl_err);

	cl_err = clEnqueueReleaseGLObjects(my_cl_command_queue, 1, &my_cl_rendertexture_buffer, 1, &RenderTextureDone, &ReleaseTextureDone);	
	cl_perr("clEnqueueReleaseGLObjects", cl_err);

	clWaitForEvents(1, &ReleaseTextureDone);

	//clFinish(my_cl_command_queue);


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
    //glFinish();

	glutSwapBuffers();
	glutPostRedisplay();
	frameCount++;
	
	if(frameCount % 100 == 0)
	{
		time_t now;
		time(&now);
		cout << (frameCount/((float)(now-startTime))) << " FPS" << endl;

	}
}

int loadFile(char* path, char** data)
{
	*data = NULL;
	FILE* file = fopen(path, "rb");
	if(file == NULL) return 0;

	fseek(file, 0, SEEK_END);
	long size = ftell(file);
	fseek(file, 0, SEEK_SET);

	*data = new char[size+1];
	if(data == NULL) return 0;

	size_t bytesRead = fread((void*)(*data),1,size,file);
	*((*data)+size) = 0;
	return bytesRead;
}

void GetDesktopResolution(int& horizontal, int& vertical)
{
   RECT desktop;
   const HWND hDesktop = GetDesktopWindow();
   GetWindowRect(hDesktop, &desktop);
   horizontal = desktop.right;
   vertical = desktop.bottom;
}

int main(int argc, char** argv) 
{
	GetDesktopResolution(screen_width, screen_height);

	camPosition.s[0] = 0;
	camPosition.s[1] = 0;
	camPosition.s[2] = -5;

	glutInit(&argc, argv);
	
	glutInitDisplayMode(GLUT_RGB | GLUT_DOUBLE | GLUT_DEPTH);
	glutInitWindowSize(screen_width, screen_height);
	glutInitWindowPosition(200,50);
	windowID = glutCreateWindow("OpenGL glDrawPixels demo");

	glutDisplayFunc(display);
	//glutReshapeFunc(reshape);
	//glutMouseFunc(mouse_button);
	//glutMotionFunc(mouse_motion);
	//glutKeyboardFunc(keyboard);
	//glutIdleFunc(idle);
  
	//glEnable(GL_DEPTH_TEST);
	//glClearColor(0.0, 0.0, 0.0, 1.0);
	//glPointSize(2);



	glEnable(GL_TEXTURE_2D);
	glGenTextures(1, &textureID); 	
	glBindTexture(GL_TEXTURE_2D, textureID);

	//const int textureSize = 1024;
	

	glTexParameterf(GL_TEXTURE_2D,  GL_TEXTURE_MAG_FILTER, GL_NEAREST);
    glTexParameterf(GL_TEXTURE_2D,  GL_TEXTURE_MIN_FILTER, GL_NEAREST);
	glTexImage2D(GL_TEXTURE_2D, 0,GL_RGBA, screen_width, screen_height, 0, GL_RGBA, GL_UNSIGNED_BYTE, NULL);
	
	

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



	
	char* kernel_file_c_str;
	size_t kernel_file_c_str_size = loadFile("../AcceleratedVoxelRendering/kernel.cl", &kernel_file_c_str);
		
	my_cl_program = clCreateProgramWithSource(my_cl_context, 1, (const char**)&kernel_file_c_str, &kernel_file_c_str_size, &cl_err);
	cl_perr("clCreateProgramWithSource", cl_err);

	cl_err = clBuildProgram(my_cl_program, 1, &my_cl_device, NULL, NULL, NULL);
	cl_perr("clBuildProgram", cl_err);

	if(cl_err != CL_SUCCESS)
	{
		char message_buffer[1024*16];
		clGetProgramBuildInfo (my_cl_program, my_cl_device, CL_PROGRAM_BUILD_LOG, 1024*16-1, message_buffer, NULL);
		cout << message_buffer << endl;
		char dummy;
		cin >> dummy;
	}
	else
	{
		my_cl_kernel = clCreateKernel (my_cl_program, "renderPixel", &cl_err);
		cl_perr("clCreateKernel", cl_err);

		
		my_cl_rendertexture_buffer = clCreateFromGLTexture2D(my_cl_context, CL_MEM_WRITE_ONLY, GL_TEXTURE_2D, 0, textureID, &cl_err);
		cl_perr("clCreateBuffer", cl_err);
		
		
		char* oct_file;
		size_t oct_file_size = loadFile("D:\\donut.oct", &oct_file);
		my_cl_octree_buffer = clCreateBuffer( 	my_cl_context , CL_MEM_READ_ONLY | CL_MEM_USE_HOST_PTR, oct_file_size, oct_file, &cl_err);
		cl_perr("clCreateBuffer", cl_err);

		glutFullScreen();
		time(&startTime);
		glutMainLoop();
	}
}
