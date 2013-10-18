using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VoxelPOC
{
    class Timer
    {
        static DateTime _tic;
        public static void Tic()
        {
            _tic = DateTime.Now;
        }
        public static double Toc()
        {
            return DateTime.Now.Subtract(_tic).TotalSeconds;
        }

    }
}
