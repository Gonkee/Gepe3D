
using System;
using Gepe3D.Core;
using OpenTK.Mathematics;

namespace Gepe3D.Entities
{
    public class Entity
    {

        private bool _dirtyTransform = true;
        internal readonly Mesh Mesh;
        internal readonly Material Material;

        private float posX = 0, posY = 0, posZ = 0;
        private float sclX = 1, sclY = 1, sclZ = 1;
        private Quaternion rotation = Quaternion.Identity;

        public Matrix4 TransformMatrix { get; private set; }
        public Matrix3 NormalMatrix    { get; private set; }
        public bool DrawWireframe = false;

        public float PosX { get { return posX; } set { posX = value; _dirtyTransform = true; } }
        public float PosY { get { return posY; } set { posY = value; _dirtyTransform = true; } }
        public float PosZ { get { return posZ; } set { posZ = value; _dirtyTransform = true; } }
        public float SclX { get { return sclX; } set { sclX = value; _dirtyTransform = true; } }
        public float SclY { get { return sclY; } set { sclY = value; _dirtyTransform = true; } }
        public float SclZ { get { return sclZ; } set { sclZ = value; _dirtyTransform = true; } }


        public Entity (Mesh mesh, Material material)
        {
            this.Mesh = mesh;
            this.Material = material;
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

        internal void UpdateTransform()
        {
            if (!_dirtyTransform) return;

            // OpenTK matrices are transposed by default for some reason
            Matrix4 scaleMatrix = Matrix4.CreateScale(sclX, sclY, sclZ);
            scaleMatrix.Transpose();
            Matrix4 positionMatrix = Matrix4.CreateTranslation(posX, posY, posZ);
            positionMatrix.Transpose();
            Matrix4 rotationMatrix = Matrix4.CreateFromQuaternion(rotation);
            rotationMatrix.Transpose();

            TransformMatrix = positionMatrix * rotationMatrix * scaleMatrix; // transformations go from right to left
            NormalMatrix = new Matrix3( Matrix4.Transpose( Matrix4.Invert(TransformMatrix) ) );

            _dirtyTransform = false;
        }

    }
}