
using OpenTK.Mathematics;

namespace Gepe3D
{
    public class ClothGenerator
    {
        
        private static readonly (Vector2i, Vector2i)[] connections = 
        {
            ( new Vector2i(0, 0), new Vector2i(0, 1) ) ,
            ( new Vector2i(0, 0), new Vector2i(1, 0) ) ,
            ( new Vector2i(0, 0), new Vector2i(1, 1) ) ,
            ( new Vector2i(1, 0), new Vector2i(0, 1) ) ,
        };
        
        
        private static int coord2id(Vector2i coord, int xRes, int yRes)
        {
            return coord.X * yRes + coord.Y;
        }
        
        private static Vector2i id2coord(int id, int xRes, int yRes)
        {
            int x = id / yRes;
            int y = id % yRes;
            return new Vector2i(x, y);
        }
        
        
        public static void AddCloth(
            ParticleSimulator simulator,
            float x, float y, float z,
            float xLength, float zLength,
            int xRes, int zRes
        ) {
            
            Particle[] particles = new Particle[xRes * zRes];
            
            int pointer = 0;
            float tx, tz;
            for (int px = 0; px < xRes; px++)
            {
                for (int pz = 0; pz < zRes; pz++)
                {
                    tx = MathHelper.Lerp(x, x + xLength, px / (xRes - 1f) );
                    tz = MathHelper.Lerp(z, z + zLength, pz / (zRes - 1f) );

                    int currentP = pointer++;
                    particles[currentP] = simulator.AddParticle(tx, y, tz);
                    // if (px == 0) particles[currentP].inverseMass = 0;
                    if (px == 0) particles[currentP].immovable = true;
                }
            }
            
            for (int i = 0; i < particles.Length; i++)
            {
                Vector2i coord = id2coord(i, xRes, zRes);
                
                foreach ( (Vector2i, Vector2i) connect in connections)
                {
                    Vector2i c1 = coord + connect.Item1;
                    Vector2i c2 = coord + connect.Item2;
                    
                    if (
                        c1.X < xRes && c2.X < xRes &&
                        c1.Y < zRes && c2.Y < zRes
                    ) {
                        Particle p1 = particles[ coord2id(c1, xRes, zRes) ];
                        Particle p2 = particles[ coord2id(c2, xRes, zRes) ];
                        float dist = (p1.pos - p2.pos).Length;
                        simulator.AddDistanceConstraint(p1, p2, dist);
                        p1.constraintCount++;
                        p2.constraintCount++;
                    }
                }
                
                
                
            }
            
        }
        
        
        
    }
}