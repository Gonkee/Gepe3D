using System;
using OpenTK.Graphics.OpenGL4;

namespace Gepe3D.Entities
{
    public class Quad : Entity
    {

        float[] _vertices = new float[4 * 3];
        int[] _indices = new int[] {
            0, 1, 2,
            0, 3, 2
        };

        int _vboID;
        int _vaoID;


        public Quad(float x, float y, float width, float height)
        {
            _vertices[ 0] = x;
            _vertices[ 1] = y;
            _vertices[ 2] = 0;

            _vertices[ 3] = x;
            _vertices[ 4] = y + height;
            _vertices[ 5] = 0;

            _vertices[ 6] = x + width;
            _vertices[ 7] = y + height;
            _vertices[ 8] = 0;

            _vertices[ 9] = x + width;
            _vertices[10] = y;
            _vertices[11] = 0;
            
            _vboID = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboID);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

            _vaoID = GL.GenVertexArray();
            GL.BindVertexArray(_vaoID);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
        }

        public override void Render()
        {

        }

        public override void Update()
        {
            System.Console.WriteLine();
        }
    }
}