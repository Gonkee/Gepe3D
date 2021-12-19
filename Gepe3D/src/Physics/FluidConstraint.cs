
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace Gepe3D
{
    public class FluidConstraint
    {
        
        Particle[] fluidParticles;
        float[] lambdas;
        float restDensity;
        List<Particle>[] neighbours;
        
        // kernel values
        float h, poly6coeff, spikyGradCoeff;
        
        float RELAXATION = 0.01f;
        
        
        public FluidConstraint(Particle[] fluidParticles, float restDensity, float particleEffectRadius)
        {
            this.fluidParticles = fluidParticles;
            this.restDensity = restDensity;
            this.lambdas = new float[fluidParticles.Length];
            this.neighbours = new List<Particle>[fluidParticles.Length];
            for (int i = 0; i < fluidParticles.Length; i++)
                neighbours[i] = new List<Particle>();
            
            // kernel values
            this.h  = particleEffectRadius;
            this.poly6coeff = 315f / ( 64f * MathF.PI * MathF.Pow(h, 9) );
            this.spikyGradCoeff = -45 / (MathF.PI * MathF.Pow(h, 6) );
        }
        
        
        private float Kernel_Poly6(float dist)
        {
            dist = MathHelper.Clamp(dist, 0, h);
            float t = h * h - dist * dist;
            return poly6coeff * t * t * t;
        }
        
        private float Kernel_SpikyGrad(float dist)
        {
            dist = MathHelper.Clamp(dist, 0, h);
            if (dist == 0) return 0; // spiky gradient can't be used when its the same particle
            return spikyGradCoeff * (h - dist) * (h - dist);
        }
        
        public void Project(Particle[] allParticles)
        {
            for (int i = 0; i < fluidParticles.Length; i++)
            {
                Particle p1 = fluidParticles[i];
                neighbours[i].Clear();
                float density = 0;
                float denominator = 0;
                
                foreach (Particle p2 in allParticles)
                {
                    if (p2.inverseMass == 0) continue;
                    
                    float dist = (p1.posEstimate - p2.posEstimate).Length;
                    
                    if (dist < h)
                    {
                        // the added bit should be multiplied by an extra scalar if its a solid
                        density += (1f / p2.inverseMass) * Kernel_Poly6(dist);
                        
                        if (p1 != p2)
                        {
                            neighbours[i].Add(p2);
                            // if p1 == p2, spiky gradient will be zero - hence this is only done for neighbours
                            float constraintGradient = Kernel_SpikyGrad(dist) / restDensity;
                            denominator += constraintGradient * constraintGradient;
                        }
                        
                    }
                }
                
                // add spiky grad of same particle onto denominator
                Vector3 grad = new Vector3();
                foreach (Particle p2 in neighbours[i])
                {
                    Vector3 diff = p1.posEstimate - p2.posEstimate;
                    // the added bit should be multiplied by an extra scalar if its a solid
                    grad += Kernel_SpikyGrad( diff.Length ) * diff.Normalized();
                }
                grad /= restDensity;
                denominator += Vector3.Dot(grad, grad); // add dist squared
                
                lambdas[i] = -( density / restDensity - 1 ) / (denominator + RELAXATION);
            }
        }
        
        
    }
}