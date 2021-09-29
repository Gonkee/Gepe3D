using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using OpenTK;

namespace Gepe3D.Core
{
    public class GpWindow
    {

        public class Config
        {
            public int Width { get; set; } = 800;
            public int Height { get; set; } = 600;
            public string Title { get; set; } = "Gepe3D";
        }

        private class EncapsulatedWindow : GameWindow
        {
            private readonly GpScene _scene;

            public EncapsulatedWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, GpScene scene)
                : base(gameWindowSettings, nativeWindowSettings)
            {
                this._scene = scene;
            }

            protected override void OnLoad()
            {
                base.OnLoad();
                GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
                GL.Enable(EnableCap.DepthTest);

                _scene.Init();
            }

            protected override void OnUpdateFrame(FrameEventArgs e)
            {
                _scene.Update((float) e.Time);
            }

            protected override void OnRenderFrame(FrameEventArgs e)
            {
                base.OnRenderFrame(e);

                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                SwapBuffers();
            }
        }

        private readonly EncapsulatedWindow _window;

        public GpWindow(GpWindow.Config config, GpScene scene)
        {
            _window = new EncapsulatedWindow(
                GameWindowSettings.Default,
                new NativeWindowSettings()
                {
                    Size = new Vector2i(config.Width, config.Height),
                    Title = config.Title
                },
                scene
            );
            _window.Run();
        }



        

    }

}