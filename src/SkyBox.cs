

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

        private int _vboID, _vaoID;
        
        
        private readonly Shader skyboxShader;

        public SkyBox()
        {
            _vaoID = GLUtils.GenVAO();
            _vboID = GLUtils.GenVBO(vertices);
            GLUtils.VaoFloatAttrib(_vaoID, _vboID, 0, 3, 3, 0);
            GLUtils.AttachEBO(_vaoID, indices);
            
            skyboxShader = new Shader("res/Shaders/skybox.vert", "res/Shaders/skybox.frag");
        }

        
        public void Render(MainWindow world)
        {
            skyboxShader.Use();
            skyboxShader.SetVector3("cameraPos", world.character.activeCam.Position);
            skyboxShader.SetMatrix4("cameraMatrix", world.character.activeCam.GetMatrix());
            
            GLUtils.DrawIndexedVAO(_vaoID, indices.Length);
        }
        
    }
}