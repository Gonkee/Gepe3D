

using System.Collections.Generic;
using Gepe3D.Entities;
using Gepe3D.Util;

namespace Gepe3D.Core
{
    public abstract class GpScene
    {

        private readonly List<Entity> _entities = new List<Entity>();

        public abstract void Init();

        public abstract void Update(float delta);

        public void Render(Shader shader)
        {
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