namespace EU.Core.Api.MCP.Services.BusinessQuery.Errors;

public static class BusinessQueryErrorCodes
{
    public const string PlanRequired = "BUSINESS_QUERY_PLAN_REQUIRED";
    public const string PlanTooLarge = "BUSINESS_QUERY_PLAN_TOO_LARGE";
    public const string PlanInvalidJson = "BUSINESS_QUERY_PLAN_INVALID_JSON";
    public const string PlanUnknownProperty = "BUSINESS_QUERY_PLAN_UNKNOWN_PROPERTY";
    public const string PlanDuplicateProperty = "BUSINESS_QUERY_PLAN_DUPLICATE_PROPERTY";
    public const string PlanInvalid = "BUSINESS_QUERY_PLAN_INVALID";
    public const string PlanLimitExceeded = "BUSINESS_QUERY_PLAN_LIMIT_EXCEEDED";
}
