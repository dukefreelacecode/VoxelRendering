using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VoxelPOC
{
    public struct OctreeParent : IOctreeNode
    {
        public IOctreeNode xyz;
        public IOctreeNode xyZ;
        public IOctreeNode xYz;
        public IOctreeNode xYZ;
        
        public IOctreeNode Xyz;
        public IOctreeNode XyZ;
        public IOctreeNode XYz;
        public IOctreeNode XYZ;
    }
}
