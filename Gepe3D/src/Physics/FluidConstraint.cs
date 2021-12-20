
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace Gepe3D
{
    public class FluidConstraint
    {
        
        float restDensity;
        Particle[] fluidParticles;
        List<Particle>[] neighbours;
        Vector3[] corrections;
        Dictionary<Particle, float> lambdas;
        
        
        // kernel values
        float h, poly6coeff, spikyGradCoeff;
        
        // Epsilon in gamma correction denominator
        float RELAXATION = 0.01f;
        
        // Pressure terms
        float K_P  = 0.1f;
        float E_P  = 4.0f;
        float DQ_P = 0.2f;
        
        
        public FluidConstraint(Particle[] fluidParticles, float restDensity, float particleEffectRadius)
        {
            this.fluidParticles = fluidParticles;
            this.restDensity = restDensity;
            this.lambdas = new Dictionary<Particle, float>();
            this.corrections = new Vector3[fluidParticles.Length];
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
                        neighbours[i].Add(p2); // it will add itself as well

                        // the added bit should be multiplied by an extra scalar if its a solid
                        density += (1f / p2.inverseMass) * Kernel_Poly6(dist);
                        
                        // if p1 == p2, spiky gradient will be zero and will require a different calculation below
                        float constraintGradient = Kernel_SpikyGrad(dist) / restDensity;
                        denominator += constraintGradient * constraintGradient;
                        
                    }
                }
                
                // add spiky grad when p1 == p2 onto denominator
                Vector3 grad = new Vector3();
                foreach (Particle p2 in neighbours[i])
                {
                    if (p1 == p2) continue;
                    Vector3 diff = p1.posEstimate - p2.posEstimate;
                    // the added bit should be multiplied by an extra scalar if its a solid
                    grad += Kernel_SpikyGrad( diff.Length ) * diff.Normalized();
                }
                grad /= restDensity;
                denominator += Vector3.Dot(grad, grad); // add dist squared
                
                lambdas[p1] = -( density / restDensity - 1 ) / (denominator + RELAXATION);
                
                // if (i == 30) System.Console.WriteLine(density);
            }
            
            
            for (int i = 0; i < fluidParticles.Length; i++)
            {
                Particle p1 = fluidParticles[i];
                Vector3 correction = new Vector3();
                
                foreach (Particle p2 in neighbours[i])
                {
                    if (p1 == p2) continue;
                    
                    Vector3 diff = p1.posEstimate - p2.posEstimate;
                    
                    Vector3 grad = Kernel_SpikyGrad( diff.Length ) * diff.Normalized();
                    
                    float lambdaCorr = -K_P * MathF.Pow( Kernel_Poly6(diff.Length) / Kernel_Poly6(DQ_P * h), E_P );
                    
                    float neighbourLambda = lambdas.ContainsKey(p2) ? lambdas[p2] : 0;
                    
                    correction += (lambdas[p1] + neighbourLambda + lambdaCorr) * grad;
                    
                }
                
                corrections[i] = correction / restDensity;
            }
            
            for (int i = 0; i < fluidParticles.Length; i++)
            {
                Particle p1 = fluidParticles[i];
                p1.posEstimate += corrections[i] / (float) (neighbours[i].Count + p1.constraintCount);
            }
            
        }
        
        
    }
}