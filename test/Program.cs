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
                Material red = new Material()
                {
                    color = new Vector3(1.0f, 0.2f, 0.2f)
                };
                
                Material white = new Material()
                {
                    color = new Vector3(0.6f, 0.6f, 0.6f)
                };

                Geometry ico = GeometryGenerator.GenIcoSphere(0.5f, 2);


                SoftBody sphere = new SoftBody(ico, red);
                
                AddBody(sphere);

                sphere.DrawWireframe = true;
            }

            public override void Update(float delta)
            {
            }
        }
    }
}
