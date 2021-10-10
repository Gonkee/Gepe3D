using System;
using Gepe3D.Core;
using Gepe3D.Physics;
using OpenTK.Mathematics;

namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            GpWindow.Config config = new GpWindow.Config
            {
                Width = 1920,
                Height = 1080,
                Title = "Breugh"
            };
            GpWindow window = new GpWindow(config, new TestScene());
        }

        private class TestScene : GpScene
        {

            public override void Init()
            {
                Material red = new Material() { color = new Vector3(1.0f, 0.2f, 0.2f) };
                Geometry ico = GeometryGenerator.GenIcoSphere(0.5f, 2);
                SoftBody sphere = new SoftBody(ico, red);
                AddBody(sphere);
                
                Material white = new Material() { color = new Vector3(0.6f, 0.6f, 0.6f) };
                Geometry lPanel = GeometryGenerator.GenCube(3, 0.5f, 5);
                Geometry rPanel = GeometryGenerator.GenCube(3, 0.5f, 5);
                lPanel.Rotate( new Vector3(1, 0, 0),  45);
                lPanel.OffsetPosition(0, 0, -1f);
                rPanel.Rotate( new Vector3(1, 0, 0), -45);
                rPanel.OffsetPosition(0, 0, 3f);

                lPanel.OffsetPosition(0, -3, 0);
                StaticBody p1 = new StaticBody(lPanel, white);
                rPanel.OffsetPosition(0, -5, 0);
                StaticBody p2 = new StaticBody(rPanel, white);
                lPanel.OffsetPosition(0, -4.5f, 0);
                StaticBody p3 = new StaticBody(lPanel, white);
                rPanel.OffsetPosition(0, -4.5f, 0);
                StaticBody p4 = new StaticBody(rPanel, white);
                lPanel.OffsetPosition(0, -4.5f, 0);
                StaticBody p5 = new StaticBody(lPanel, white);
                rPanel.OffsetPosition(0, -4.5f, 0);
                StaticBody p6 = new StaticBody(rPanel, white);
                
                AddBody(p1);
                AddBody(p2);
                AddBody(p3);
                AddBody(p4);
                AddBody(p5);
                AddBody(p6);

                
                Geometry frontPanel = GeometryGenerator.GenCube(1f, 30, 30);
                frontPanel.OffsetPosition(-1.99f, 0, 0);
                Geometry backPanel = GeometryGenerator.GenCube(1f, 30, 30);
                backPanel.OffsetPosition( 1.99f, 0, 0);

                StaticBody frontWall = new StaticBody(frontPanel, white);
                StaticBody backWall = new StaticBody(backPanel, white);
                frontWall.Visible = false;
                backWall.Visible = false;

                AddBody(frontWall);
                AddBody(backWall);

                sphere.DrawWireframe = true;
            }

            public override void Update(float delta)
            {
            }
        }
    }
}
