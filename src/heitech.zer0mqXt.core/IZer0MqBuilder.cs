using System;
using heitech.zer0mqXt.core.Adapters;
using heitech.zer0mqXt.core.infrastructure;

namespace heitech.zer0mqXt.core
{
    ///<summary>
    /// <inheritdoc/>
    /// Build an ISocket for each of your configurations. Before you call build make sure to specify your services like Serializer and Logger
    /// Those will then be utilized for every Instance of ISocket afterwards
    ///</summary>
    public interface IZer0MqBuilder
    {
        //todo create interface to replace services as soon as an injector is decided
        ///<summary>
        /// Enable the developer mode to see remote stacktraces / exceptions for debugging purposes.
        ///</summary>
        IZer0MqBuilder EnableDeveloperMode();
        ///<summary>
        /// Set the logger
        ///</summary>
        IZer0MqBuilder SetLogger(ILogger adapter);
        ///<summary>
        /// Optionally disable Logger output for this Socket Configuration
        ///</summary>
        IZer0MqBuilder SilenceLogger();
        ///<summary>
        /// Set amount of retries for retryable Patterns like (RqRp). Default is 1
        ///</summary>
        IZer0MqBuilder SetRetryCount(uint counter);

        ///<summary>
        /// Disable any retries on this configuration
        ///</summary>
        IZer0MqBuilder DisableRetry();
        ///<summary>
        /// Introduce a ISerializeAdapter, maybe from your choosing
        ///</summary>
        IZer0MqBuilder SetSerializer(ISerializerAdapter adapter);

        ///<summary>
        /// Use the Newtonsoft.JSON library for Message Serialization. Default is the System.Text.JSON Serializer
        ///</summary>
        IZer0MqBuilder UseNewtonsoftJson();

        ///<summary>
        /// Set the Timeout for Retry of Operations to your choosing
        ///</summary>
        IZer0MqBuilder SetTimeOut(long timeOutInMs);
        ///<summary>
        /// Set the Timeout for Retry of Operations to your choosing
        ///</summary>
        IZer0MqBuilder SetTimeOut(TimeSpan timeOut);

        IPatternFactory BuildWithTcp(string host, string port);
        IPatternFactory BuildWithInProc(string pipeName);
    }
}