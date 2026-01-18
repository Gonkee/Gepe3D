using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Gepe3D
{
    public class Camera
    {

        public Vector3 Position { get; set; }
        public float AspectRatio { private get; set; }
        public float FovDegrees = 70;
        public float NearClip = 0.01f, FarClip = 500;
        public float MovementSpeed = 1.5f;
        public float Sensitivity = 0.2f;

        // initially points in the positive X
        private float pitch = 0;
        private float yaw = 0;

        private Vector3 _localForward = -Vector3.UnitZ;
        private Vector3 _localUp      =  Vector3.UnitY;
        private Vector3 _localRight   =  Vector3.UnitX;

        public Camera(Vector3 position, float aspectRatio)
        {
            Position = position;
            AspectRatio = aspectRatio;
        }

        public Matrix4 GetViewMatrix()
        {
            Matrix4 viewMatrix = Matrix4.LookAt(Position, Position + _localForward, _localUp);
            viewMatrix.Transpose();
            return viewMatrix;
        }
        
        public Matrix4 GetProjectionMatrix()
        {
            Matrix4 projMatrix = Matrix4.CreatePerspectiveFieldOfView( MathHelper.DegreesToRadians(FovDegrees), AspectRatio, NearClip, FarClip );
            projMatrix.Transpose();
            return projMatrix;
        }

        public Matrix4 GetMatrix()
        {
            // OpenTK matrices are transposed by default for some reason
            Matrix4 viewMatrix = Matrix4.LookAt(Position, Position + _localForward, _localUp);
            viewMatrix.Transpose();
            Matrix4 projMatrix = Matrix4.CreatePerspectiveFieldOfView( MathHelper.DegreesToRadians(FovDegrees), AspectRatio, NearClip, FarClip );
            projMatrix.Transpose();

            // transformations go from right to left
            return projMatrix * viewMatrix;
        }
        
        public void Update(float delta, KeyboardState keyboardState)
        {
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
            movement.X = rotatedMovement.X;
            movement.Y = rotatedMovement.Y;
            movement.Z = rotatedMovement.Z;

            Position += movement * MovementSpeed * delta;
            UpdateLocalVectors();
        }

        public void MouseInput(Vector2 mouseDelta)
        {
            yaw   += mouseDelta.X * Sensitivity;
            pitch -= mouseDelta.Y * Sensitivity;
            pitch = MathHelper.Clamp(pitch, -89.9f, 89.9f);
        }


        public void LookAt(float x, float y, float z)
        {
            float dx = x - Position.X;
            float dy = y - Position.Y;
            float dz = z - Position.Z;
            float horizontalDist = MathF.Sqrt(dx * dx + dz * dz);
            if (horizontalDist == 0) return;

            pitch = MathHelper.RadiansToDegrees( MathF.Atan2(dy, horizontalDist) );
            yaw =   MathHelper.RadiansToDegrees( MathF.Atan2(dz, dx) );
        }

        public void LookAt(Vector3 lookAt)
        {
            float dx = lookAt.X - Position.X;
            float dy = lookAt.Y - Position.Y;
            float dz = lookAt.Z - Position.Z;
            float horizontalDist = MathF.Sqrt(dx * dx + dz * dz);
            if (horizontalDist == 0) return;

            pitch = MathHelper.RadiansToDegrees( MathF.Atan2(dy, horizontalDist) );
            yaw =   MathHelper.RadiansToDegrees( MathF.Atan2(dz, dx) );
        }
        
        public void SetPos(Vector3 pos)
        {
            Position = pos;
        }

        public void UpdateLocalVectors()
        {
            _localForward.X = MathF.Cos( MathHelper.DegreesToRadians(pitch) ) * MathF.Cos( MathHelper.DegreesToRadians(yaw) );
            _localForward.Y = MathF.Sin( MathHelper.DegreesToRadians(pitch) );
            _localForward.Z = MathF.Cos( MathHelper.DegreesToRadians(pitch) ) * MathF.Sin( MathHelper.DegreesToRadians(yaw) );

            _localForward = Vector3.Normalize( _localForward );
            _localRight   = Vector3.Normalize( Vector3.Cross(_localForward, Vector3.UnitY) );
            _localUp      = Vector3.Normalize( Vector3.Cross(_localRight  , _localForward) );
        }

    }
}