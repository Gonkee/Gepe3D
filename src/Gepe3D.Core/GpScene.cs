

using System.Collections.Generic;
using Gepe3D.Entities;
using Gepe3D.Util;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

namespace Gepe3D.Core
{
    public abstract class GpScene
    {


        private Shader _entityShader;
        private Shader _skyboxShader;

        private readonly List<Entity> _entities = new List<Entity>();
        private Camera activeCam = new Camera( new Vector3(), 16f / 9f);

        public Vector3 ambientLight = new Vector3(0.5f, 0.5f, 0.5f);

        public SkyBox skyBox;

        public abstract void Init();
        public abstract void Update(float delta);

        public void InitInternal()
        {
            Init();
            skyBox = new SkyBox();

            _skyboxShader = new Shader("Shaders/skybox.vert", "Shaders/skybox.frag");
            _entityShader = new Shader("Shaders/entity.vert", "Shaders/entity.frag");
            _entityShader.Use();
            _entityShader.SetVector3("lightPos", new Vector3(10, 10, 10));
        }

        public void Render()
        {
            activeCam.Update();

            _skyboxShader.Use();
            _skyboxShader.SetMatrix4("cameraMatrix", activeCam.GetMatrix());
            
            skyBox.PosX = activeCam.Position.X;
            skyBox.PosY = activeCam.Position.Y;
            skyBox.PosZ = activeCam.Position.Z;
            skyBox.UpdateTransform();
            _skyboxShader.SetMatrix4("modelMatrix", skyBox.TransformMatrix);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            skyBox.Mesh.Draw();


            _entityShader.Use();
            _entityShader.SetMatrix4("cameraMatrix", activeCam.GetMatrix());
            _entityShader.SetVector3("viewPos", activeCam.Position);
            _entityShader.SetVector3("ambientLight", ambientLight);

            foreach (Entity e in _entities)
            {
                e.UpdateTransform();
                _entityShader.SetMatrix4("modelMatrix", e.TransformMatrix);
                _entityShader.SetMatrix3("normalMatrix", e.NormalMatrix);
                
                if (e.Mesh != null)
                {

                    _entityShader.SetVector3("fillColor", e.Material.color);
                    // drawStyle
                    // 0 = fill
                    // 1 = line
                    // 2 = point

                    _entityShader.SetInt("drawStyle", 0);
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                    e.Mesh.Draw();

                    if (e.DrawWireframe)
                    {
                        _entityShader.SetInt("drawStyle", 1);
                        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                        GL.Enable(EnableCap.PolygonOffsetLine);
                        GL.PolygonOffset(-1, -1);
                        GL.LineWidth(4f);
                        e.Mesh.Draw();
                        GL.Disable(EnableCap.PolygonOffsetLine);
                        
                        _entityShader.SetInt("drawStyle", 2);
                        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point);
                        GL.Enable(EnableCap.PolygonOffsetPoint);
                        GL.PolygonOffset(-2, -2);
                        GL.PointSize(10f);
                        e.Mesh.Draw();
                        GL.Disable(EnableCap.PolygonOffsetPoint);
                    }
                }
            }
        }

        public void AddChild(Entity e)
        {
            if ( !_entities.Contains(e) )
            _entities.Add(e);
        }

        public void Draw(Shader shader)
        {


        }

    }
}