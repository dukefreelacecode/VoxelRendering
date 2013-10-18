using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace VoxelPOC
{
    public partial class Form1 : Form
    {
        private List<SurfaceTangentPoint> pcloud;
        Octree octree;


        public Form1()
        {
            InitializeComponent();
            DoubleBuffered = true;
            WindowState = FormWindowState.Maximized;

            Text = "Voxel Renderer Proof Of Concept";
            pcloud = GeneratePointCloud.Donut();
            octree = new Octree(pcloud);

        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.TranslateTransform(20, 20);
            e.Graphics.DrawImage((Image)PointCloudRenderer.Render(pcloud, 100, 100), 0, 0);
            e.Graphics.DrawString("PointCloudRenderer: Renders a \nlist of points into a Z-Buffer image.", new Font("Arial", 16), new SolidBrush(Color.Black), 150, 20);

            e.Graphics.DrawImage((Image)FullRecursiveOctreeRenderer.Render(octree, 100, 100), 0, 200);
            e.Graphics.DrawString("FullRecursiveOctreeRenderer: Reads through the entire Octree \nand renders it into a Z-Buffer image.", new Font("Arial", 16), new SolidBrush(Color.Black), 150, 220);

            e.Graphics.DrawImage((Image)RaycastingOctreeRenderer.Render(octree, 100, 100), 0, 400);
            e.Graphics.DrawString("RaycastingOctreeRenderer: Propagates a ray through \nthe Octree for each pixel.", new Font("Arial", 16), new SolidBrush(Color.Black), 150, 420);
            Invalidate();
        }
    }
}
