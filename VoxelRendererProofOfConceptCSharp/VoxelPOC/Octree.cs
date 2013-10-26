using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace VoxelPOC
{
    public class Octree
    {
        public IOctreeNode Root { get; protected set; }
        public Vector3 LowerCornerWorld { get; protected set; }
        public Vector3 UpperCornerWorld { get; protected set; }
        const double epsilon = 1e-10;

        public static List<int> rayTraceStepCountLog = new List<int>();
        static int rayTraceStepCount = 0;
        public int bytes = 0;

        const byte ID_LEAF = 0x42;
        const byte ID_BRANCH = 0x43;


        public Vector3 WorldToOctree(Vector3 v)
        {
            return Vector3.PointwiseDivide(v - LowerCornerWorld, UpperCornerWorld - LowerCornerWorld);
        }


        public Vector3 OctreeToWorld(Vector3 v)
        {
            return Vector3.PointwiseMultiply(v, UpperCornerWorld - LowerCornerWorld) + LowerCornerWorld;
        }

        public Octree()
        { }

        public Octree(List<SurfaceTangentPoint> pcloud, int depth)
        {
            // find range of points
            FindMinMax(pcloud);

            // convert to local coortinates
            List<SurfaceTangentPoint> local_points = new List<SurfaceTangentPoint>(pcloud.Count);
            foreach (var p in pcloud)
            {
                var x = p;
                x.Location = WorldToOctree(x.Location);
                local_points.Add(x);
            }

            // build the tree
            Root = BuildOctree(local_points, new Vector3(0, 0, 0), 0, depth);
        }

        private IOctreeNode BuildOctree(List<SurfaceTangentPoint> points, Vector3 LowerLocalCorner, int current_recursion, int max_recursion)
        {
            double boxWidth = Math.Pow(2, -current_recursion);
            List<SurfaceTangentPoint> local_points = new List<SurfaceTangentPoint>(points.Count);
            foreach (var p in points)
            {
                var x = p;
                if (PointInBox(p.Location, LowerLocalCorner, LowerLocalCorner + new Vector3(boxWidth, boxWidth, boxWidth))) 
                    local_points.Add(x);
            }

            if (local_points.Count == 0) { bytes++; return null; }
            if (current_recursion >= max_recursion)
            {
                OctreeEndNode node = new OctreeEndNode();
                node.SurfaceNormal = local_points[0].SurfaceNormal;
                bytes += 7;
                return node;
            }
            bytes += 8 * 4;

            double b = boxWidth/2;
            OctreeParent parent = new OctreeParent();

            parent.xyz = BuildOctree(local_points, LowerLocalCorner + new Vector3(0, 0, 0), current_recursion + 1, max_recursion);
            parent.xyZ = BuildOctree(local_points, LowerLocalCorner + new Vector3(0, 0, b), current_recursion + 1, max_recursion);
            parent.xYz = BuildOctree(local_points, LowerLocalCorner + new Vector3(0, b, 0), current_recursion + 1, max_recursion);
            parent.xYZ = BuildOctree(local_points, LowerLocalCorner + new Vector3(0, b, b), current_recursion + 1, max_recursion);

            parent.Xyz = BuildOctree(local_points, LowerLocalCorner + new Vector3(b, 0, 0), current_recursion + 1, max_recursion);
            parent.XyZ = BuildOctree(local_points, LowerLocalCorner + new Vector3(b, 0, b), current_recursion + 1, max_recursion);
            parent.XYz = BuildOctree(local_points, LowerLocalCorner + new Vector3(b, b, 0), current_recursion + 1, max_recursion);
            parent.XYZ = BuildOctree(local_points, LowerLocalCorner + new Vector3(b, b, b), current_recursion + 1, max_recursion);

            return parent;
        }

        private bool PointInBox(Vector3 v, Vector3 lower, Vector3 upper)
        {
            return (lower.X <= v.X && v.X <= upper.X &&
                    lower.Y <= v.Y && v.Y <= upper.Y &&
                    lower.Z <= v.Z && v.Z <= upper.Z);
        }

        private bool PointInBox(Vector3 v, Vector3 lower, double increment)
        {
            return (lower.X <= v.X && v.X <= lower.X + increment &&
                    lower.Y <= v.Y && v.Y <= lower.Y + increment &&
                    lower.Z <= v.Z && v.Z <= lower.Z + increment);
        }


        private bool PointInBox(Vector3 v, Vector3 lower, Vector3 upper, double epsilon)
        {
            return (lower.X <= v.X + epsilon && v.X <= upper.X + epsilon &&
                    lower.Y <= v.Y + epsilon && v.Y <= upper.Y + epsilon &&
                    lower.Z <= v.Z + epsilon && v.Z <= upper.Z + epsilon);
        }

        private bool PointInBox(Vector3 v, Vector3 lower, double increment, double epsilon)
        {
            return (lower.X <= v.X + epsilon && v.X <= lower.X + increment + epsilon &&
                    lower.Y <= v.Y + epsilon && v.Y <= lower.Y + increment + epsilon &&
                    lower.Z <= v.Z + epsilon && v.Z <= lower.Z + increment + epsilon);
        }

        private void FindMinMax(List<SurfaceTangentPoint> pcloud)
        {
            Vector3 min = new Vector3(double.MaxValue, double.MaxValue, double.MaxValue);
            Vector3 max = new Vector3(double.MinValue, double.MinValue, double.MinValue);

            foreach (var tp in pcloud)
            {
                var p = tp.Location;
                min.X = Math.Min(p.X, min.X);
                min.Y = Math.Min(p.Y, min.Y);
                min.Z = Math.Min(p.Z, min.Z);

                max.X = Math.Max(p.X, max.X);
                max.Y = Math.Max(p.Y, max.Y);
                max.Z = Math.Max(p.Z, max.Z);
            }

            var diag = max - min;
            var center = (max + min)*.5;
            double span = Math.Max(diag.X, Math.Max(diag.Y, diag.Z));


            LowerCornerWorld = center + new Vector3(span / 2, span / 2, span / 2);
            UpperCornerWorld = center - new Vector3(span / 2, span / 2, span / 2);
        }

        public void TraceRay(IOctreeNode this_node, Ray ray, Vector3 LowerLocalCorner, int current_recursion)
        {
            double boxWidth = Math.Pow(2, -current_recursion);

            if (this_node is OctreeEndNode)
            {
                ray.Result = (OctreeEndNode)this_node;
                ray.CurrentParameter = 9999;
                return;
            }
            else if (this_node is OctreeParent)
            {
                while (PointInBox(ray.ParameterPoint(), LowerLocalCorner, boxWidth))
                {
                    // dispatch to the right child
                    double b = boxWidth / 2;

                    if (PointInBox(ray.ParameterPoint(), LowerLocalCorner + new Vector3(0, 0, 0), b))
                        TraceRay(((OctreeParent)this_node).xyz, ray, LowerLocalCorner + new Vector3(0, 0, 0), current_recursion + 1);

                    else if (PointInBox(ray.ParameterPoint(), LowerLocalCorner + new Vector3(0, 0, b), b))
                        TraceRay(((OctreeParent)this_node).xyZ, ray, LowerLocalCorner + new Vector3(0, 0, b), current_recursion + 1);

                    else if (PointInBox(ray.ParameterPoint(), LowerLocalCorner + new Vector3(0, b, 0), b))
                        TraceRay(((OctreeParent)this_node).xYz, ray, LowerLocalCorner + new Vector3(0, b, 0), current_recursion + 1);

                    else if (PointInBox(ray.ParameterPoint(), LowerLocalCorner + new Vector3(0, b, b), b))
                        TraceRay(((OctreeParent)this_node).xYZ, ray, LowerLocalCorner + new Vector3(0, b, b), current_recursion + 1);


                    else if (PointInBox(ray.ParameterPoint(), LowerLocalCorner + new Vector3(b, 0, 0), b))
                        TraceRay(((OctreeParent)this_node).Xyz, ray, LowerLocalCorner + new Vector3(b, 0, 0), current_recursion + 1);

                    else if (PointInBox(ray.ParameterPoint(), LowerLocalCorner + new Vector3(b, 0, b), b))
                        TraceRay(((OctreeParent)this_node).XyZ, ray, LowerLocalCorner + new Vector3(b, 0, b), current_recursion + 1);

                    else if (PointInBox(ray.ParameterPoint(), LowerLocalCorner + new Vector3(b, b, 0), b))
                        TraceRay(((OctreeParent)this_node).XYz, ray, LowerLocalCorner + new Vector3(b, b, 0), current_recursion + 1);

                    else if (PointInBox(ray.ParameterPoint(), LowerLocalCorner + new Vector3(b, b, b), b))
                        TraceRay(((OctreeParent)this_node).XYZ, ray, LowerLocalCorner + new Vector3(b, b, b), current_recursion + 1);
                }
                return;
            }
            else if (this_node == null)
            {
                // if this box is empty, pass the ray through
                ray.CurrentParameter = epsilon + RayParameterForBoxExitPoint(ray, LowerLocalCorner, boxWidth);

                if (ray.CurrentParameter < 0) 
                    throw new Exception("Oops");
                return;
            }

            throw new Exception("Oops what kind of tree is that? Ask the developer.");
        }

        public double RayParameterForBoxExitPoint(Ray ray, Vector3 LowerLocalCorner, double boxWidth)
        {
            double[] canidates = new double[6];
            canidates[0] = (ray.SolveParameterForGivenX(LowerLocalCorner.X));
            canidates[1] = (ray.SolveParameterForGivenY(LowerLocalCorner.Y));
            canidates[2] = (ray.SolveParameterForGivenZ(LowerLocalCorner.Z));

            canidates[3] = (ray.SolveParameterForGivenX(LowerLocalCorner.X + boxWidth));
            canidates[4] = (ray.SolveParameterForGivenY(LowerLocalCorner.Y + boxWidth));
            canidates[5] = (ray.SolveParameterForGivenZ(LowerLocalCorner.Z + boxWidth));

            double max = 0;

            foreach (var canidate in canidates)
                if (canidate > 0 && PointInBox(ray.ParameterPoint(canidate), LowerLocalCorner, boxWidth, epsilon) && canidate > max) max = canidate;

            return max > 0 ? max : -1;

        }


        public double RayParameterForBoxEntryPoint(Ray ray, Vector3 LowerLocalCorner, double boxWidth)
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

        public void WriteToFile(string path)
        {
            List<byte> data = new List<byte>(bytes);
            WriteToFileRec(data, Root);
            FileStream fs = File.Create(path);
            BinaryWriter bw = new BinaryWriter(fs);
            bw.Write(data.ToArray());
            bw.Close();
            fs.Close();
        }

        int WriteToFileRec(List<byte> data, IOctreeNode self_node)
        {
            int thisNodeOffset = data.Count;

            if (self_node is OctreeParent)
            {
                OctreeParent op = (OctreeParent)self_node;

                byte[] block = new byte[8 * 4 + 1];
                block[0] = ID_BRANCH;
                data.AddRange(block);


                writeIntToBytesAt(data, thisNodeOffset + 1 + 4 * 0, WriteToFileRec(data, op.xyz));
                writeIntToBytesAt(data, thisNodeOffset + 1 + 4 * 1, WriteToFileRec(data, op.xyZ));
                writeIntToBytesAt(data, thisNodeOffset + 1 + 4 * 2, WriteToFileRec(data, op.xYz));
                writeIntToBytesAt(data, thisNodeOffset + 1 + 4 * 3, WriteToFileRec(data, op.xYZ));

                writeIntToBytesAt(data, thisNodeOffset + 1 + 4 * 4, WriteToFileRec(data, op.Xyz));
                writeIntToBytesAt(data, thisNodeOffset + 1 + 4 * 5, WriteToFileRec(data, op.XyZ));
                writeIntToBytesAt(data, thisNodeOffset + 1 + 4 * 6, WriteToFileRec(data, op.XYz));
                writeIntToBytesAt(data, thisNodeOffset + 1 + 4 * 7, WriteToFileRec(data, op.XYZ));



            }
            else if (self_node is OctreeEndNode)
            {
                OctreeEndNode oen = (OctreeEndNode)self_node;

                byte[] block = new byte[7];
                block[0] = ID_LEAF;
                block[1] = 111; // red
                block[2] = 112; // green 
                block[3] = 113; // blue
                block[4] = (byte)((char)(oen.SurfaceNormal.X * 126));
                block[5] = (byte)((char)(oen.SurfaceNormal.Y * 126));
                block[6] = (byte)((char)(oen.SurfaceNormal.Z * 126));

                data.AddRange(block);
            }
            else if (self_node == null) return 0;

            return thisNodeOffset;

        }

        private void writeIntToBytesAt(List<byte> data, int index, int integer)
        {
            byte[] bytes = new byte[4]
            {
                (byte)((integer>>0)&0x000000ff),
                (byte)((integer>>8)&0x000000ff),
                (byte)((integer>>16)&0x000000ff),
                (byte)((integer>>24)&0x000000ff),
            };

            for (int i = 0; i < 4; i++)
            {
                data[index + i] = bytes[i];
            }
        }
    }
}
