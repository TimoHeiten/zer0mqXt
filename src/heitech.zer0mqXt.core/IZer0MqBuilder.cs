using heitech.zer0mqXt.core.Adapters;
using heitech.zer0mqXt.core.infrastructure;

namespace heitech.zer0mqXt.core
{
    ///<summary>
    /// Build an IEntry for each of your configurations. Before you call build make sure to specify your services
    /// Those will then be utilized for every Instance of IEntry afterwards
    ///</summary>
    public interface IZer0MqBuilder
    {
        //todo create interface to replace services as soon as an injector is decided
        IZer0MqBuilder SetLogger(ILogger adapter);
        IZer0MqBuilder SilenceLogger();
        IZer0MqBuilder SetSerializer(ISerializerAdapter adapter);

        ISocket BuildWithTcp(string host, string port);
        ISocket BuildWithInProc(string pipeName);
    }
}