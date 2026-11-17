namespace ConvictionCore.Infrastructure.Ibkr.Flex;

/// <summary>
/// A sync step failed. <see cref="Exception.Message"/> is already redacted and safe to persist.
/// </summary>
internal sealed class FlexServiceException : Exception
{
    public FlexServiceException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public FlexServiceException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
