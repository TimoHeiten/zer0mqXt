using System.Text;

namespace heitech.zer0mqXt.core.Adapters
{
    public interface ISerializerAdapter
    {
        byte[] Serialize<T>(T @object) where T : class;
        T Deserialize<T>(byte[] payload) where T : class;

        Encoding Encoding { get; }
    }
}