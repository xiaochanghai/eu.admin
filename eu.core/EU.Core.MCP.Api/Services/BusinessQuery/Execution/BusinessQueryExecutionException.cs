namespace EU.Core.Api.MCP.Services.BusinessQuery.Execution;

public static class BusinessQueryExecutionErrorCodes
{
    public const string DescriptorMismatch = "BUSINESS_QUERY_DATA_SOURCE_MISMATCH";
    public const string ReadOnlyRequired = "BUSINESS_QUERY_READ_ONLY_REQUIRED";
    public const string CommandRejected = "BUSINESS_QUERY_COMMAND_REJECTED";
    public const string Timeout = "BUSINESS_QUERY_TIMEOUT";
    public const string Cancelled = "BUSINESS_QUERY_CANCELLED";
    public const string ResultLimitExceeded = "BUSINESS_QUERY_RESULT_LIMIT_EXCEEDED";
    public const string TieResultLimitExceeded = "BUSINESS_QUERY_TIE_RESULT_LIMIT_EXCEEDED";
    public const string ResultInvalid = "BUSINESS_QUERY_RESULT_INVALID";
    public const string ExecutionFailed = "BUSINESS_QUERY_EXECUTION_FAILED";
}

public sealed class BusinessQueryExecutionException : Exception
{
    public BusinessQueryExecutionException(string code)
        : base("The business query could not be executed.")
    {
        Code = code;
    }

    public string Code { get; }
}
