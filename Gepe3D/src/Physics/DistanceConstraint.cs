
using OpenTK.Mathematics;
using System;

namespace Gepe3D
{
    public class DistanceConstraint
    {
        Particle p1, p2;
        float restDistance;
        float stiffnessFac;
        
        public DistanceConstraint(Particle p1, Particle p2, float restDistance, float stiffness, int numIters)
        {
            this.p1 = p1;
            this.p2 = p2;
            this.restDistance = restDistance;
            this.stiffnessFac = 1 - MathF.Pow( 1 - stiffness, 1f / (float) numIters );
        }
        
        public void Project()
        {
            if ( (!p1.active) || (!p2.active) ) return;
            if ( p1.inverseMass == 0 && p2.inverseMass == 0 ) return;
            
            Vector3 posDiff = p1.posEstimate - p2.posEstimate;
            float displacement = posDiff.Length - restDistance;
            Vector3 direction = posDiff.Normalized();
            
            float w1 = p1.inverseMass / (p1.inverseMass + p2.inverseMass);
            float w2 = p2.inverseMass / (p1.inverseMass + p2.inverseMass);
            
            Vector3 correction1 = -w1 * displacement * direction;
            Vector3 correction2 = +w2 * displacement * direction;
            
            p1.posEstimate += correction1 * stiffnessFac;
            p2.posEstimate += correction2 * stiffnessFac;
        }
    }
}