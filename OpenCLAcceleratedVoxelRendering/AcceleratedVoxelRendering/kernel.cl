__kernel void blub (global char* data, const int num)
{
   for (int i = 1; i < 200; i++)
      data[num+i] = 255;
}