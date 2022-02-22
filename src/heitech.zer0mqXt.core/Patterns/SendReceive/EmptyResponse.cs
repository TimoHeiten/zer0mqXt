///<summary>
/// Just needed to satisfy the way the underlying NetMQ RqRep works with regards to requiring to follow the protocol of Rq and subsequent Response
///</summary>
internal class EmptyResponse
{
    internal static EmptyResponse Value;
    static EmptyResponse()
    {
        Value = new EmptyResponse();
    }
}