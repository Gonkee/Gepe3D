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
                Title = ""
            };
            GpWindow window = new GpWindow(config, new TestScene());
        }

        private class TestScene : GpScene
        {
            public override void Init()
            {
                // AddChild(new Quad());
            }
            public override void Update(float delta)
            {
                
            }
        }
    }
}
