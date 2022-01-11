
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Gepe3D
{
    public class BallCharacter
    {
        
        ///////////////////////////
        // ball generation utils //
        ///////////////////////////
        
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
        
        //////////////////////////
        // ball character class //
        //////////////////////////
        
        private readonly ParticleSystem particleSystem;
        private readonly int centreParticleID;
        private Vector3 centrePos;
        public Camera activeCam = new Camera( new Vector3(), 16f / 9f);
        
        // initially points in the positive X
        private float pitch = 0;
        private float yaw = 0;
        public float Sensitivity = 0.2f;
        
        int[] particleIDs;
        
        
        public BallCharacter( ParticleSystem particleSystem, float x, float y, float z, float radius, int resolution ) {
            
            this.particleSystem = particleSystem;
            
            Dictionary<Vector3i, int> coord2id = new Dictionary<Vector3i, int>();
            
            List<int> particlesList = new List<int>();
            int centreParticleTemp = 0;
            float closestDist = float.MaxValue;
            
            int currentID = 0;
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
                            
                            particleSystem.SetPhase(currentID, ParticleSystem.PHASE_SOLID);
                            particleSystem.SetColour(currentID, 1, 0.6f, 0);
                            
                            particleSystem.SetPos(
                                currentID,
                                x + offsetX,
                                y + offsetY,
                                z + offsetZ
                            );
                            coord2id[ new Vector3i(px, py, pz) ] = currentID;
                            
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
                        Vector3 pos1 = particleSystem.GetPos(p1);
                        Vector3 pos2 = particleSystem.GetPos(p2);
                        float dist = (pos1 - pos2).Length;
                        particleSystem.AddDistConstraint(p1, p2, dist);
                    }
                }
            }
            
            this.centreParticleID = centreParticleTemp;
            this.particleIDs = particlesList.ToArray();
        }
        
        public void MouseMovementUpdate(Vector2 mouseDelta) {
            
            yaw   += mouseDelta.X * Sensitivity;
            pitch -= mouseDelta.Y * Sensitivity;
            pitch = MathHelper.Clamp(pitch, -89.9f, 89.9f);
        }
        
        public void Update(float delta, KeyboardState keyboardState) {
            
            
            Vector3 camOffset = new Vector3(7, 0, 0);
            camOffset = Vector3.TransformColumn( Matrix3.CreateRotationZ( MathHelper.DegreesToRadians(pitch) ), camOffset );
            camOffset = Vector3.TransformColumn( Matrix3.CreateRotationY( MathHelper.DegreesToRadians(yaw) ), camOffset );
            
            centrePos = particleSystem.GetPos(centreParticleID);
            activeCam.SetPos(centrePos + camOffset);
            activeCam.LookAt(centrePos);
            activeCam.UpdateLocalVectors();
            
            Vector3 movement = new Vector3();
            if ( keyboardState.IsKeyDown(Keys.W)         ) { movement.X += 1; }
            if ( keyboardState.IsKeyDown(Keys.S)         ) { movement.X -= 1; }
            if ( keyboardState.IsKeyDown(Keys.A)         ) { movement.Z -= 1; }
            if ( keyboardState.IsKeyDown(Keys.D)         ) { movement.Z += 1; }
            if ( keyboardState.IsKeyDown(Keys.Space)     ) { movement.Y += 1; }
            if ( keyboardState.IsKeyDown(Keys.LeftShift) ) { movement.Y -= 1; }
            if (movement.Length != 0) movement.Normalize();

            Matrix4 rotation = Matrix4.CreateRotationY( MathHelper.DegreesToRadians(-yaw) );
            rotation.Transpose(); // OpenTK matrices are transposed by default for some reason

            Vector4 rotatedMovement = rotation * new Vector4(movement, 1);
            movement.X = -rotatedMovement.X;
            movement.Y =  rotatedMovement.Y;
            movement.Z = -rotatedMovement.Z;
            
            movement *= 0.1f;

            foreach (int pID in particleIDs) {
                particleSystem.AddVel(pID, movement.X, movement.Y, movement.Z);
            }
            
        }
        
        public float GetCenterX()
        {
            return centrePos.X;
        }
        
        
    }
}