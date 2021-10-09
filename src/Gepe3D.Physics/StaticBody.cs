

using Gepe3D.Core;

namespace Gepe3D.Physics
{
    public class StaticBody : PhysicsBody
    {
        public StaticBody(Geometry geometry, Material material) : base(geometry, material)
        {
            
        }

        public override float[] GetState()
        {
            throw new System.NotImplementedException();
        }

        public override float[] GetDerivative(float[] state)
        {
            throw new System.NotImplementedException();
        }

        public override void UpdateState(float[] change)
        {
            throw new System.NotImplementedException();
        }

    }
}