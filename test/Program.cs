using System;
using Gepe3D.Core;
using Gepe3D.Entities;
using OpenTK.Mathematics;

namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            GpWindow.Config config = new GpWindow.Config
            {
                Width = 1280,
                Height = 720,
                Title = "Breugh"
            };
            GpWindow window = new GpWindow(config, new TestScene());
        }

        private class TestScene : GpScene
        {

            Entity e1, e2, e3;
            public override void Init()
            {
                Mesh mesh = new CubeMesh(0.5f, 0.5f, 0.5f, true);

                e1 = new Entity(mesh);
                e2 = new Entity(mesh);
                e3 = new Entity(mesh);
                
                AddChild(e1);
                AddChild(e2);
                AddChild(e3);

                e1.SetPosition( -1, 0, 0);
                e2.SetPosition(  0, 0, 0);
                e3.SetPosition(  1, 0, 0);
            }

            public override void Update(float delta)
            {
                e1.ApplyRotation( Vector3.UnitX, delta * 20 );
                e2.ApplyRotation( Vector3.UnitY, delta * 20 );
                e3.ApplyRotation( Vector3.UnitZ, delta * 20 );
            }
        }
    }
}
