using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace VoxelPOC
{
    class RaycastingOctreeRenderer
    {
        public static int frameCount = 0;

        public static Bitmap Render(Octree tree, int width, int height)
        {
            Vector3 light = (new Vector3(1, 1, .5).Normalized());
            bmp.FastBitmap fbmp = new bmp.FastBitmap(width, height);

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Vector3 from = transform(new Vector3(0, 0, 0), false);
                        Vector3 to = transform(new Vector3((double)x / width * 2 - 1, (double)y / height * 2 - 1, 1), false);
                        Ray ray = new Ray();
                        ray.Origin = tree.WorldToOctree(from);
                        ray.Direction = (tree.WorldToOctree(to) - tree.WorldToOctree(from)).Normalized();

                        ray.CurrentParameter = 1e-10 + tree.RayParameterForBoxEntryPoint(ray, new Vector3(0, 0, 0), 1);

                        if (ray.CurrentParameter > 0)
                        {
                            tree.TraceRay(tree.Root, ray, new Vector3(0, 0, 0), 0);

                            if (ray.Result != null)
                            {
                                double dot = -Vector3.Dot(transform(light, true), ray.Result.SurfaceNormal);
                                int col = (int)(dot * 255);
                                if (col < 0) col = 0;
                                fbmp.SetPixel(x, y, Color.FromArgb(col, col, col));
                            }
                            else fbmp.SetPixel(x, y, Color.Red);

                        }
                        else fbmp.SetPixel(x, y, Color.Red);

                        //fbmp.SetPixel(
                    }
                }
            frameCount++;


            return fbmp.ToBitmap();
        }

        private static Vector3 transform(Vector3 v, bool onlyRotation)
        {
            if(!onlyRotation) v = v + new Vector3(1, 1, -7);
            
            v = v.RotateZ(.1);
            v = v.RotateX(-frameCount * 0.05);
            
            return v;
        }
    }
}
