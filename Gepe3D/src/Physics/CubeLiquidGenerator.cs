
using OpenTK.Mathematics;

namespace Gepe3D
{
    public class CubeLiquidGenerator
    {
        
        
        public static void AddCube(
            ParticleSimulator simulator,
            float x, float y, float z,
            float xLength, float yLength, float zLength,
            int xRes, int yRes, int zRes
        ) {
            
            int[] particleIDs = new int[xRes * yRes * zRes];
            
            int pointer = 0;
            float tx, ty, tz;
            for (int px = 0; px < xRes; px++)
            {
                for (int py = 0; py < yRes; py++)
                {
                    for (int pz = 0; pz < zRes; pz++)
                    {
                        tx = MathHelper.Lerp(x, x + xLength, px / (xRes - 1f) );
                        ty = MathHelper.Lerp(y, y + yLength, py / (yRes - 1f) );
                        tz = MathHelper.Lerp(z, z + zLength, pz / (zRes - 1f) );

                        particleIDs[pointer++] = simulator.AddParticle(tx, ty, tz);
                    }
                }
            }
            
            simulator.fluidConstraints.Add( new FluidConstraint(particleIDs, 600f, ParticleSimulator.GRID_CELL_WIDTH) );
            
        }
        
        
    }
}