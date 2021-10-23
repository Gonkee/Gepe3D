using System;
using Gepe3D;
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
                Title = "Breugh",
                TickRate = 100
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
                // AddBody(sphere);
                
                Material white = new Material() { color = new Vector3(0.6f, 0.6f, 0.6f) };

                Geometry l1, l2, l3, r1, r2, r3;
                l1 = GeometryGenerator.GenCube(3, 0.5f, 5);
                r1 = GeometryGenerator.GenCube(3, 0.5f, 5);
                l1.Rotate( new Vector3(1, 0, 0),  45);
                r1.Rotate( new Vector3(1, 0, 0), -45);
                l1.OffsetPosition(0, -3, -1f);
                r1.OffsetPosition(0, -5,  3f);
                l2 = l1.Duplicate().OffsetPosition(0, -4.5f, 0);
                r2 = r1.Duplicate().OffsetPosition(0, -4.5f, 0);
                l3 = l2.Duplicate().OffsetPosition(0, -4.5f, 0);
                r3 = r2.Duplicate().OffsetPosition(0, -4.5f, 0);

                StaticBody p1 = new StaticBody(l1, white);
                StaticBody p2 = new StaticBody(r1, white);
                StaticBody p3 = new StaticBody(l2, white);
                StaticBody p4 = new StaticBody(r2, white);
                StaticBody p5 = new StaticBody(l3, white);
                StaticBody p6 = new StaticBody(r3, white);
                
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

                // StaticBody frontWall = new StaticBody(frontPanel, white);
                // StaticBody backWall = new StaticBody(backPanel, white);
                // frontWall.Visible = false;
                // backWall.Visible = false;

                // AddBody(frontWall);
                // AddBody(backWall);

                // sphere.DrawWireframe = true;

                FluidBody fluid = new FluidBody(
                    0, 0, 0,
                    3, 3, 3,
                    20, 20, 20
                );

                AddBody(fluid);
            }

            public override void Update(float delta)
            {
            }
        }
    }
}
