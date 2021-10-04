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
                Width = 1920,
                Height = 1080,
                Title = "Breugh"
            };
            GpWindow window = new GpWindow(config, new TestScene());
        }

        private class TestScene : GpScene
        {


            Entity cube, ground;

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

                Mesh mesh = new CubeMesh(1f, 1f, 1f, true);

                cube = new Entity(mesh, red);
                ground = new Entity(mesh, white);
                
                AddChild(cube);
                AddChild(ground);

                cube.SetPosition( 0, 0, 0);
                ground.SetPosition(  0, -1f, 0);

                ground.SetScale(8, 0.5f, 8);

                cube.DrawWireframe = true;
            }

            public override void Update(float delta)
            {
                cube.ApplyRotation( Vector3.UnitY, delta * 20 );
            }
        }
    }
}
