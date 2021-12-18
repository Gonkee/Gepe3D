
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Gepe3D
{
    public class World : Scene
    {
        
        private Shader _entityShader;

        // private readonly List<PhysicsBody> _bodies = new List<PhysicsBody>();
        public Camera activeCam = new Camera( new Vector3(), 16f / 9f);

        public Vector3 ambientLight = new Vector3(0.2f, 0.2f, 0.2f);
        public Vector3 lightPos = new Vector3(1f, 0f, 0f);

        public SkyBox skyBox;
        
        private readonly Renderer renderer;
        
        // public PBD pbd;
        
        public ParticleRenderer prenderer;
        public ParticleSimulator simulator;

        
        public World(MainWindow window) : base(window)
        {
            skyBox = new SkyBox();
            renderer = new Renderer();
            
            simulator = new ParticleSimulator(36);
            prenderer = new ParticleRenderer(36, simulator);
            
            // CubeGenerator.AddCube(
            //     simulator,
            //     -0.5f, -0.5f, -0.5f,
            //     1, 1, 1,
            //     6, 6, 6
            // );
            
            
            ClothGenerator.AddCloth(
                simulator,
                -0.5f, 0.5f, -0.5f,
                1, 1,
                6, 6
            );

            _entityShader = new Shader("res/Shaders/entity.vert", "res/Shaders/entity.frag");
            _entityShader.Use();
            _entityShader.SetVector3("lightPos", lightPos);

            activeCam.Position = new Vector3( 1.7f, 0, 1.7f );
            activeCam.LookAt(0, -0.5f, 0);
            
            Init();
        }
        
        public override void Update()
        {
            float delta = 0.01f;
            Global.Elapsed += delta;
            
            activeCam.Update(delta);
            // foreach (PhysicsBody body in _bodies)
            // {
            //     PhysicsSolver.IntegrateExplicitEuler(body, delta, _bodies); // fixed timestep
            
            // }
            
            simulator.Update(delta);
            
            // pbd.Update(delta);
        }
        
        public override void Render()
        {
            activeCam.MouseInput(window.MouseState.Delta);

            renderer.Prepare(this);
            renderer.Render(skyBox);
            // foreach (PhysicsBody body in _bodies) renderer.Render(body);


            // _entityShader.Use();
            // _entityShader.SetMatrix4("cameraMatrix", activeCam.GetMatrix());
            // // _entityShader.SetMatrix4("viewMatrix", activeCam.GetViewMatrix());
            // // _entityShader.SetMatrix4("projectionMatrix", activeCam.GetProjectionMatrix());
            // _entityShader.SetVector3("viewPos", activeCam.Position);
            // _entityShader.SetVector3("ambientLight", ambientLight);

            // foreach (PhysicsBody body in _bodies)
            // {
            //     Mesh bodyMesh = body.GetMesh();
            //     if (body is FluidBody)
            //     {
            //         body.Draw();
            //         continue;
            //     }
            //     if (bodyMesh == null) continue;

            //     if (!bodyMesh.Visible) continue;
            //     _entityShader.SetVector3("fillColor", bodyMesh.Material.color);
            //     // drawStyle
            //     // 0 = fill
            //     // 1 = line
            //     // 2 = point

            //     _entityShader.SetInt("drawStyle", 0);
            //     GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            //     body.Draw();

            //     if (bodyMesh.DrawWireframe)
            //     {
            //         _entityShader.SetInt("drawStyle", 1);
            //         GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            //         GL.Enable(EnableCap.PolygonOffsetLine);
            //         GL.PolygonOffset(-1, -1);
            //         GL.LineWidth(4f);
            //         body.Draw();
            //         GL.Disable(EnableCap.PolygonOffsetLine);
                    
            //         _entityShader.SetInt("drawStyle", 2);
            //         GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point);
            //         GL.Enable(EnableCap.PolygonOffsetPoint);
            //         GL.PolygonOffset(-2, -2);
            //         GL.PointSize(10f);
            //         body.Draw();
            //         GL.Disable(EnableCap.PolygonOffsetPoint);
            //     }
            // }
            
            prenderer.Render(renderer);
            
            // pbd.Render(renderer);
        }
        
        // public void AddBody(PhysicsBody body)
        // {
        //     if ( !_bodies.Contains(body) )
        //     _bodies.Add(body);
        // }
        
        private void Init()
        {
            
                // Material red = new Material() { color = new Vector3(1.0f, 0.2f, 0.2f) };
                // Geometry ico = GeometryGenerator.GenIcoSphere(0.5f, 2);
                // SoftBody sphere = new SoftBody(ico, red);
                // AddBody(sphere);
                
                // Material white = new Material() { color = new Vector3(0.6f, 0.6f, 0.6f) };

                // Geometry l1, l2, l3, r1, r2, r3;
                // l1 = GeometryGenerator.GenCube(3, 0.5f, 5);
                // r1 = GeometryGenerator.GenCube(3, 0.5f, 5);
                // l1.Rotate( new Vector3(1, 0, 0),  45);
                // r1.Rotate( new Vector3(1, 0, 0), -45);
                // l1.OffsetPosition(0, -3, -1f);
                // r1.OffsetPosition(0, -5,  3f);
                // l2 = l1.Duplicate().OffsetPosition(0, -4.5f, 0);
                // r2 = r1.Duplicate().OffsetPosition(0, -4.5f, 0);
                // l3 = l2.Duplicate().OffsetPosition(0, -4.5f, 0);
                // r3 = r2.Duplicate().OffsetPosition(0, -4.5f, 0);

                // StaticBody p1 = new StaticBody(l1, white);
                // StaticBody p2 = new StaticBody(r1, white);
                // StaticBody p3 = new StaticBody(l2, white);
                // StaticBody p4 = new StaticBody(r2, white);
                // StaticBody p5 = new StaticBody(l3, white);
                // StaticBody p6 = new StaticBody(r3, white);
                
                // AddBody(p1);
                // AddBody(p2);
                // AddBody(p3);
                // AddBody(p4);
                // AddBody(p5);
                // AddBody(p6);
                
                // Geometry frontPanel = GeometryGenerator.GenCube(1f, 30, 30);
                // frontPanel.OffsetPosition(-1.99f, 0, 0);
                // Geometry backPanel = GeometryGenerator.GenCube(1f, 30, 30);
                // backPanel.OffsetPosition( 1.99f, 0, 0);

                // StaticBody frontWall = new StaticBody(frontPanel, white);
                // StaticBody backWall = new StaticBody(backPanel, white);
                // frontWall.Visible = false;
                // backWall.Visible = false;

                // AddBody(frontWall);
                // AddBody(backWall);

                // sphere.DrawWireframe = true;

                // float xL = 1f, yL = 1f, zL = 0.5f;
                // float radius = 0.2f;
                // float density = 2f;

                // FluidBody fluid = new FluidBody(
                //     -xL/2, -yL/2, -zL/2,
                //     xL, yL, zL,
                //     (int) (xL / radius * density), (int) (yL / radius * density), (int) (zL / radius * density),
                //     radius
                // );

                // AddBody(fluid);
                
            // pbd = new PBD();
                
        }
        
    }
}