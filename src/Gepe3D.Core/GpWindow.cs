using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using OpenTK;
using Gepe3D.Util;

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

            private Shader _shader;

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

                _shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
                _shader.Use();
            }

            protected override void OnUpdateFrame(FrameEventArgs e)
            {
                _scene.Update((float) e.Time);
            }

            protected override void OnRenderFrame(FrameEventArgs e)
            {
                base.OnRenderFrame(e);

                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                
                _shader.Use();
                
                _scene.Render();

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