using System;
using Gepe3D.Core;
using Gepe3D.Entities;

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
            public override void Init()
            {
                QuadMesh quad = new QuadMesh(0.5f, 0.5f, true);


                var e1 = new Entity(quad);
                var e2 = new Entity(quad);
                var e3 = new Entity(quad);
                
                AddChild(e1);
                AddChild(e2);
                AddChild(e3);

                e1.SetPosition( 0.5f,  0.5f, 0);
                e2.SetPosition(-0.5f,  0.5f, 0);
                e3.SetPosition( 0.5f, -0.5f, 0);

                e1.SetScale(0.5f);
                e2.SetScale(0.75f);
                e3.SetScale(0.2f);
            }
            public override void Update(float delta)
            {
                
            }
        }
    }
}
