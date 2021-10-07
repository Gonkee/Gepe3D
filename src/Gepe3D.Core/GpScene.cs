

using System.Collections.Generic;
using Gepe3D.Util;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using Gepe3D.Physics;
using System;

namespace Gepe3D.Core
{
    public abstract class GpScene
    {


        private Shader _entityShader;

        private readonly List<PhysicsBody> _bodies = new List<PhysicsBody>();
        private Camera activeCam = new Camera( new Vector3(), 16f / 9f);

        public Vector3 ambientLight = new Vector3(0.5f, 0.5f, 0.5f);

        public SkyBox skyBox;

        public abstract void Init();
        public abstract void Update(float delta);

        public void InitInternal()
        {
            Init();
            skyBox = new SkyBox();

            _entityShader = new Shader("res/Shaders/entity.vert", "res/Shaders/entity.frag");
            _entityShader.Use();
            _entityShader.SetVector3("lightPos", new Vector3(10, 10, 10));

            activeCam.Position = new Vector3( 2, 1.5f, 2 );
            activeCam.LookAt(0, -0.5f, 0);
        }

        public void Render()
        {
            // activeCam.Position = new Vector3( MathF.Cos(Global.Elapsed) * 3, 2, MathF.Sin(Global.Elapsed) * 2 );
            // activeCam.LookAt(0, 0, 0);

            activeCam.Update();

            skyBox.Draw(activeCam);

            _entityShader.Use();
            _entityShader.SetMatrix4("cameraMatrix", activeCam.GetMatrix());
            _entityShader.SetVector3("viewPos", activeCam.Position);
            _entityShader.SetVector3("ambientLight", ambientLight);

            foreach (PhysicsBody body in _bodies)
            {
                // e.UpdateTransform();
                // _entityShader.SetMatrix4("modelMatrix", e.TransformMatrix);
                // _entityShader.SetMatrix3("normalMatrix", e.NormalMatrix);

                body.Update();
                

                _entityShader.SetVector3("fillColor", body.Material.color);
                // drawStyle
                // 0 = fill
                // 1 = line
                // 2 = point

                _entityShader.SetInt("drawStyle", 0);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                body.Draw();

                if (body.DrawWireframe)
                {
                    _entityShader.SetInt("drawStyle", 1);
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                    GL.Enable(EnableCap.PolygonOffsetLine);
                    GL.PolygonOffset(-1, -1);
                    GL.LineWidth(4f);
                    body.Draw();
                    GL.Disable(EnableCap.PolygonOffsetLine);
                    
                    _entityShader.SetInt("drawStyle", 2);
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point);
                    GL.Enable(EnableCap.PolygonOffsetPoint);
                    GL.PolygonOffset(-2, -2);
                    GL.PointSize(10f);
                    body.Draw();
                    GL.Disable(EnableCap.PolygonOffsetPoint);
                }
            }
        }

        public void AddBody(PhysicsBody body)
        {
            if ( !_bodies.Contains(body) )
            _bodies.Add(body);
        }

        public void Draw(Shader shader)
        {


        }

    }
}