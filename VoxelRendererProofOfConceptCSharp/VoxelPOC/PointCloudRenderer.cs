using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace VoxelPOC
{
    public class PointCloudRenderer
    {
        public static int frameCount = 0;

        public static Bitmap Render(List<SurfaceTangentPoint> points,int width, int height)
        {
            Vector3 light = (new Vector3(1, 1, 1).Normalized());

            BufferPixel[,] buffer = new BufferPixel[width, height];
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 v = transform(points[i].Location, false);
                if (v.Z > 0.001)
                {
                    double x = v.X / v.Z;
                    double y = v.Y / v.Z;
                    if(-1 < x && x < 1 && -1 < y && y < 1)
                    {
                        int _x = (int)((x + 1) * width/2);
                        int _y = (int)((y + 1) * height/2);
                        if (buffer[_x, _y].depth < 0.001 || (buffer[_x, _y].depth > 0.001 && buffer[_x, _y].depth > v.Z))
                        {
                            buffer[_x, _y].depth = v.Z;
                            double dot = -Vector3.Dot(transform(points[i].SurfaceNormal,true), light);
                            int col = (int)(dot * 255);
                            if (col < 0) col = 0;
                            buffer[_x, _y].color = Color.FromArgb(col, col, col);
                        }
                    }
                }
            }
            bmp.FastBitmap fbmp = new bmp.FastBitmap(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    fbmp.SetPixel(x, y, buffer[x, y].depth > 0.001 ? buffer[x, y].color : Color.Red);
                }
            }
            frameCount++;
            return fbmp.ToBitmap();
        }

        private static Vector3 transform(Vector3 v, bool onlyRotation)
        {
            v = v.RotateX(frameCount*0.05);
            v = v.RotateZ(.1);
            if(!onlyRotation) v += new Vector3(-1, -1, 7);
            return v;
        }
    }
}
