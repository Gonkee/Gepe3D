
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;

namespace Gepe3D
{
    public abstract class PhysicsBody
    {
    
        public abstract PhysicsData GetState();

        public abstract PhysicsData GetDerivative(PhysicsData state);

        public abstract void UpdateState(PhysicsData change, List<PhysicsBody> bodies);

        public abstract void Draw();

        public abstract float MaxX();
        public abstract float MinX();
        public abstract float MaxY();
        public abstract float MinY();
        public abstract float MaxZ();
        public abstract float MinZ();

        public abstract Mesh GetMesh();
    }
}