using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VoxelPOC
{
    public class GeneratePointCloud
    {
        public static List<SurfaceTangentPoint> Donut()
        {
            List<SurfaceTangentPoint> list = new List<SurfaceTangentPoint>();

            for (double a = 0; a < 1; a += .002)
            {
                Vector3 v1 = new Vector3(3 * Math.Cos(a * Math.PI * 2), 3 * Math.Sin(a * Math.PI * 2), 0);
                for (double b = 0; b < 1; b += .002)
                {
                    Vector3 v2 = new Vector3(Math.Cos(b * Math.PI * 2) * Math.Cos(a * Math.PI * 2), Math.Cos(b * Math.PI * 2) * Math.Sin(a * Math.PI * 2),  Math.Sin(b * Math.PI * 2));
                    v2 = v2.Normalized();
                    SurfaceTangentPoint p = new SurfaceTangentPoint();
                    p.Location = v1 + v2;
                    p.SurfaceNormal = v2;
                    list.Add(p);
                }
            }
            return list;
        }
    }
}
