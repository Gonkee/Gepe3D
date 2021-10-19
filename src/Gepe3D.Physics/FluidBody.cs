
using System;
using System.Collections.Generic;

namespace Gepe3D.Physics
{
    public class FluidBody : PhysicsBody
    {
        
        public override PhysicsData GetState()
        {
            return new PhysicsData(0);
        }

        public override PhysicsData GetDerivative(PhysicsData state)
        {
            return new PhysicsData(0);
        }

        public override void UpdateState(PhysicsData change, List<PhysicsBody> bodies)
        {
        }

        
        public override float MaxX() { return 0; }
        public override float MinX() { return 0; }
        public override float MaxY() { return 0; }
        public override float MinY() { return 0; }
        public override float MaxZ() { return 0; }
        public override float MinZ() { return 0; }

        public override Mesh GetMesh()
        {
            return null;
        }

        public override void Draw()
        {
            // mesh.Draw();
        }
    }
}