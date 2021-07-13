using heitech.zer0mqXt.core.Adapters;
using heitech.zer0mqXt.core.infrastructure;

namespace heitech.zer0mqXt.core
{
    ///<summary>
    /// Build an ISocket for each of your configurations. Before you call build make sure to specify your services like Serializer and Logger
    /// Those will then be utilized for every Instance of ISocket afterwards
    ///</summary>
    public interface IZer0MqBuilder
    {
        //todo create interface to replace services as soon as an injector is decided
        IZer0MqBuilder SetLogger(ILogger adapter);
        IZer0MqBuilder SilenceLogger();
        IZer0MqBuilder SetSerializer(ISerializerAdapter adapter);

        ///<summary>
        /// Important to prime a socket as a publishing one. Can only be used for one Publisher at a time
        ///</summary>
        IZer0MqBuilder UsePublisher();

        ISocket BuildWithTcp(string host, string port);
        ISocket BuildWithInProc(string pipeName);
    }
}