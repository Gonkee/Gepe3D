
using OpenTK.Graphics.OpenGL4;

namespace Gepe3D
{
    public class SkyBox
    {

        private static readonly float SIDE_LENGTH = 200;
        
        private readonly float[] vertices = new float[]
        {
            -SIDE_LENGTH / 2, -SIDE_LENGTH / 2, -SIDE_LENGTH / 2,
            -SIDE_LENGTH / 2, -SIDE_LENGTH / 2,  SIDE_LENGTH / 2,
             SIDE_LENGTH / 2, -SIDE_LENGTH / 2,  SIDE_LENGTH / 2,
             SIDE_LENGTH / 2, -SIDE_LENGTH / 2, -SIDE_LENGTH / 2,
            -SIDE_LENGTH / 2,  SIDE_LENGTH / 2, -SIDE_LENGTH / 2,
            -SIDE_LENGTH / 2,  SIDE_LENGTH / 2,  SIDE_LENGTH / 2,
             SIDE_LENGTH / 2,  SIDE_LENGTH / 2,  SIDE_LENGTH / 2,
             SIDE_LENGTH / 2,  SIDE_LENGTH / 2, -SIDE_LENGTH / 2
        };
        
        // counter clockwise specification, faces facing inward
        private readonly uint[] indices = new uint[]
        {
            0, 1, 2,    0, 2, 3,    0, 5, 1,    0, 4, 5,
            1, 6, 2,    1, 5, 6,    2, 7, 3,    2, 6, 7,
            3, 4, 0,    3, 7, 4,    4, 6, 5,    4, 7, 6
        };

        private int _vboID, _vaoID, _eboID;
        
        private Shader _skyboxShader;

        public SkyBox()
        {
            _vboID = GL.GenBuffer();
            _vaoID = GL.GenVertexArray();
            _eboID = GL.GenBuffer();
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboID);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.BindVertexArray(_vaoID);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _eboID);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            
            _skyboxShader = new Shader("res/Shaders/skybox.vert", "res/Shaders/skybox.frag");
        }

        public void Draw(Camera camera)
        {
            _skyboxShader.Use();
            _skyboxShader.SetVector3("cameraPos", camera.Position);
            _skyboxShader.SetMatrix4("cameraMatrix", camera.GetMatrix());
            // _skyboxShader.SetMatrix4("viewMatrix", camera.GetViewMatrix());
            // _skyboxShader.SetMatrix4("projectionMatrix", camera.GetProjectionMatrix());
            
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            GL.BindVertexArray(_vaoID);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);
        }
        
    }
}