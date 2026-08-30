using EU.Core.IServices.Evaluation;
using EU.Core.IServices.Orchestration;
using EU.Core.IServices.Skills;
using EU.Core.Api.Agent.Controllers;
using EU.Core.Model;
using EU.Core.Model.Models;
using EU.Core.Model.ViewModels.Extend;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace EU.Core.Api.Agent.Configuration;

/// <summary>
/// 为 Agent API Action 生成统一响应包装及特殊协议的 OpenAPI 元数据。
/// </summary>
public sealed class AgentApiResponseMetadataConvention : IApplicationModelConvention
{
    private static readonly IReadOnlyDictionary<string, SpecialResponse> SpecialResponses =
        new Dictionary<string, SpecialResponse>(StringComparer.Ordinal)
        {
            ["AgentRunsController.Run"] = new("text/event-stream", typeof(void)),
            ["ChatRunsController.Start"] = new("text/event-stream", typeof(void)),
            ["AgentsController.Export"] = new("application/json", typeof(byte[])),
            ["SkillsController.ReadFile"] = new("text/plain", typeof(string)),
            ["MetricsController.Get"] = new("text/plain", typeof(string), IncludeDefaultError: false)
        };

    private static readonly IReadOnlyDictionary<string, ServiceResponse> ServiceResponses =
        new Dictionary<string, ServiceResponse>(StringComparer.Ordinal)
        {
            ["ChatRunsController.Cancel"] = Response<ChatRunCancelResponse>(StatusCodes.Status202Accepted),
            ["EvaluationSuitesController.Create"] = Response<EvaluationSuiteDefinition>(StatusCodes.Status201Created),
            ["OrchestrationsController.Create"] = Response<OrchestrationDefinition>(StatusCodes.Status201Created),
            ["OrchestrationsController.Start"] = Response<OrchestrationRunRecord>(StatusCodes.Status202Accepted),
            ["OrchestrationsController.Cancel"] = Response<OrchestrationRunCancelResponse>(StatusCodes.Status202Accepted),
            ["SkillsController.Create"] = Response<SkillDefinition>(StatusCodes.Status201Created)
        };

    public void Apply(ApplicationModel application)
    {
        ArgumentNullException.ThrowIfNull(application);
        foreach (ControllerModel controller in application.Controllers.Where(IsAgentController))
        {
            foreach (ActionModel action in controller.Actions)
            {
                string key = $"{controller.ControllerType.Name}.{action.ActionMethod.Name}";
                if (SpecialResponses.TryGetValue(key, out SpecialResponse? special))
                {
                    string[] additionalContentTypes = special.IncludeDefaultError
                        && !string.Equals(
                            special.ContentType,
                            "application/json",
                            StringComparison.Ordinal)
                            ? ["application/json"]
                            : [];
                    action.Filters.Add(new ProducesAttribute(
                        special.ContentType,
                        additionalContentTypes));
                    action.Filters.Add(new ProducesResponseTypeAttribute(
                        special.ResponseType,
                        StatusCodes.Status200OK,
                        special.ContentType));
                    if (special.IncludeDefaultError)
                        AddDefaultError(action);
                    continue;
                }

                ProducesResponseTypeAttribute? declaredResponse =
                    FindDeclaredServiceResponse(action);
                ServiceResponses.TryGetValue(key, out ServiceResponse? response);
                response ??= declaredResponse is null
                    ? null
                    : ToServiceResponse(declaredResponse);
                response ??= InferServiceResponse(action.ActionMethod.ReturnType);
                if (response is null)
                {
                    throw new InvalidOperationException(
                        $"Agent API Action '{key}' does not declare its ServiceResult response type.");
                }
                action.Filters.Add(new ProducesAttribute("application/json"));
                if (declaredResponse is null)
                {
                    action.Filters.Add(new ProducesResponseTypeAttribute(
                        typeof(ServiceResult<>).MakeGenericType(response.DataType),
                        response.HttpStatus,
                        "application/json"));
                }
                else if (!action.Filters.Contains(declaredResponse))
                {
                    action.Filters.Add(declaredResponse);
                }
                if (string.Equals(
                        key,
                        "OrchestrationsController.Output",
                        StringComparison.Ordinal))
                {
                    action.Filters.Add(new ProducesResponseTypeAttribute(
                        StatusCodes.Status204NoContent));
                }
                AddDefaultError(action);
            }
        }
    }

    public static IReadOnlyCollection<string> SpecialActionKeys =>
        SpecialResponses.Keys.ToArray();

    public static IReadOnlyDictionary<string, Type> ServiceActionDataTypes =>
        ServiceResponses.ToDictionary(item => item.Key, item => item.Value.DataType);

    private static bool IsAgentController(ControllerModel controller) =>
        string.Equals(
            controller.ControllerType.Namespace,
            "EU.Core.Api.Agent.Controllers",
            StringComparison.Ordinal);

    private static void AddDefaultError(ActionModel action) =>
        action.Filters.Add(new ProducesDefaultResponseTypeAttribute(
            typeof(ServiceResult<AgentApiErrorData>)));

    private static ServiceResponse? InferServiceResponse(Type returnType)
    {
        while (returnType.IsGenericType)
        {
            Type genericType = returnType.GetGenericTypeDefinition();
            if (genericType == typeof(Task<>)
                || genericType == typeof(ValueTask<>)
                || genericType == typeof(ActionResult<>))
            {
                returnType = returnType.GetGenericArguments()[0];
                continue;
            }

            break;
        }

        if (returnType.IsGenericType
            && returnType.GetGenericTypeDefinition() == typeof(ServiceResult<>))
        {
            return new ServiceResponse(
                returnType.GetGenericArguments()[0],
                StatusCodes.Status200OK);
        }

        return null;
    }

    private static ProducesResponseTypeAttribute? FindDeclaredServiceResponse(
        ActionModel action)
    {
        ProducesResponseTypeAttribute[] responses = action.Attributes
            .OfType<ProducesResponseTypeAttribute>()
            .Where(response =>
                response.StatusCode is >= 200 and < 300
                && response.Type?.IsGenericType == true
                && response.Type.GetGenericTypeDefinition() == typeof(ServiceResult<>))
            .ToArray();
        return responses.Length switch
        {
            0 => null,
            1 => responses[0],
            _ => throw new InvalidOperationException(
                $"Agent API Action '{action.ActionMethod.Name}' declares multiple successful ServiceResult responses.")
        };
    }

    private static ServiceResponse ToServiceResponse(
        ProducesResponseTypeAttribute response) => new(
        response.Type!.GetGenericArguments()[0],
        response.StatusCode);

    private static ServiceResponse Response<T>(
        int httpStatus = StatusCodes.Status200OK) => new(typeof(T), httpStatus);

    private sealed record ServiceResponse(Type DataType, int HttpStatus);

    private sealed record SpecialResponse(
        string ContentType,
        Type ResponseType,
        bool IncludeDefaultError = true);
}
