
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

namespace Gepe3D
{
    public class FluidBody : PhysicsBody
    {
        private readonly ParticleData state;
        private readonly Geometry particleShape;

        private readonly float x, y, z;
        private readonly float xLength, yLength, zLength;
        private readonly float xResolution, yResolution, zResolution;
        
        private readonly int _vaoID;
        private readonly int _meshVBO_ID;
        private readonly int _instanceVBO_ID;

        private readonly float[] particlePositions;

        public FluidBody(
            float x, float y, float z,
            float xLength, float yLength, float zLength,
            int xResolution, int yResolution, int zResolution)
        {
            this.x = x;
            this.y = y;
            this.z = z;

            this.xLength = xLength;
            this.yLength = yLength;
            this.zLength = zLength;

            this.xResolution = xResolution;
            this.yResolution = yResolution;
            this.zResolution = zResolution;

            state = new ParticleData(xResolution * yResolution * zResolution);
            particlePositions = new float[xResolution * yResolution * zResolution * 3];

            int pointer = 0;
            float tx, ty, tz;
            for (int px = 0; px < xResolution; px++)
            {
                for (int py = 0; py < yResolution; py++)
                {
                    for (int pz = 0; pz < zResolution; pz++)
                    {
                        tx = MathHelper.Lerp(x, x + xLength, px / (xResolution - 1f) );
                        ty = MathHelper.Lerp(y, y + yLength, py / (yResolution - 1f) );
                        tz = MathHelper.Lerp(z, z + zLength, pz / (zResolution - 1f) );

                        particlePositions[pointer * 3 + 0] = tx;
                        particlePositions[pointer * 3 + 1] = ty;
                        particlePositions[pointer * 3 + 2] = tz;

                        state.SetPos( pointer++, tx, ty, tz );
                    }
                }
            }

            particleShape = GeometryGenerator.GenQuad(0.05f, 0.05f);
            
            _meshVBO_ID = GL.GenBuffer();
            _instanceVBO_ID = GL.GenBuffer();
            _vaoID = GL.GenVertexArray();
            
            float[] vertexData = particleShape.GenerateVertexData();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _meshVBO_ID);
            GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * sizeof(float), vertexData, BufferUsageHint.StaticDraw);

            GL.BindVertexArray(_vaoID);
            // vertex positions
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, particleShape.FloatsPerVertex * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            // vertex normals
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, particleShape.FloatsPerVertex * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            // instance positions
            GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVBO_ID);
            GL.BufferData(BufferTarget.ArrayBuffer, particlePositions.Length * sizeof(float), particlePositions, BufferUsageHint.StreamDraw);
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.VertexAttribDivisor(2, 1);
            GL.EnableVertexAttribArray(2);
        }
        
        public override PhysicsData GetState()
        {
            return new PhysicsData(0);
        }

        public override PhysicsData GetDerivative(PhysicsData state)
        {
            return new PhysicsData(0);
        }

        public override void UpdateState(PhysicsData change, List<PhysicsBody> bodies)
        {
        }

        
        public override float MaxX() { return x + xLength; }
        public override float MinX() { return x; }
        public override float MaxY() { return y + yLength; }
        public override float MinY() { return y; }
        public override float MaxZ() { return z + zLength; }
        public override float MinZ() { return z; }

        public override Mesh GetMesh()
        {
            return null;
        }

        public override void Draw()
        {
            GL.BindVertexArray(_vaoID);
            GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, particleShape.TriangleIDs.Count * 3, state.ParticleCount);
        }
    }
}