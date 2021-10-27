

namespace Gepe3D
{
    public class SkyBox : Renderable
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

        private int _vboID, _vaoID;
        

        public SkyBox()
        {
            _vaoID = GLUtils.GenVAO();
            _vboID = GLUtils.GenVBO(vertices);
            GLUtils.VaoFloatAttrib(_vaoID, _vboID, 0, 3, 3, 0);
            GLUtils.AttachEBO(_vaoID, indices);
            
        }

        
        public void Render(Renderer renderer)
        {
            Shader shader = renderer.UseShader("skybox");
            shader.SetVector3("cameraPos", renderer.CameraPos);
            shader.SetMatrix4("cameraMatrix", renderer.CameraMatrix);
            
            GLUtils.DrawIndexedVAO(_vaoID, indices.Length);
        }
        
    }
}