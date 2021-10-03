
using Gepe3D.Entities;
using Gepe3D.Util;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Gepe3D.Core
{
    public class SkyBox
    {

        private class SkyBoxMesh
        {
            private static readonly float SIDE_LENGTH = 200;

            private readonly List<Vector3 > vertices  = new List<Vector3 >();
            private readonly List<Vector3i> triangles = new List<Vector3i>();

            private readonly int floatsPerVertex = 6;
            private float[] _vertexData;
            private bool _dataDirty = false;
            
            private int _vboID;
            private int _vaoID;

            public int AddVertex(float x, float y, float z)
            {
                int id = vertices.Count;
                vertices.Add(new Vector3(x, y, z));
                return id;
            }

            public void AddTriangle(int v1_ID, int v2_ID, int v3_ID)
            {
                triangles.Add(new Vector3i(v1_ID, v2_ID, v3_ID));
                _dataDirty = true;
            }

            public void GenerateVertexData()
            {
                _vertexData = new float[triangles.Count * 3 * floatsPerVertex];

                int pointer = 0;
                Vector3 v1, v2, v3, normal;
                foreach (Vector3i tri in triangles)
                {
                    v1 = vertices[tri.X];
                    v2 = vertices[tri.Y];
                    v3 = vertices[tri.Z];

                    normal = Vector3.Cross( v2 - v1, v3 - v1 ).Normalized();

                    _vertexData[pointer++] = v1.X;
                    _vertexData[pointer++] = v1.Y;
                    _vertexData[pointer++] = v1.Z;
                    _vertexData[pointer++] = normal.X;
                    _vertexData[pointer++] = normal.Y;
                    _vertexData[pointer++] = normal.Z;

                    _vertexData[pointer++] = v2.X;
                    _vertexData[pointer++] = v2.Y;
                    _vertexData[pointer++] = v2.Z;
                    _vertexData[pointer++] = normal.X;
                    _vertexData[pointer++] = normal.Y;
                    _vertexData[pointer++] = normal.Z;

                    _vertexData[pointer++] = v3.X;
                    _vertexData[pointer++] = v3.Y;
                    _vertexData[pointer++] = v3.Z;
                    _vertexData[pointer++] = normal.X;
                    _vertexData[pointer++] = normal.Y;
                    _vertexData[pointer++] = normal.Z;
                }
            }

            private void SendToGPU()
            {
                GenerateVertexData();
                GL.BindBuffer(BufferTarget.ArrayBuffer, _vboID);
                GL.BufferData(BufferTarget.ArrayBuffer, _vertexData.Length * sizeof(float), _vertexData, BufferUsageHint.StaticDraw);

                GL.BindVertexArray(_vaoID);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, floatsPerVertex * sizeof(float), 0);
                GL.EnableVertexAttribArray(0);
                GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, floatsPerVertex * sizeof(float), 3 * sizeof(float));
                GL.EnableVertexAttribArray(1);

                _dataDirty = false;
            }
            public SkyBoxMesh()
            {
                _vboID = GL.GenBuffer();
                _vaoID = GL.GenVertexArray();

                // first letter = b or t (bottom/top)
                // second letter = l or r (left/right)
                // third letter = f or b (front/back)
                int blf, blb, brb, brf, tlf, tlb, trb, trf;
                
                blf = AddVertex(-SIDE_LENGTH / 2, -SIDE_LENGTH / 2, -SIDE_LENGTH / 2);
                blb = AddVertex(-SIDE_LENGTH / 2, -SIDE_LENGTH / 2,  SIDE_LENGTH / 2);
                brb = AddVertex( SIDE_LENGTH / 2, -SIDE_LENGTH / 2,  SIDE_LENGTH / 2);
                brf = AddVertex( SIDE_LENGTH / 2, -SIDE_LENGTH / 2, -SIDE_LENGTH / 2);
                tlf = AddVertex(-SIDE_LENGTH / 2,  SIDE_LENGTH / 2, -SIDE_LENGTH / 2);
                tlb = AddVertex(-SIDE_LENGTH / 2,  SIDE_LENGTH / 2,  SIDE_LENGTH / 2);
                trb = AddVertex( SIDE_LENGTH / 2,  SIDE_LENGTH / 2,  SIDE_LENGTH / 2);
                trf = AddVertex( SIDE_LENGTH / 2,  SIDE_LENGTH / 2, -SIDE_LENGTH / 2);
                
                // counter clockwise specification, faces facing inward
                AddTriangle(blf, blb, brb);
                AddTriangle(blf, brb, brf);
                AddTriangle(blf, tlb, blb);
                AddTriangle(blf, tlf, tlb);
                AddTriangle(blb, trb, brb);
                AddTriangle(blb, tlb, trb);
                AddTriangle(brb, trf, brf);
                AddTriangle(brb, trb, trf);
                AddTriangle(brf, tlf, blf);
                AddTriangle(brf, trf, tlf);
                AddTriangle(tlf, trb, tlb);
                AddTriangle(tlf, trf, trb);
            }
            
            public void Draw(Shader shader)
            {
                if (_dataDirty) SendToGPU();
                GL.BindVertexArray(_vaoID);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                GL.DrawArrays(PrimitiveType.Triangles, 0, _vertexData.Length / floatsPerVertex);

            }
        }

        SkyBoxMesh skyBoxMesh;
        private Matrix4 transform;
        private bool _dirtyTransform = true;
        private float posX = 0, posY = 0, posZ = 0;
        private float sclX = 1, sclY = 1, sclZ = 1;
        private Quaternion rotation = Quaternion.Identity;

        public SkyBox()
        {
            skyBoxMesh = new SkyBoxMesh();
        }


        public void Render(Shader shader, Camera cam)
        {
            posX = cam.Position.X;
            posY = cam.Position.Y;
            posZ = cam.Position.Z;
            UpdateTransform();
            shader.SetMatrix4("modelMatrix", transform);
            skyBoxMesh.Draw(shader);
        }
        
        private void UpdateTransform()
        {
            // OpenTK matrices are transposed by default for some reason
            Matrix4 scaleMatrix = Matrix4.CreateScale(sclX, sclY, sclZ);
            scaleMatrix.Transpose();
            Matrix4 positionMatrix = Matrix4.CreateTranslation(posX, posY, posZ);
            positionMatrix.Transpose();
            Matrix4 rotationMatrix = Matrix4.CreateFromQuaternion(rotation);
            rotationMatrix.Transpose();

            transform = positionMatrix * rotationMatrix * scaleMatrix; // transformations go from right to left


            _dirtyTransform = false;
        }
    }
}