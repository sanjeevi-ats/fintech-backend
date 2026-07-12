using System;

namespace Fintech.Core.Domain;

public class FinVedaException : Exception
{
    public int StatusCode { get; }
    public string ErrorCode { get; }

    public FinVedaException(int statusCode, string errorCode, string message) : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}
