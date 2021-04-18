using System.Text;
using heitech.zer0mqXt.core.infrastructure;

namespace heitech.zer0mqXt.core.Adapters
{
    ///<summary>
    /// Uses the default implementations of all services
    ///</summary>
    public class InternalAdapters : ISerializerAdapter
    {
        public Encoding Encoding { get; } = Encoding.UTF8;
        private readonly Serializer _serializer;
        private readonly BasicLogger _logger;

        public InternalAdapters()
        {
            _logger = new BasicLogger();
            _serializer = Serializer.UseNewtonsoft(Encoding);
        }

        public T Deserialize<T>(byte[] payload)
            where T : class
        {
            return _serializer.Deserialize<T>(payload);
        }

        public byte[] Serialize<T>(T @object)
            where T : class
        {
            return _serializer.Serialize<T>(@object);
        }
    }
}