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
                AddChild( new Quad(-0.7f, -0.6f, 0.3f, 0.3f) );
                AddChild( new Quad(0.2f, 0.0f, 0.1f, 0.2f) );
                AddChild( new Quad(-0.7f, 0.5f, 0.3f, 0.15f) );
            }
            public override void Update(float delta)
            {
                
            }
        }
    }
}
