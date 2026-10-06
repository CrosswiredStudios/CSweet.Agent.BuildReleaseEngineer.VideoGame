using System.Text.Json;
using CSweet.Agent.SDK;
using CSweet.WorkManagement.Contracts;

namespace CSweet.Agent.BuildReleaseEngineer.VideoGame;

public sealed partial class SpecialistAgent
{
    protected override async Task<AgentWorkResult> ExecuteDeliveryScopeAsync(WorkExecutionAssignmentV2 assignment,
        AgentRuntimeContext context, CancellationToken ct)
    {
        try
        {
            if (assignment.Scope != WorkExecutionScopes.Release || assignment.StageKey != "build-readiness" ||
                assignment.DeliveryPlanId is not { } planId || assignment.Candidate is not { } candidate || candidate.Repositories.Count == 0)
                throw new InvalidOperationException("Build readiness requires the exact assigned release and complete repository candidate.");
            var plan = (await context.Platform.Work.ReadDeliveryPlansAsync(new(assignment.WorkstreamId, planId), ct)).Single();
            var execution = plan.Executions.Single(x => x.Id == assignment.ExecutionId);
            if (plan.Status != "Active" || plan.ScopeRevision != assignment.ScopeRevision || execution.Candidate?.Digest != candidate.Digest ||
                execution.Stages.Last().AgentInstallationId.ToString() != context.InstallationId)
                throw new UnauthorizedAccessException("The build assignment or candidate changed.");
            var builds = new List<DeliveryBuildV2>();
            foreach (var repository in candidate.Repositories)
            {
                var specification = plan.Branches.Single(x => x.Scope == WorkExecutionScopes.Release && x.RepositoryId == repository.RepositoryId).Build
                    ?? throw new InvalidOperationException("Ask the architect to configure the certified build recipe and packaging target.");
                builds.Add(await context.Platform.RequestBuildAsync(new(plan.WorkstreamId, null, specification.ToolchainDefinitionId,
                    specification.ProviderInstallationId, repository.RepositoryId, repository.CandidateCommitSha, specification.RecipeKey,
                    specification.TargetKey, specification.Configuration, 3, $"release-build:{execution.Id:N}:{repository.RepositoryId:N}:{candidate.Digest}"), ct));
            }
            if (builds.Any(x => x.Status is "Queued" or "Claimed" or "Running"))
                return AgentWorkResult.Success(new WorkExecutionOutcomeV1(assignment.StageExecutionId, assignment.AttemptId,
                    WorkExecutionDispositions.Completed, "awaiting-build", "Certified candidate builds are pending; completion events wake delivery recovery.",
                    JsonSerializer.SerializeToElement(new WorkDeliveryBuildPending(candidate.Digest, builds.Select(x => x.Id).ToArray()), new JsonSerializerOptions(JsonSerializerDefaults.Web)), [], []));
            if (builds.Any(x => x.Status != "Succeeded" || x.SourceRevision != candidate.Repositories.Single(r => r.RepositoryId == x.RepositoryId).CandidateCommitSha ||
                x.Outputs.Count == 0 || x.Provenance?.SourceRevision != x.SourceRevision))
                throw new InvalidOperationException("One or more release repositories lack successful candidate-bound packaging and provenance.");
            var scope = plan.Scopes.Single(x => x.Scope == WorkExecutionScopes.Release);
            var summary = "Certified provider builds produced exact-candidate packaging and provenance for every participating repository. Full release regression and technical acceptance must follow.";
            return DeliveryScopeReview.Outcome(assignment, new(candidate.Digest, true, summary,
                scope.Planning.AcceptanceCriteria.Select(c => new WorkDeliveryCriterionResult(c, true,
                    "Build readiness evidence only: " + string.Join("; ", builds.Select(b => $"{b.RepositoryId:D}: build {b.Id:D}, commit {b.SourceRevision}, manifest {b.Provenance!.NormalizedOutputManifestHash}")))).ToArray(), [])
                { BuildIds = builds.Select(x => x.Id).ToArray() });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error)
        { return AgentWorkResult.Success(new WorkExecutionOutcomeV1(assignment.StageExecutionId, assignment.AttemptId,
            WorkExecutionDispositions.Blocked, "blocked", error.Message, JsonSerializer.SerializeToElement(new { }), [], [error.Message])); }
    }
}
