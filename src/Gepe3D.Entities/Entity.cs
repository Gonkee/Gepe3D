
using System;
using Gepe3D.Core;
using Gepe3D.Util;
using OpenTK.Mathematics;

namespace Gepe3D.Entities
{
    public class Entity
    {

        private Matrix4 transform;
        private Matrix3 normalMatrix;
        private bool _dirtyTransform = true;
        private readonly Mesh _mesh;

        private float posX = 0, posY = 0, posZ = 0;
        private float sclX = 1, sclY = 1, sclZ = 1;
        private Quaternion rotation = Quaternion.Identity;

        public float PosX { get { return posX; } set { posX = value; _dirtyTransform = true; } }
        public float PosY { get { return posY; } set { posY = value; _dirtyTransform = true; } }
        public float PosZ { get { return posZ; } set { posZ = value; _dirtyTransform = true; } }
        public float SclX { get { return sclX; } set { sclX = value; _dirtyTransform = true; } }
        public float SclY { get { return sclY; } set { sclY = value; _dirtyTransform = true; } }
        public float SclZ { get { return sclZ; } set { sclZ = value; _dirtyTransform = true; } }


        public Entity (Mesh mesh)
        {
            this._mesh = mesh;
        }

        public void SetPosition(float x, float y, float z)
        {
            posX = x;
            posY = y;
            posZ = z;
            _dirtyTransform = true;
        }
        
        public void SetScale(float x, float y, float z)
        {
            sclX = x;
            sclY = y;
            sclZ = z;
            _dirtyTransform = true;
        }
        
        public void SetScale(float scale)
        {
            sclX = scale;
            sclY = scale;
            sclZ = scale;
            _dirtyTransform = true;
        }

        public void ApplyRotation (Vector3 axis, float rotationDegrees)
        {
            float rotRad = MathHelper.DegreesToRadians(rotationDegrees);
            float sin = MathF.Sin(rotRad / 2);
            float x = axis.X * sin;
            float y = axis.Y * sin;
            float z = axis.Z * sin;
            float w = MathF.Cos(rotRad / 2);
            
            // right to left seems to be the right order in this case (for conversion to matrix)
            rotation = Quaternion.Multiply( new Quaternion(x, y, z, w), rotation );
            _dirtyTransform = true;
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

            normalMatrix = new Matrix3( Matrix4.Transpose( Matrix4.Invert(transform) ) );

            _dirtyTransform = false;
        }

        public virtual void Update()
        {
            
        }

        public virtual void Render(Shader shader)
        {
            if (_dirtyTransform) UpdateTransform();
            shader.SetMatrix4("modelMatrix", transform);
            shader.SetMatrix3("normalMatrix", normalMatrix);
            if (_mesh != null) _mesh.Draw(shader);
        }
    }
}