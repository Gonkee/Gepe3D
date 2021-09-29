

using System.Collections.Generic;
using Gepe3D.Entities;

namespace Gepe3D.Core
{
    public abstract class GpScene
    {

        private readonly List<Entity> _entities = new List<Entity>();

        public abstract void Init();

        public abstract void Update(float delta);

        public void Render()
        {
            foreach (Entity e in _entities)
            {
                e.Render();
            }
        }

        public void AddChild(Entity e)
        {
            _entities.Add(e);
        }

    }
}