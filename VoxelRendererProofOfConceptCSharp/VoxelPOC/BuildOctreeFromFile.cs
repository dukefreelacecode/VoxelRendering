using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace VoxelPOC
{
    public class BuildOctreeFromFile : Octree
    {
        public BuildOctreeFromFile()
        {
            byte[] file = null;
            {
                StreamReader s = new StreamReader("D:\\donut.oct");
                BinaryReader b = new BinaryReader(s.BaseStream);
                List<byte> data = new List<byte>();
                byte[] buffer = new byte[1024 * 16];
                int byteCount = 0;
                while ((byteCount = b.Read(buffer, 0, 1024 * 16)) > 0)
                {
                    byte[] buffer2 = new byte[byteCount];
                    Array.Copy(buffer, buffer2, byteCount);
                    data.AddRange(buffer2);
                }
                file = data.ToArray();
            }
            int offset = 0;
            Root = BuildOctreeFromFileRecursion(file, offset);
            LowerCornerWorld = new Vector3(-4, -4, -4);
            UpperCornerWorld = new Vector3(4, 4, 4);
        }

        private IOctreeNode BuildOctreeFromFileRecursion(byte[] file, int offset)
        {
            const byte ID_LEAF = 0x42;
            const byte ID_BRANCH = 0x43;

            if (file[offset] == ID_LEAF)
            {
                OctreeEndNode leaf = new OctreeEndNode();
                leaf.SurfaceNormal = new Vector3(0, -1, 0);
                return leaf;
            }
            else if (file[offset] == ID_BRANCH)
            {
                OctreeParent branch = new OctreeParent();

                if (getIntAt(file, offset + 1 + 4 * 0) > 0) branch.xyz = BuildOctreeFromFileRecursion(file, getIntAt(file, offset + 1 + 4 * 0));
                if (getIntAt(file, offset + 1 + 4 * 1) > 0) branch.xyZ = BuildOctreeFromFileRecursion(file, getIntAt(file, offset + 1 + 4 * 1));
                if (getIntAt(file, offset + 1 + 4 * 2) > 0) branch.xYz = BuildOctreeFromFileRecursion(file, getIntAt(file, offset + 1 + 4 * 2));
                if (getIntAt(file, offset + 1 + 4 * 3) > 0) branch.xYZ = BuildOctreeFromFileRecursion(file, getIntAt(file, offset + 1 + 4 * 3));

                if (getIntAt(file, offset + 1 + 4 * 4) > 0) branch.Xyz = BuildOctreeFromFileRecursion(file, getIntAt(file, offset + 1 + 4 * 4));
                if (getIntAt(file, offset + 1 + 4 * 5) > 0) branch.XyZ = BuildOctreeFromFileRecursion(file, getIntAt(file, offset + 1 + 4 * 5));
                if (getIntAt(file, offset + 1 + 4 * 6) > 0) branch.XYz = BuildOctreeFromFileRecursion(file, getIntAt(file, offset + 1 + 4 * 6));
                if (getIntAt(file, offset + 1 + 4 * 7) > 0) branch.XYZ = BuildOctreeFromFileRecursion(file, getIntAt(file, offset + 1 + 4 * 7));

                return branch;
            }
            else { throw new Exception("Broken file"); }
        }

        private int getIntAt(byte[] file, int offset)
        {
            return
                (file[offset + 0] << 0) |
                (file[offset + 1] << 8) |
                (file[offset + 2] << 16) |
                (file[offset + 3] << 24);
        }
    }
}
