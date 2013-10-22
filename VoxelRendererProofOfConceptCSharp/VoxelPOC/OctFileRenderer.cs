using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace VoxelPOC
{
    class OctFileRenderer
    {
        static byte[] file = null;
        public static int frameCount = 0;

        const double epsilon = 1e-10;
        const byte ID_LEAF = 0x42;
        const byte ID_BRANCH = 0x43;

        public static Bitmap Render(int width, int height)
        {
            if (file == null) loadFile();

            Vector3 light = (new Vector3(1, 1, .5).Normalized());
            bmp.FastBitmap fbmp = new bmp.FastBitmap(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3 from = transform(new Vector3(0, 0, 0), false);
                    Vector3 to = transform(new Vector3((double)x / width * 2 - 1, (double)y / height * 2 - 1, 1), false);
                    Ray ray = new Ray();
                    ray.Origin = (from);
                    ray.Direction = ((to) - (from)).Normalized();

                    ray.CurrentParameter = epsilon + RayParameterForBoxEntryPoint(ray, new Vector3(0, 0, 0), 1);

                    if (ray.CurrentParameter > 0)
                    {
                        /*tree.TraceRay(tree.Root, ray, new Vector3(0, 0, 0), 0);

                        if (ray.Result != null)
                        {
                            double dot = -Vector3.Dot(transform(light, true), ray.Result.SurfaceNormal);
                            int col = (int)(dot * 255);
                            if (col < 0) col = 0;
                            fbmp.SetPixel(x, y, Color.FromArgb(col, col, col));
                        }
                        else*/ fbmp.SetPixel(x, y, Color.Green);

                    }
                    else fbmp.SetPixel(x, y, Color.Red);

                    //fbmp.SetPixel(
                }
            }
            frameCount++;


            return fbmp.ToBitmap();
        }


        public static double RayParameterForBoxEntryPoint(Ray ray, Vector3 LowerLocalCorner, double boxWidth)
        {
            double[] canidates = new double[6];
            canidates[0] = (ray.SolveParameterForGivenX(LowerLocalCorner.X));
            canidates[1] = (ray.SolveParameterForGivenY(LowerLocalCorner.Y));
            canidates[2] = (ray.SolveParameterForGivenZ(LowerLocalCorner.Z));

            canidates[3] = (ray.SolveParameterForGivenX(LowerLocalCorner.X + boxWidth));
            canidates[4] = (ray.SolveParameterForGivenY(LowerLocalCorner.Y + boxWidth));
            canidates[5] = (ray.SolveParameterForGivenZ(LowerLocalCorner.Z + boxWidth));

            double min = double.MaxValue;

            foreach (var canidate in canidates)
                if (canidate > 0 && PointInBox(ray.ParameterPoint(canidate), LowerLocalCorner, boxWidth, epsilon) && canidate < min) min = canidate;

            return min < double.MaxValue ? min : -1;

        }

        private static bool PointInBox(Vector3 v, Vector3 lower, Vector3 upper)
        {
            return (lower.X <= v.X && v.X <= upper.X &&
                    lower.Y <= v.Y && v.Y <= upper.Y &&
                    lower.Z <= v.Z && v.Z <= upper.Z);
        }

        private static bool PointInBox(Vector3 v, Vector3 lower, double increment)
        {
            return (lower.X <= v.X && v.X <= lower.X + increment &&
                    lower.Y <= v.Y && v.Y <= lower.Y + increment &&
                    lower.Z <= v.Z && v.Z <= lower.Z + increment);
        }


        private static bool PointInBox(Vector3 v, Vector3 lower, Vector3 upper, double epsilon)
        {
            return (lower.X <= v.X + epsilon && v.X <= upper.X + epsilon &&
                    lower.Y <= v.Y + epsilon && v.Y <= upper.Y + epsilon &&
                    lower.Z <= v.Z + epsilon && v.Z <= upper.Z + epsilon);
        }

        private static bool PointInBox(Vector3 v, Vector3 lower, double increment, double epsilon)
        {
            return (lower.X <= v.X + epsilon && v.X <= lower.X + increment + epsilon &&
                    lower.Y <= v.Y + epsilon && v.Y <= lower.Y + increment + epsilon &&
                    lower.Z <= v.Z + epsilon && v.Z <= lower.Z + increment + epsilon);
        }

        private static void loadFile()
        {
            StreamReader s = new StreamReader("D:\\donut.oct");
            BinaryReader b = new BinaryReader(s.BaseStream);
            List<byte> data = new List<byte>();
            byte[] buffer = new byte[1024*16];
            int byteCount = 0;
            while ((byteCount = b.Read(buffer, 0, 1024 * 16)) > 0)
            {
                byte[] buffer2 = new byte[byteCount];
                Array.Copy(buffer, buffer2, byteCount);
                data.AddRange(buffer2);
            }
            file = data.ToArray();
        }

        private static Vector3 transform(Vector3 v, bool onlyRotation)
        {
            if (!onlyRotation) v = v + new Vector3(-.5, .5, -2);

            v = v.RotateZ(.1);
            v = v.RotateX(-frameCount * 0.05);

            return v;
        }
    }
}
