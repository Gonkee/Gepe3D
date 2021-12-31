
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;

namespace Gepe3D
{
    public class BallGen
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
        
        
        public static void GenBall(
            float x, float y, float z, float radius, int resolution,
            float[] posData,
            out int[] constraints,
            out float[] distances,
            int[] numConstraints
        ) {
            
            List<float> posDataList = new List<float>();
            
            Dictionary<Vector3i, int> coord2id = new Dictionary<Vector3i, int>();
            
            int particleCount = 0;
            for (int px = 0; px < resolution; px++)
            {
                for (int py = 0; py < resolution; py++)
                {
                    for (int pz = 0; pz < resolution; pz++)
                    {
                        float offsetX = MathHelper.Lerp( -radius, +radius, px / (resolution - 1f) );
                        float offsetY = MathHelper.Lerp( -radius, +radius, py / (resolution - 1f) );
                        float offsetZ = MathHelper.Lerp( -radius, +radius, pz / (resolution - 1f) );
                        float dist = MathF.Sqrt(offsetX * offsetX + offsetY * offsetY + offsetZ * offsetZ);
                        
                        if (dist <= radius) {
                            posDataList.Add( x + offsetX );
                            posDataList.Add( y + offsetY );
                            posDataList.Add( z + offsetZ );
                            
                            coord2id[ new Vector3i(px, py, pz) ] = particleCount;
                            particleCount++;
                        }
                    }
                }
            }
            
            List<int> constraintsList = new List<int>();
            List<float> distList = new List<float>();
            
            foreach (KeyValuePair<Vector3i, int> pair in coord2id)
            {
                Vector3i coord = pair.Key;
                
                foreach ( (Vector3i, Vector3i) connect in connections)
                {
                    Vector3i c1 = coord + connect.Item1;
                    Vector3i c2 = coord + connect.Item2;
                    
                    if (coord2id.ContainsKey(c1) && coord2id.ContainsKey(c2))
                    {
                        int p1 = coord2id[c1];
                        int p2 = coord2id[c2];
                        
                        Vector3 pos1 = new Vector3( posData[p1 * 3 + 0], posData[p1 * 3 + 1], posData[p1 * 3 + 2] );
                        Vector3 pos2 = new Vector3( posData[p2 * 3 + 0], posData[p2 * 3 + 1], posData[p2 * 3 + 2] );
                        float dist = (pos1 - pos2).Length;
                        
                        constraintsList.Add(p1);
                        constraintsList.Add(p2);
                        distList.Add(dist);
                        
                        numConstraints[p1]++;
                        numConstraints[p2]++;
                    }
                }
            }
            
            
            constraints = constraintsList.ToArray();
            distances = distList.ToArray();
            
            for (int i = 0; i < Math.Min(posData.Length, posDataList.Count); i++) {
                posData[i] = posDataList[i];
            }
            
        }
        
        
    }
}