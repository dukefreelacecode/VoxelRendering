using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VoxelPOC
{
    public struct Vector3
    {
        public double X, Y, Z;

        public Vector3(double x, double y, double z)
        {
            X = x; Y = y; Z = z;
        }

        public static Vector3 operator +(Vector3 c1, Vector3 c2)
        {
            return new Vector3(c1.X + c2.X, c1.Y + c2.Y, c1.Z + c2.Z);
        }
        public static Vector3 operator -(Vector3 c1, Vector3 c2)
        {
            return new Vector3(c1.X - c2.X, c1.Y - c2.Y, c1.Z - c2.Z);
        }
        public static Vector3 operator *(Vector3 c1, double d)
        {
            return new Vector3(c1.X * d, c1.Y * d, c1.Z * d);
        }
        public Vector3 Normalized()
        {
            double len = this.Length;
            return new Vector3(this.X / len, this.Y / len, this.Z / len);
        }


        public double Length { get { return Math.Sqrt(this.SqLength); } }
        public double SqLength { get { return X * X + Y * Y + Z * Z; } }

        public static double Dot(Vector3 v, Vector3 v2)
        {
            return v.X * v2.X + v.Y * v2.Y + v.Z * v2.Z;
        }

        public Vector3 RotateX(double p)
        {
            return new Vector3(X, Math.Cos(p) * Y - Math.Sin(p) * Z, Math.Cos(p) * Z + Math.Sin(p) * Y);
        }

        public Vector3 RotateY(double p)
        {
            return new Vector3(Math.Cos(p) * X - Math.Sin(p) * Z, Y, Math.Cos(p) * Z + Math.Sin(p) * X);
        }

        public Vector3 RotateZ(double p)
        {
            return new Vector3(Math.Cos(p) * Y - Math.Sin(p) * X, Math.Cos(p) * X + Math.Sin(p) * Y, Z);
        }

        public static Vector3 PointwiseMultiply(Vector3 v, Vector3 u)
        {
            return new Vector3(v.X * u.X, v.Y * u.Y, v.Z * u.Z);
        }

        public static Vector3 PointwiseDivide(Vector3 v, Vector3 u)
        {
            return new Vector3(v.X / u.X, v.Y / u.Y, v.Z / u.Z);
        }

        public override string ToString()
        {
            return "(" + X.ToString() + "; " + Y.ToString() + "; " + Z.ToString() + ")";
        }
    }
}
