using EU.Core.IServices.MainAgent;
using EU.Core.IServices.Agents;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.Model;

#nullable enable

namespace EU.Core.Services;

public sealed class MainAgentAssignmentService(
    IAgentDefinitionCatalog agents,
    IMainAgentAssignmentRepository assignments) : BaseServices
{
    private readonly IAgentDefinitionCatalog _agents = agents ?? throw new ArgumentNullException(nameof(agents));
    private readonly IMainAgentAssignmentRepository _assignments = assignments ?? throw new ArgumentNullException(nameof(assignments));

    public async Task<ServiceResult<MainAgentAssignment>> GetAsync(CancellationToken cancellationToken = default)
    {
        MainAgentAssignment? assignment = await _assignments.GetAsync(cancellationToken);
        if (assignment is null)
        {
            return Failure(
                MainAgentErrorCodes.NotConfigured,
                "No Main Agent is configured.");
        }

        AgentDefinition? agent = await _agents.GetDefinitionAsync(assignment.AgentId, cancellationToken);
        if (agent is null)
        {
            return Failure(
                MainAgentErrorCodes.AgentNotFound,
                "The configured Main Agent was not found.");
        }

        if (agent.RuntimeStatus != AgentRuntimeStatus.Enabled)
        {
            return Failure(
                MainAgentErrorCodes.AgentDisabled,
                "The configured Main Agent is disabled.");
        }

        AgentVersion? latestPublished = LatestPublishedVersion(agent);
        if (latestPublished?.Snapshot is null)
        {
            return Failure(
                MainAgentErrorCodes.VersionMissing,
                "The configured Main Agent has no published snapshot.");
        }

        // The assignment selects an Agent, while every new Unified Chat run uses
        // that Agent's latest published snapshot. Existing runs keep their own
        // version ID in the persisted run record for historical replay.
        return Success(assignment with { AgentVersionId = latestPublished.Id });
    }

    public async Task<ServiceResult<MainAgentAssignment>> SetAsync(
        SetMainAgentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        AgentDefinition? agent = await _agents.GetDefinitionAsync(command.AgentId, cancellationToken);
        if (agent is null)
        {
            return Failure(
                MainAgentErrorCodes.AgentNotFound,
                "The selected Main Agent was not found.");
        }

        if (agent.RuntimeStatus != AgentRuntimeStatus.Enabled)
        {
            return Failure(
                MainAgentErrorCodes.AgentDisabled,
                "The selected Main Agent is disabled.");
        }

        AgentVersion? currentPublished = LatestPublishedVersion(agent);
        if (currentPublished?.Snapshot is null)
        {
            return Failure(
                MainAgentErrorCodes.VersionMissing,
                "The selected Main Agent has no published snapshot.");
        }

        if (command.ExpectedLogicalRevision == long.MaxValue)
        {
            return Failure(
                MainAgentErrorCodes.RowVersionConflict,
                "The Main Agent assignment was changed by another request.");
        }

        var assignment = new MainAgentAssignment(
            agent.Id,
            currentPublished.Id,
            command.ExpectedLogicalRevision is null ? 0 : command.ExpectedLogicalRevision.Value + 1,
            DateTimeOffset.UtcNow);
        bool replaced = await _assignments.TryReplaceAsync(
            assignment,
            command.ExpectedLogicalRevision,
            cancellationToken);
        return replaced
            ? Success(assignment)
            : Failure(
                MainAgentErrorCodes.RowVersionConflict,
                "The Main Agent assignment was changed by another request.");
    }

    private static ServiceResult<MainAgentAssignment> Failure(string code, string message) =>
        ServiceResult<MainAgentAssignment>.Failure(
            MainAgentServiceStatusCodes.FromErrorCode(code),
            message);

    private static AgentVersion? LatestPublishedVersion(AgentDefinition agent) =>
        agent.PublishedVersions.Count == 0
            ? null
            : agent.PublishedVersions[^1];
}
