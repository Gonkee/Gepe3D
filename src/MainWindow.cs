
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
        public ParticleSystem particleSystem;
        (int, float, float, float)[] barParticles;

        public Matrix4 camViewMatrix;
        public Matrix4 camProjectionMatrix;
        private float totalTime = 0;

        public MainWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {}

        protected override void OnLoad()
        {
            base.OnLoad();
            GL.ClearColor(0.4f, 0.4f, 0.4f, 1);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(TriangleFace.Back);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Blend);

            // set up camera
            (camViewMatrix, camProjectionMatrix) = MainWindow.GetCameraMatrices(
                fovDegrees : 50,
                aspectRatio : 16f / 9f,
                nearClip : 0.01f,
                farClip : 500f,
                camPosition : ParticleSystem.lowCenter + new Vector3(-12, 8, -6),
                camLookAt : ParticleSystem.lowCenter
            );

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

        private static (Matrix4, Matrix4) GetCameraMatrices(
            float fovDegrees,
            float aspectRatio,
            float nearClip,
            float farClip,
            Vector3 camPosition,
            Vector3 camLookAt
        ) {
            float dx = camLookAt.X - camPosition.X;
            float dy = camLookAt.Y - camPosition.Y;
            float dz = camLookAt.Z - camPosition.Z;
            float horizontalDist = MathF.Sqrt(dx * dx + dz * dz);
            float pitch = MathHelper.RadiansToDegrees( MathF.Atan2(dy, horizontalDist) );
            float yaw =   MathHelper.RadiansToDegrees( MathF.Atan2(dz, dx) );

            Vector3 localForward = Vector3.Normalize(new Vector3(
                MathF.Cos( MathHelper.DegreesToRadians(pitch) ) * MathF.Cos( MathHelper.DegreesToRadians(yaw) ),
                MathF.Sin( MathHelper.DegreesToRadians(pitch) ),
                MathF.Cos( MathHelper.DegreesToRadians(pitch) ) * MathF.Sin( MathHelper.DegreesToRadians(yaw) )
            ));
            Vector3 localRight = Vector3.Normalize( Vector3.Cross(localForward, Vector3.UnitY) );
            Vector3 localUp    = Vector3.Normalize( Vector3.Cross(localRight  , localForward) );

            Matrix4 viewMatrix = Matrix4.LookAt(camPosition, camPosition + localForward, localUp);
            viewMatrix.Transpose();

            Matrix4 projectionMatrix = Matrix4.CreatePerspectiveFieldOfView( MathHelper.DegreesToRadians(fovDegrees), aspectRatio, nearClip, farClip );
            projectionMatrix.Transpose();

            return (viewMatrix, projectionMatrix);
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

            // update bar position
            foreach ((int id, float px, float py, float pz) in barParticles) {
                particleSystem.SetPos(id,
                    px + MathF.Cos(totalTime * 1.5f) * ParticleSystem.MAX_X * 0.3f,
                    py,
                    pz + MathF.Sin(totalTime * 1.5f) * ParticleSystem.MAX_Z * 0.3f
                );
            }

            particleSystem.Render(this);

            SwapBuffers();
        }

        // using same function for both update and render
        protected override void OnRenderFrame(FrameEventArgs e) { }

    }
}
