using System.Text;

namespace heitech.zer0mqXt.core.Adapters
{
    ///<summary>
    /// Uses the default implementations of all services
    ///</summary>
    internal class InternalAdapter : ISerializerAdapter
    {
        private Serializer _serializer;
        public Encoding Encoding { get; } = Encoding.UTF8;

        internal InternalAdapter()
            => _serializer = Serializer.UseSystemText(Encoding);

        public T Deserialize<T>(byte[] payload) where T : class
            => _serializer.Deserialize<T>(payload);

        public byte[] Serialize<T>(T @object) where T : class
            => _serializer.Serialize<T>(@object);

        public void SetToNewtonsoft()
            => _serializer = Serializer.UseNewtonsoft(Encoding);
    }
}