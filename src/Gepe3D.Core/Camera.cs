using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Gepe3D.Core
{
    public class Camera
    {

        public Vector3 Position { get; set; }
        public float AspectRatio { private get; set; }
        public float FovDegrees = 70;
        public float NearClip = 0.01f, FarClip = 100;
        public float MovementSpeed = 1.5f;
        public float Sensitivity = 0.2f;

        private float pitch;
        private float yaw;

        private Vector3 _localForward = -Vector3.UnitZ;
        private Vector3 _localUp      =  Vector3.UnitY;
        private Vector3 _localRight   =  Vector3.UnitX;

        public Camera(Vector3 position, float aspectRatio)
        {
            Position = position;
            AspectRatio = aspectRatio;
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
        
        public void Update()
        {
            Vector3 movement = new Vector3();
            if ( Global.IsKeyDown(Keys.W)         ) { movement.X += 1; }
            if ( Global.IsKeyDown(Keys.S)         ) { movement.X -= 1; }
            if ( Global.IsKeyDown(Keys.A)         ) { movement.Z -= 1; }
            if ( Global.IsKeyDown(Keys.D)         ) { movement.Z += 1; }
            if ( Global.IsKeyDown(Keys.Space)     ) { movement.Y += 1; }
            if ( Global.IsKeyDown(Keys.LeftShift) ) { movement.Y -= 1; }
            if (movement.Length != 0) movement.Normalize();

            Matrix4 rotation = Matrix4.CreateRotationY( MathHelper.DegreesToRadians(-yaw) );
            rotation.Transpose(); // OpenTK matrices are transposed by default for some reason

            Vector4 rotatedMovement = rotation * new Vector4(movement, 1);
            movement.X = rotatedMovement.X;
            movement.Y = rotatedMovement.Y;
            movement.Z = rotatedMovement.Z;

            Position += movement * MovementSpeed * Global.Delta;

            yaw   += Global.MouseDelta.X * Sensitivity;
            pitch -= Global.MouseDelta.Y * Sensitivity;
            pitch = MathHelper.Clamp(pitch, -89.9f, 89.9f);
            UpdateLocalVectors();
        }

        private void UpdateLocalVectors()
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