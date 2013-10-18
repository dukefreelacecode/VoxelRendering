using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace VoxelPOC
{
    public class FullRecursiveOctreeRenderer
    {
        public static int frameCount = 0;
        static Vector3 light;
        static BufferPixel[,] buffer;
        static int width;
        static int height;

        public static Bitmap Render(Octree tree, int width, int height)
        {
            buffer = new BufferPixel[width, height];
            FullRecursiveOctreeRenderer.width = width;
            FullRecursiveOctreeRenderer.height = height;
            light = (new Vector3(1, 1, 1).Normalized());

            
            /*for (int i = 0; i < points.Count; i++)
            {
                RenderPoint(points[i], width, height);
            }*/



            RecDrawOctree(tree, tree.Root, new Vector3(0, 0, 0), 0);




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

        private static void RecDrawOctree(Octree tree, IOctreeNode iOctreeNode,Vector3 LowerLocalCorner, int current_recursion)
        {
            if (iOctreeNode == null) return;

            
            double boxWidth = Math.Pow(2, -current_recursion);

            if (iOctreeNode is OctreeEndNode)
            {
                OctreeEndNode oen = ((OctreeEndNode)iOctreeNode);
                SurfaceTangentPoint stp = new SurfaceTangentPoint();
                stp.SurfaceNormal = oen.SurfaceNormal;
                stp.Location = tree.OctreeToWorld(LowerLocalCorner);
                RenderPoint(stp);
            }
            if (iOctreeNode is OctreeParent)
            {
                double b = boxWidth/2;
                OctreeParent parent = (OctreeParent)iOctreeNode;

                RecDrawOctree(tree, parent.xyz, LowerLocalCorner + new Vector3(0, 0, 0), current_recursion + 1);
                RecDrawOctree(tree, parent.xyZ, LowerLocalCorner + new Vector3(0, 0, b), current_recursion + 1);
                RecDrawOctree(tree, parent.xYz, LowerLocalCorner + new Vector3(0, b, 0), current_recursion + 1);
                RecDrawOctree(tree, parent.xYZ, LowerLocalCorner + new Vector3(0, b, b), current_recursion + 1);

                RecDrawOctree(tree, parent.Xyz, LowerLocalCorner + new Vector3(b, 0, 0), current_recursion + 1);
                RecDrawOctree(tree, parent.XyZ, LowerLocalCorner + new Vector3(b, 0, b), current_recursion + 1);
                RecDrawOctree(tree, parent.XYz, LowerLocalCorner + new Vector3(b, b, 0), current_recursion + 1);
                RecDrawOctree(tree, parent.XYZ, LowerLocalCorner + new Vector3(b, b, b), current_recursion + 1);
            }
        }

        private static void RenderPoint(SurfaceTangentPoint point)
        {
            Vector3 v = transform(point.Location, false);
            if (v.Z > 0.001)
            {
                double x = v.X / v.Z;
                double y = v.Y / v.Z;
                if (-1 < x && x < 1 && -1 < y && y < 1)
                {
                    int _x = (int)((x + 1) * width / 2);
                    int _y = (int)((y + 1) * height / 2);
                    if (buffer[_x, _y].depth < 0.001 || (buffer[_x, _y].depth > 0.001 && buffer[_x, _y].depth > v.Z))
                    {
                        buffer[_x, _y].depth = v.Z;
                        double dot = -Vector3.Dot(transform(point.SurfaceNormal, true), light);
                        int col = (int)(dot * 255);
                        if (col < 0) col = 0;
                        buffer[_x, _y].color = Color.FromArgb(col, col, col);
                    }
                }
            }
        }

        private static Vector3 transform(Vector3 v, bool onlyRotation)
        {
            v = v.RotateX(frameCount * 0.05);
            v = v.RotateZ(.1);
            if (!onlyRotation) v += new Vector3(-1, -1, 7);
            return v;
        }
    }
}
