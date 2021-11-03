
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Gepe3D
{
    public class Global
    {

        internal static KeyboardState keyboardState;
        internal static MouseState mouseState;
        internal static float Elapsed = 0;


        public static bool IsKeyDown(Keys key)
        {
            return keyboardState.IsKeyDown(key);
        }
        
        public static bool IsKeyPressed(Keys key)
        {
            return keyboardState.IsKeyPressed(key);
        }
        
        public static bool IsKeyReleased(Keys key)
        {
            return keyboardState.IsKeyReleased(key);
        }


        public static Vector2 MousePosition { get { return mouseState.Position; } }
        
        public static Vector2 MouseDelta { get { return mouseState.Delta; } }
    
        public static Vector2 MouseScrollDelta { get { return mouseState.ScrollDelta; } }
        
        public static bool IsButtonDown(MouseButton button)
        {
            return mouseState.IsButtonDown(button);
        }
        
        

    }
}