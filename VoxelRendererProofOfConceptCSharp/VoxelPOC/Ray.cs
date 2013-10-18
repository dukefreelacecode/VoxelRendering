using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VoxelPOC
{
    public class Ray
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public double CurrentParameter;
        public OctreeEndNode Result;


        public Vector3 ParameterPoint(double ray_parameter)
        {
            return Origin + Direction * ray_parameter;
        }
        public Vector3 ParameterPoint()
        {
            return Origin + Direction * CurrentParameter;
        }

        internal double SolveParameterForGivenZ(double z)
        {
            return (z - Origin.Z) / Direction.Z;
        }

        internal double SolveParameterForGivenY(double y)
        {
            return (y - Origin.Y) / Direction.Y;
        }

        internal double SolveParameterForGivenX(double x)
        {
            return (x - Origin.X) / Direction.X;
        }
    }
}
