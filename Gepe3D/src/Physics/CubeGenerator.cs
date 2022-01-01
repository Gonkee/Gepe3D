
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;

namespace Gepe3D
{
    public class CubeGenerator
    {
        
        
        private static readonly (Vector3i, Vector3i)[] connections = 
        {
            // 1 axis
            ( new Vector3i(0, 0, 0), new Vector3i(1, 0, 0) ) ,
            ( new Vector3i(0, 0, 0), new Vector3i(0, 1, 0) ) ,
            ( new Vector3i(0, 0, 0), new Vector3i(0, 0, 1) ) ,
            
            // 2 axes
            ( new Vector3i(0, 0, 0), new Vector3i(1, 1, 0) ) ,
            ( new Vector3i(0, 0, 0), new Vector3i(0, 1, 1) ) ,
            ( new Vector3i(0, 0, 0), new Vector3i(1, 0, 1) ) ,
            
            // 2 axes other
            ( new Vector3i(1, 0, 0), new Vector3i(0, 1, 0) ) ,
            ( new Vector3i(1, 0, 0), new Vector3i(0, 0, 1) ) ,
            ( new Vector3i(0, 1, 0), new Vector3i(0, 0, 1) ) ,
            
            // 3 axes
            ( new Vector3i(0, 0, 0), new Vector3i(1, 1, 1) ) ,
            ( new Vector3i(1, 0, 0), new Vector3i(0, 1, 1) ) ,
            ( new Vector3i(0, 1, 0), new Vector3i(1, 0, 1) ) ,
            ( new Vector3i(0, 0, 1), new Vector3i(1, 1, 0) ) ,
        };
        
        private static int coord2id(Vector3i coord, int xRes, int yRes, int zRes)
        {
            return
                coord.X * yRes * zRes +
                coord.Y * zRes + 
                coord.Z;
        }
        
        private static Vector3i id2coord(int id, int xRes, int yRes, int zRes)
        {
            int x = id / (yRes * zRes);
            int y = ( id % (yRes * zRes) ) / zRes;
            int z = ( id % (yRes * zRes) ) % zRes;
            return new Vector3i(x, y, z);
        }
        
        
        public static void AddCube(
            float x, float y, float z,
            float xLength, float yLength, float zLength,
            int xRes, int yRes, int zRes,
            float[] posData,
            out int[] constraints,
            out float[] distances,
            int[] numConstraints
        ) {
            
            int particleCount = 0;
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
                        

                        posData[particleCount * 3 + 0] = tx;
                        posData[particleCount * 3 + 1] = ty;
                        posData[particleCount * 3 + 2] = tz;
                        particleCount++;
                    }
                }
            }
            
            List<int> constraintsList = new List<int>();
            List<float> distList = new List<float>();
            
            
            for (int i = 0; i < particleCount; i++)
            {
                Vector3i coord = id2coord(i, xRes, yRes, zRes);
                
                foreach ( (Vector3i, Vector3i) connect in connections)
                {
                    Vector3i c1 = coord + connect.Item1;
                    Vector3i c2 = coord + connect.Item2;
                    
                    if (
                        c1.X < xRes && c2.X < xRes &&
                        c1.Y < yRes && c2.Y < yRes &&
                        c1.Z < zRes && c2.Z < zRes
                    ) {
                        int p1 = coord2id(c1, xRes, yRes, zRes);
                        int p2 = coord2id(c2, xRes, yRes, zRes);
                        
                        Vector3 pos1 = new Vector3( posData[p1 * 3 + 0], posData[p1 * 3 + 1], posData[p1 * 3 + 2] );
                        Vector3 pos2 = new Vector3( posData[p2 * 3 + 0], posData[p2 * 3 + 1], posData[p2 * 3 + 2] );
                        float dist = (pos1 - pos2).Length;
                        
                        constraintsList.Add(p1);
                        constraintsList.Add(p2);
                        distList.Add(dist);
                        
                        numConstraints[p1]++;
                        numConstraints[p2]++;
                        
                        // p1.constraintCount++;
                        // p2.constraintCount++;
                    }
                }
            }
            constraints = constraintsList.ToArray();
            distances = distList.ToArray();
        }
        
        
    }
}