using EU.Core.Agent.Application.MainAgent;
using EU.Core.Agent.Application.Agents;
using EU.Core.Model.ViewModels.Extend;

#nullable enable

namespace EU.Core.Services;

public sealed class MainAgentAssignmentService(
    IAgentDefinitionCatalog agents,
    IMainAgentAssignmentRepository assignments)
{
    private readonly IAgentDefinitionCatalog _agents = agents ?? throw new ArgumentNullException(nameof(agents));
    private readonly IMainAgentAssignmentRepository _assignments = assignments ?? throw new ArgumentNullException(nameof(assignments));

    public async Task<MainAgentOperationResult> GetAsync(CancellationToken cancellationToken = default)
    {
        MainAgentAssignment? assignment = await _assignments.GetAsync(cancellationToken);
        if (assignment is null)
        {
            return MainAgentOperationResult.Failure(
                MainAgentErrorCodes.NotConfigured,
                "No Main Agent is configured.");
        }

        AgentDefinition? agent = await _agents.GetDefinitionAsync(assignment.AgentId, cancellationToken);
        if (agent is null)
        {
            return MainAgentOperationResult.Failure(
                MainAgentErrorCodes.AgentNotFound,
                "The configured Main Agent was not found.");
        }

        if (agent.RuntimeStatus != AgentRuntimeStatus.Enabled)
        {
            return MainAgentOperationResult.Failure(
                MainAgentErrorCodes.AgentDisabled,
                "The configured Main Agent is disabled.");
        }

        AgentVersion? pinnedVersion = agent.PublishedVersions.SingleOrDefault(
            version => version.Id == assignment.AgentVersionId);
        if (pinnedVersion?.Snapshot is null)
        {
            return MainAgentOperationResult.Failure(
                MainAgentErrorCodes.VersionMissing,
                "The configured Main Agent version is unavailable.");
        }

        return MainAgentOperationResult.Success(assignment);
    }

    public async Task<MainAgentOperationResult> SetAsync(
        SetMainAgentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        AgentDefinition? agent = await _agents.GetDefinitionAsync(command.AgentId, cancellationToken);
        if (agent is null)
        {
            return MainAgentOperationResult.Failure(
                MainAgentErrorCodes.AgentNotFound,
                "The selected Main Agent was not found.");
        }

        if (agent.RuntimeStatus != AgentRuntimeStatus.Enabled)
        {
            return MainAgentOperationResult.Failure(
                MainAgentErrorCodes.AgentDisabled,
                "The selected Main Agent is disabled.");
        }

        AgentVersion? currentPublished = agent.PublishedVersions.Count == 0
            ? null
            : agent.PublishedVersions[^1];
        if (currentPublished?.Snapshot is null)
        {
            return MainAgentOperationResult.Failure(
                MainAgentErrorCodes.VersionMissing,
                "The selected Main Agent has no published snapshot.");
        }

        if (command.ExpectedLogicalRevision == long.MaxValue)
        {
            return MainAgentOperationResult.Failure(
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
            ? MainAgentOperationResult.Success(assignment)
            : MainAgentOperationResult.Failure(
                MainAgentErrorCodes.RowVersionConflict,
                "The Main Agent assignment was changed by another request.");
    }
}
