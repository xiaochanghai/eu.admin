namespace EU.Core.Api.MCP.Services.BusinessQuery.Compilation;

public static class BusinessQueryCompilationErrorCodes
{
    public const string PolicyRequired = "BUSINESS_QUERY_POLICY_REQUIRED";
    public const string PolicyMismatch = "BUSINESS_QUERY_POLICY_MISMATCH";
    public const string DialectUnsupported = "BUSINESS_QUERY_DIALECT_UNSUPPORTED";
    public const string CatalogInvalid = "BUSINESS_QUERY_CATALOG_INVALID";
    public const string FieldInvalid = "BUSINESS_QUERY_FIELD_INVALID";
    public const string JoinUnsafe = "BUSINESS_QUERY_JOIN_UNSAFE";
    public const string ParameterLimitExceeded = "BUSINESS_QUERY_PARAMETER_LIMIT_EXCEEDED";
}

public sealed class BusinessQueryCompilationException : Exception
{
    public BusinessQueryCompilationException(string code)
        : base("The business query could not be compiled.")
    {
        Code = code;
    }

    public string Code { get; }
}
