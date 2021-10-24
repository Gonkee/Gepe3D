

namespace Gepe3D
{
    public abstract class Scene
    {
        
        protected readonly MainWindow window;
        
        public Scene(MainWindow window)
        {
            this.window = window;
        }
        
        public abstract void Update();
        
        public abstract void Render();
        
    }
}