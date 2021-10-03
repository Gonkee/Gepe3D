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
                GL.Enable(EnableCap.DepthTest);

                GL.Enable(EnableCap.CullFace);
                GL.CullFace(CullFaceMode.Back);

                _scene.Init();

                _shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
                _shader.Use();
                _shader.SetVector3("lightPos", new Vector3(10, 10, 10));

                CursorGrabbed = true;

                Global.keyboardState = KeyboardState;
                Global.mouseState = MouseState;
            }

            protected override void OnUpdateFrame(FrameEventArgs e)
            {
                Global.keyboardState = KeyboardState;
                Global.mouseState = MouseState;
                Global.Delta = (float) e.Time;
                _scene.Update((float) e.Time);

                
                if (Global.IsKeyDown(Keys.Escape))
                {
                    Close();
                }
            }

            protected override void OnRenderFrame(FrameEventArgs e)
            {
                base.OnRenderFrame(e);

                GL.ClearColor(_scene.ambientLight.X, _scene.ambientLight.Y, _scene.ambientLight.Z, 1.0f);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                

                _shader.Use();
                
                _scene.Render(_shader);

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