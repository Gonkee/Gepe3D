


namespace Gepe3D.Physics
{
    public class PhysicsData
    {
        private readonly float[] _data;
        public readonly int DataLength;

        public PhysicsData(int dataLength)
        {
            this._data = new float[dataLength];
            DataLength =_data.Length;
        }

        public float Get(int id)
        {
            return _data[id];
        }

        public void Set(int id, float value)
        {
            _data[id] = value;
        }

    }
}