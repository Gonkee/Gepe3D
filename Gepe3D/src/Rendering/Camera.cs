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

        private float smoothCamYaw;
        private float smoothCamPitch;
        private float smoothCamFilterX;
        private float smoothCamFilterY;
        private float smoothCamPartialTicks;
        
        private int smoothnessGrade = 3;

        public void Update(float delta)
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

            Position += movement * MovementSpeed * delta;

            if (Global.IsKeyDown(Keys.C))
            {

                float f = Sensitivity / smoothnessGrade * 0.6F + 0.2F;
                float f1 = f * f * f * 8.0F;
                this.smoothCamFilterX = smoothX(this.smoothCamYaw * 2, 0.05F * f1);
                this.smoothCamFilterY = smoothY(this.smoothCamPitch * 2, 0.05F * f1);
                this.smoothCamPartialTicks = 0.0F;
                this.smoothCamYaw = 0.0F;
                this.smoothCamPitch = 0.0F;

            }
            else
            {
                resetX();
                resetY();
            }

            UpdateLocalVectors();

        }

        private float targetValueX;
        private float remainingValueX;
        private float lastAmountX;

        private float targetValueY;
        private float remainingValueY;
        private float lastAmountY;

        /**
         * Smooths mouse input
         */
        public float smoothX(float f1, float f2)
        {
            this.targetValueX += f1;
            f1 = (this.targetValueX - this.remainingValueX) * f2;
            this.lastAmountX += (f1 - this.lastAmountX) * 0.5F;

            if (f1 > 0.0F && f1 > this.lastAmountX || f1 < 0.0F && f1 < this.lastAmountX)
            {
                f1 = this.lastAmountX;
            }

            this.remainingValueX += f1;
            return f1;
        }

        public void resetX()
        {
            this.targetValueX = 0.0F;
            this.remainingValueX = 0.0F;
            this.lastAmountX = 0.0F;
        }

        /**
        * Smooths mouse input
        */
        public float smoothY(float f1, float f2)
        {
            this.targetValueY += f1;
            f1 = (this.targetValueY - this.remainingValueY) * f2;
            this.lastAmountY += (f1 - this.lastAmountY) * 0.5F;

            if (f1 > 0.0F && f1 > this.lastAmountY || f1 < 0.0F && f1 < this.lastAmountY)
            {
                f1 = this.lastAmountY;
            }

            this.remainingValueY += f1;
            return f1;
        }

        public void resetY()
        {
            this.targetValueY = 0.0F;
            this.remainingValueY = 0.0F;
            this.lastAmountY = 0.0F;
        }

        public void MouseInput(Vector2 mouseDelta)
        {

            if (Global.IsKeyDown(Keys.C))
            {
                float f = Sensitivity / smoothnessGrade * 0.6F + 0.2F;
                float f1 = f * f * f * 8.0F;
                float f2 = mouseDelta.X * f1;
                float f3 = mouseDelta.Y * f1;
                int i = 1;

                this.smoothCamYaw += f2;
                this.smoothCamPitch += f3;
                float f4 = this.smoothCamPartialTicks - 0.1f;
                this.smoothCamPartialTicks = f4;
                f2 = this.smoothCamFilterX * f4;
                f3 = this.smoothCamFilterY * f4;

                yaw -= f2;
                pitch += f3 * (float)i;

                return;
            }

            yaw += mouseDelta.X * Sensitivity;
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
