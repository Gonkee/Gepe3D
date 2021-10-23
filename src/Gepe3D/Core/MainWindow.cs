
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;

namespace Gepe3D
{
    public class MainWindow : GameWindow
    {
        
        static void Main(string[] args)
        {
            int WIDTH = 1280;
            int HEIGHT = 720;
            string TITLE = "Gepe3D";
            
            new MainWindow(
                GameWindowSettings.Default,
                new NativeWindowSettings()
                {
                    Size = new Vector2i(WIDTH, HEIGHT),
                    Title = TITLE,
                }
            ).Run();
        }
        
        public MainWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }
        
        protected override void OnLoad()
        {
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
        }
    }
}