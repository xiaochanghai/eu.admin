namespace EU.Core.Api.MCP.Services.BusinessQuery.Policy;

public static class BusinessQueryPolicyErrorCodes
{
    public const string CallerInvalid = "BUSINESS_QUERY_CALLER_INVALID";
    public const string TenantMismatch = "BUSINESS_QUERY_TENANT_MISMATCH";
    public const string DataSourceUnauthorized = "BUSINESS_QUERY_DATA_SOURCE_UNAUTHORIZED";
    public const string EntityUnknown = "BUSINESS_QUERY_ENTITY_UNKNOWN";
    public const string PermissionDenied = "BUSINESS_QUERY_PERMISSION_DENIED";
    public const string FieldInvalid = "BUSINESS_QUERY_FIELD_INVALID";
    public const string ScopeConflict = "BUSINESS_QUERY_SCOPE_CONFLICT";
    public const string ScopeRequired = "BUSINESS_QUERY_SCOPE_REQUIRED";
    public const string TimeRangeRequired = "BUSINESS_QUERY_TIME_RANGE_REQUIRED";
    public const string TimeRangeInvalid = "BUSINESS_QUERY_TIME_RANGE_INVALID";
    public const string TimeRangeExceeded = "BUSINESS_QUERY_TIME_RANGE_EXCEEDED";
    public const string ResultLimitExceeded = "BUSINESS_QUERY_RESULT_LIMIT_EXCEEDED";
    public const string ContainsDenied = "BUSINESS_QUERY_CONTAINS_DENIED";
    public const string ComplexityExceeded = "BUSINESS_QUERY_COMPLEXITY_EXCEEDED";
    public const string QuotaExceeded = "BUSINESS_QUERY_QUOTA_EXCEEDED";
}
