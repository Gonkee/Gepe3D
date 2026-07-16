
using System;
using System.Collections.Generic;
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
            settings.UpdateFrequency = 0; // update as fast as possible

            MainWindow game = new MainWindow(
                settings,
                new NativeWindowSettings()
                {
                    ClientSize = new Vector2i(1280, 720),
                    Title = "Gepe3D",
                }
            );

            game.CenterWindow();
            game.Run();
        }


        public Vector3 ambientLight = new Vector3(0.2f, 0.2f, 0.2f);
        public Vector3 lightPos = new Vector3(0f, 10f, 0f);
        public SkyBox skyBox;
        public ParticleSystem particleSystem;
        (int, float, float, float)[] barParticles;

        // camera initially points in the positive X
        public Camera camera = new Camera( new Vector3(), 16f / 9f);
        private float pitch = 0;
        private float yaw = 0;
        public float Sensitivity = 0.2f;
        private float totalTime = 0;

        public MainWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {}

        protected override void OnLoad()
        {
            base.OnLoad();
            GL.ClearColor(1, 0, 1, 1);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(TriangleFace.Back);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Blend);

            CursorState = CursorState.Grabbed;

            skyBox = new SkyBox();
            particleSystem = new ParticleSystem(20000);

            ///////////////////////////////////////
            // Setting up fluid, ball and spikes //
            ///////////////////////////////////////

            Random rand = new Random();
            for (int i = 0; i < particleSystem.ParticleCount; i++) {
                float x = (float) rand.NextDouble() * ParticleSystem.MAX_X;
                float y = (float) rand.NextDouble() * ParticleSystem.MAX_Y * 0.5f;
                float z = (float) rand.NextDouble() * ParticleSystem.MAX_Z;
                particleSystem.SetPos(i, x, y, z);
                particleSystem.SetPhase(i, ParticleSystem.PHASE_LIQUID);
                particleSystem.SetColour( i, 0, 0.5f, 1 );
            }

            CreateBall(
                ParticleSystem.MAX_X * 0.5f,
                ParticleSystem.MAX_Y * 0.5f,
                ParticleSystem.MAX_Z * 0.5f,
                1.2f, 0.15f
            );

            barParticles = CreateBar(
                ParticleSystem.MAX_X * 0.4f, ParticleSystem.MAX_Y * 0.1f, ParticleSystem.MAX_Z * 0.4f,
                ParticleSystem.MAX_X * 0.2f, ParticleSystem.MAX_Y * 0.3f, ParticleSystem.MAX_Z * 0.2f,
                0.15f, 3000
            );
        }

        private void CreateBall(float x, float y, float z, float radius, float particleGap) {
            (Vector3i, Vector3i)[] connections = {
                // 1 axis
                ( new Vector3i(0, 0, 0), new Vector3i(1, 0, 0) ) ,
                ( new Vector3i(0, 0, 0), new Vector3i(0, 1, 0) ) ,
                ( new Vector3i(0, 0, 0), new Vector3i(0, 0, 1) ) ,

                // 2 axes
                ( new Vector3i(0, 0, 0), new Vector3i(1, 1, 0) ) ,
                ( new Vector3i(0, 0, 0), new Vector3i(0, 1, 1) ) ,
                ( new Vector3i(0, 0, 0), new Vector3i(1, 0, 1) ) ,

                // 2 axes other
                ( new Vector3i(1, 0, 0), new Vector3i(0, 1, 0) ) ,
                ( new Vector3i(1, 0, 0), new Vector3i(0, 0, 1) ) ,
                ( new Vector3i(0, 1, 0), new Vector3i(0, 0, 1) ) ,

                // 3 axes
                ( new Vector3i(0, 0, 0), new Vector3i(1, 1, 1) ) ,
                ( new Vector3i(1, 0, 0), new Vector3i(0, 1, 1) ) ,
                ( new Vector3i(0, 1, 0), new Vector3i(1, 0, 1) ) ,
                ( new Vector3i(0, 0, 1), new Vector3i(1, 1, 0) ) ,
            };

            int resolution = (int) (radius / particleGap) * 2;
            Dictionary<Vector3i, int> coord2id = new Dictionary<Vector3i, int>();
            List<int> particlesList = new List<int>();

            int currentID = 0;
            for (int px = 0; px < resolution; px++) {
                for (int py = 0; py < resolution; py++) {
                    for (int pz = 0; pz < resolution; pz++) {
                        float offsetX = MathHelper.Lerp( -radius, +radius, px / (resolution - 1f) );
                        float offsetY = MathHelper.Lerp( -radius, +radius, py / (resolution - 1f) );
                        float offsetZ = MathHelper.Lerp( -radius, +radius, pz / (resolution - 1f) );
                        float dist = MathF.Sqrt(offsetX * offsetX + offsetY * offsetY + offsetZ * offsetZ);
                        if (dist <= radius) {
                            particleSystem.SetPhase(currentID, ParticleSystem.PHASE_SOLID);
                            particleSystem.SetColour(currentID, 1, 0.6f, 0);
                            particleSystem.SetPos(
                                currentID,
                                x + offsetX,
                                y + offsetY,
                                z + offsetZ
                            );
                            coord2id[ new Vector3i(px, py, pz) ] = currentID;
                            particlesList.Add(currentID);
                            currentID++;
                        }
                    }
                }
            }

            foreach (KeyValuePair<Vector3i, int> pair in coord2id)
            {
                Vector3i coord = pair.Key;
                foreach ( (Vector3i, Vector3i) connect in connections)
                {
                    Vector3i c1 = coord + connect.Item1;
                    Vector3i c2 = coord + connect.Item2;
                    if (coord2id.ContainsKey(c1) && coord2id.ContainsKey(c2))
                    {
                        int p1 = coord2id[c1];
                        int p2 = coord2id[c2];
                        Vector3 pos1 = particleSystem.GetPos(p1);
                        Vector3 pos2 = particleSystem.GetPos(p2);
                        float dist = (pos1 - pos2).Length;
                        particleSystem.AddDistConstraint(p1, p2, dist);
                    }
                }
            }
        }

        private (int, float, float, float)[] CreateBar(
            float x, float y, float z,
            float dimX, float dimY, float dimZ,
            float particleGap, int startID
        ) {
            int resX = (int) (dimX / particleGap) + 1;
            int resY = (int) (dimY / particleGap) + 1;
            int resZ = (int) (dimZ / particleGap) + 1;

            List<(int, float, float, float)> particlesList = new List<(int, float, float, float)>();
            int currentID = startID;
            for (int i = 0; i < resX; i++) {
                for (int j = 0; j < resY; j++) {
                    for (int k = 0; k < resZ; k++) {
                        particleSystem.SetPhase(currentID, ParticleSystem.PHASE_STATIC);
                        particleSystem.SetColour(currentID, 0.4f, 0.4f, 0.4f);
                        float px = x + i * particleGap;
                        float py = y + j * particleGap;
                        float pz = z + k * particleGap;
                        particleSystem.SetPos(currentID, px, py, pz);
                        particlesList.Add((currentID, px, py, pz));
                        currentID++;
                    }
                }
            }
            return particlesList.ToArray();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            // update
            if (KeyboardState.IsKeyDown(Keys.Escape)) Close();

            float delta = 0.01f;
            totalTime += delta;
            particleSystem.Update(delta);

            // render
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // update camera
            yaw   += MouseState.Delta.X * Sensitivity;
            pitch -= MouseState.Delta.Y * Sensitivity;
            pitch = MathHelper.Clamp(pitch, -89.9f, 89.9f);
            Vector3 camOffset = new Vector3(15, 0, 0);
            camOffset = Vector3.TransformColumn( Matrix3.CreateRotationZ( MathHelper.DegreesToRadians(pitch) ), camOffset );
            camOffset = Vector3.TransformColumn( Matrix3.CreateRotationY( MathHelper.DegreesToRadians(yaw) ), camOffset );
            camera.SetPos(ParticleSystem.center + camOffset);
            camera.LookAt(ParticleSystem.center);
            camera.UpdateLocalVectors();

            // update bar position
            foreach ((int id, float px, float py, float pz) in barParticles) {
                particleSystem.SetPos(id,
                    px + MathF.Cos(totalTime) * ParticleSystem.MAX_X * 0.3f,
                    py,// + MathF.Cos(totalTime) * ParticleSystem.MAX_Y * 0.4f,
                    pz + MathF.Sin(totalTime) * ParticleSystem.MAX_Z * 0.3f
                );
            }

            skyBox.Render(this);
            particleSystem.Render(this);

            SwapBuffers();
        }

        // using same function for both update and render
        protected override void OnRenderFrame(FrameEventArgs e) { }


    }
}
