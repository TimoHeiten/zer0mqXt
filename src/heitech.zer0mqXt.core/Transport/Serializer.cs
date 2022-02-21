using System.Text;
using System.Text.Json;
using Newtonsoft.Json;

namespace heitech.zer0mqXt.core
{
    public abstract class Serializer
    {
        internal Encoding Encoding { get; private set; }

        protected Serializer(Encoding encoding) => Encoding = encoding;
        public static Serializer UseNewtonsoft(Encoding encoding)
            => new NewtonJson(encoding);

        public static Serializer UseSystemText(Encoding encoding)
            => new SystemTextJson(encoding);

        public static Serializer UseUtf8Json(Encoding encoding)
            => new Utf8JsonSerializer(encoding);

        public abstract byte[] Serialize<T>(T value);
        public abstract T Deserialize<T>(byte[] bytes)
            where T : class;


        private sealed class SystemTextJson : Serializer
        {
             public SystemTextJson(Encoding encoding) 
                : base(encoding)
            { }

            public override T Deserialize<T>(byte[] bytes)
            {
                string json = Encoding.GetString(bytes);
                return System.Text.Json.JsonSerializer.Deserialize<T>(json);
            }

            public override byte[] Serialize<T>(T value)
            {
                string serialized = System.Text.Json.JsonSerializer.Serialize(value);
                return Encoding.GetBytes(serialized);
            }
        }

        private sealed class NewtonJson : Serializer
        {
            public NewtonJson(Encoding encoding) 
                : base(encoding)
            { }

            public override T Deserialize<T>(byte[] bytes)
            {
                string json = Encoding.GetString(bytes);

                return JsonConvert.DeserializeObject<T>(json);
            }

            public override byte[] Serialize<T>(T value)
            {
                string serialized = JsonConvert.SerializeObject(value);
                return Encoding.GetBytes(serialized);
            }
        }

        private sealed class Utf8JsonSerializer : Serializer
        {
            public Utf8JsonSerializer(Encoding encoding) 
                : base(encoding)
            { }

            public override T Deserialize<T>(byte[] bytes)
            {
                return Utf8Json.JsonSerializer.Deserialize<T>(bytes);
            }

            public override byte[] Serialize<T>(T value)
            {
                return Utf8Json.JsonSerializer.Serialize<T>(value);
            }
        }
    }
}