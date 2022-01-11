
using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace Gepe3D
{
    public class Spike
    {
        
        static Random random = new Random();
        int centreParticleID;
        Vector3 centrePos;
        int[] particleIDs;
        float radius;
        
        ParticleSystem particleSystem;
        
        public Spike(ParticleSystem particleSystem, float x, float z, float height, float radius, int startID)
        {
            this.particleSystem = particleSystem;
            this.radius = radius;
            
            float gap = 0.15f;
            int xzRes = (int) (radius * 2 / gap);
            int  yRes = (int) (height / gap);
            
            List<int> particlesList = new List<int>();
            int centreParticleTemp = 0;
            float closestDist = float.MaxValue;
            
            int currentID = startID;
            for (int py = 0; py < yRes; py++)
            {
                for (int px = 0; px < xzRes; px++)
                {
                    for (int pz = 0; pz < xzRes; pz++)
                    {
                        float offsetY = MathHelper.Lerp( 0, height, py / (yRes - 1f) );
                        float offsetX = MathHelper.Lerp( -radius, +radius, px / (xzRes - 1f) );
                        float offsetZ = MathHelper.Lerp( -radius, +radius, pz / (xzRes - 1f) );
                        float horDist = MathF.Sqrt(offsetX * offsetX + offsetZ * offsetZ);
                        float dist = MathF.Sqrt(offsetX * offsetX + offsetY * offsetY + offsetZ * offsetZ);
                        
                        if (horDist <= MathHelper.Lerp(radius, 0, offsetY / height)) {
                            
                            particleSystem.SetPhase(currentID, ParticleSystem.PHASE_STATIC);
                            particleSystem.SetColour(currentID, 0.4f, 0.4f, 0.4f);
                            
                            particleSystem.SetPos(
                                currentID,
                                x + offsetX,
                                offsetY,
                                z + offsetZ
                            );
                            
                            if (dist < closestDist) {
                                closestDist = dist;
                                centreParticleTemp = currentID;
                            }
                            particlesList.Add(currentID);
                            
                            currentID++;
                        }
                    }
                }
            }
            this.centreParticleID = centreParticleTemp;
            this.particleIDs = particlesList.ToArray();
        }
        
        public void Update()
        {
            centrePos = particleSystem.GetPos(centreParticleID);
            
            if (centrePos.X > ParticleSystem.MAX_X + radius)
            {
                float targetX = 0 - radius + 0.01f;
                MoveTo(targetX);
            }
        }
        
        
        private void MoveTo(float targetX)
        {
            float randZ = RandZ(radius);
            
            Vector3 targetPos = new Vector3(targetX, 0, randZ);
            Vector3 diff = targetPos - centrePos;
            
            foreach (int pID in particleIDs)
            {
                particleSystem.AddPos(pID, diff.X, diff.Y, diff.Z);
            }
            
        }
        
        public static float RandZ(float radius)
        {
            float randFac = (float) random.NextDouble();
            randFac = randFac * randFac * (3 - 2 * randFac);
            return MathHelper.Lerp( 0 + radius, ParticleSystem.MAX_Z - radius, randFac );
        }
        
    }
}