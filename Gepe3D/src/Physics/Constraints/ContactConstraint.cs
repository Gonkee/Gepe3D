

using OpenTK.Mathematics;

namespace Gepe3D
{
    public class ContactConstraint : PBD_data.Constraint
    {
        
        private PBD_data.Particle p1, p2;
        
        public ContactConstraint(PBD_data.Particle p1, PBD_data.Particle p2)
        {
            this.p1 = p1;
            this.p2 = p2;
            p1.constraintCount++;
            p2.constraintCount++;
        }
        
        public override void Project(bool stabilize)
        {
            Vector3 posDiff = stabilize ?
                p1.pos - p2.pos :
                p1.posEstimate - p2.posEstimate ;
            float weightSum = p1.inverseMass + p2.inverseMass;
            float centreDist = posDiff.Length;
            float edgeDist = centreDist - 2 * PBD_data.PARTICLE_RADIUS;
            
            // previous iterations have moved particles out of collision
            if (edgeDist > 0) return;
            
            float scale = edgeDist / weightSum;
            
            Vector3 adjustment = (scale / centreDist) * posDiff;
            Vector3 adj1 = p1.inverseMass * ( adjustment) / p1.constraintCount;
            Vector3 adj2 = p2.inverseMass * (-adjustment) / p2.constraintCount;
            
            p1.posEstimate += adj1;
            p2.posEstimate += adj2;

            if (stabilize)
            {
                p1.pos += adj1;
                p2.pos += adj2;
            }
        }
        
        public override float Evaluate()
        {
            return 0;
        }
    }
}