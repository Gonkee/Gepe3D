

using System.Collections.Generic;
using Gepe3D.Entities;
using Gepe3D.Util;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;

namespace Gepe3D.Core
{
    public abstract class GpScene
    {

        private readonly List<Entity> _entities = new List<Entity>();
        private Camera activeCam = new Camera( new Vector3(), 16f / 9f);

        public Vector3 ambientLight = new Vector3(0.65f, 0.84f, 0.95f);


        public abstract void Init();

        public abstract void Update(float delta);

        public void Render(Shader shader)
        {
            activeCam.Update();
            shader.SetMatrix4("cameraMatrix", activeCam.GetMatrix());
            shader.SetVector3("viewPos", activeCam.Position);
            shader.SetVector3("ambientLight", ambientLight);
            foreach (Entity e in _entities)
            {
                e.Render(shader);
            }
        }

        public void AddChild(Entity e)
        {
            if ( !_entities.Contains(e) )
            _entities.Add(e);
        }

    }
}