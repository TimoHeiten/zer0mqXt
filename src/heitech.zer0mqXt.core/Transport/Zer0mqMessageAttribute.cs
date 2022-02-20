using System;

namespace heitech.zer0mqXt.core.Transport
{
    ///<summary>
    /// Depicts a Message to be used with the Zer0mqXt library.
    /// <para>The MessageId is used to check for matching Messages (Make also sure that you can serialize/deserialize the matching messages)</para>
    ///</summary>
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class Zer0mqMessageAttribute : Attribute
    {
        ///<summary>
        /// The MessageId that is used to check for matching Messages (Make also sure that you can serialize/deserialize the matching messages)
        ///</summary>
        public string MessageId { get; }
        public Zer0mqMessageAttribute(string messageId)
            => MessageId = messageId;
    }
}