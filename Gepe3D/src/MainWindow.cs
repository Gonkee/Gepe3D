
using System;
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
            
            GameWindowSettings settings = GameWindowSettings.Default;
            settings.UpdateFrequency = 100;
            
            MainWindow game = new MainWindow(
                settings,
                new NativeWindowSettings()
                {
                    Size = new Vector2i(1280, 720),
                    Title = "Gepe3D",
                    // WindowBorder = WindowBorder.Hidden
                }
            );
            
            game.CenterWindow();
            game.Run();
        }
        
        
        public Vector3 ambientLight = new Vector3(0.2f, 0.2f, 0.2f);
        public Vector3 lightPos = new Vector3(0f, 10f, 0f);
        public SkyBox skyBox;
        public ParticleSystem particleSystem;
        public BallCharacter character;
        public Spike[] spikes;
        
        public MainWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }
        
        protected override void OnLoad()
        {
            base.OnLoad();
            GL.ClearColor(1, 0, 1, 1);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Blend);
            
            CursorGrabbed = true;
            
            skyBox = new SkyBox();
            particleSystem = new ParticleSystem(7000);
            
            
            ///////////////////////////////////////
            // Setting up fluid, ball and spikes //
            ///////////////////////////////////////
            
            Random rand = new Random();
            for (int i = 0; i < particleSystem.ParticleCount; i++) {
                float x = (float) rand.NextDouble() * ParticleSystem.MAX_X;
                float y = (float) rand.NextDouble() * ParticleSystem.MAX_Y * 0.7f;
                float z = (float) rand.NextDouble() * ParticleSystem.MAX_Z * 0.3f + ParticleSystem.MAX_Z * 0.7f;
                particleSystem.SetPos(i, x, y, z);
                particleSystem.SetPhase(i, ParticleSystem.PHASE_LIQUID);
                
                particleSystem.SetColour( i, 0, 0.5f, 1 );
            }
            
            character = new BallCharacter(
                particleSystem,
                ParticleSystem.MAX_X * 0.5f,
                1.1f,
                ParticleSystem.MAX_Z * 0.3f,
                1, 12
            );
            
            spikes = new Spike[3];
            
            float radius = 1f;
            float x1 = radius - (ParticleSystem.MAX_X + radius * 2) * 1.333f;
            float x2 = radius - (ParticleSystem.MAX_X + radius * 2) * 1.667f;
            float x3 = radius - (ParticleSystem.MAX_X + radius * 2) * 2.000f;
            spikes[0] = new Spike(particleSystem, x1, Spike.RandZ(radius), 2f, radius, 800);
            spikes[1] = new Spike(particleSystem, x2, Spike.RandZ(radius), 2f, radius, 2000);
            spikes[2] = new Spike(particleSystem, x3, Spike.RandZ(radius), 2f, radius, 3000);
            
        }
        
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (KeyboardState.IsKeyDown(Keys.Escape)) Close();
                
            float delta = 0.01f;
            character.Update(delta, KeyboardState);
            foreach (Spike s in spikes) s.Update();
            float shiftX = 4.5f * delta;
            particleSystem.Update(delta, shiftX);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            character.MouseMovementUpdate(MouseState.Delta);
            skyBox.Render(this);
            particleSystem.Render(this);
            
            SwapBuffers();
        }
        
        
    }
}