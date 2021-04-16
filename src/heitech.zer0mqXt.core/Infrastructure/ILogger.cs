
namespace heitech.zer0mqXt.core.infrastructure
{
    public interface ILogger
    {
        void Log(LogMessage message);
        void SetLogLevel(int level);
        void SetSilent();
    }

    ///<summary>
    /// For specific Logs create your own LogMessage
    ///</summary>
    public class LogMessage
    {
        public virtual int LogLevel { get; set; } = 1;
        public virtual string Msg { get; protected set; }

        public static implicit operator LogMessage(string message) => new LogMessage() { Msg = message };

        public override string ToString()
        {
            return Msg;
        }
    }
}
