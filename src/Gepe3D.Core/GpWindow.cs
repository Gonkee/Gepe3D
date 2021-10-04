
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;


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
                GL.ClearColor(0, 0, 0, 1);
                GL.Enable(EnableCap.DepthTest);
                GL.Enable(EnableCap.CullFace);
                GL.CullFace(CullFaceMode.Back);

                _scene.InitInternal();
                
                CursorGrabbed = true;
                Global.keyboardState = KeyboardState;
                Global.mouseState = MouseState;
            }

            protected override void OnRenderFrame(FrameEventArgs e)
            {
                if (Global.IsKeyDown(Keys.Escape)) { Close(); }

                Global.keyboardState = KeyboardState;
                Global.mouseState = MouseState;
                Global.Delta = (float) e.Time;

                _scene.Update(Global.Delta);

                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

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