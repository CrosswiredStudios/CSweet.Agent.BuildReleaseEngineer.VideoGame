using CrosswiredStudios.VideoGame.AgentKit;
using CSweet.Agent.SDK;
using Microsoft.Extensions.AI;

namespace CSweet.Agent.BuildReleaseEngineer.VideoGame;

public sealed class SpecialistAgent : VideoGameSpecialistAgentBase
{
    internal const int DefaultContextWindowTokens = 128_000;
    internal const int DefaultOutputTokens = 16_000;
    private const int MinimumOutputTokens = 1_000;
    public override string AgentId => "com.csweet.video-game-build-release-engineer";
    public override string Version => "2.3.2";
    protected override AgentConfigurationBuilder Configure(AgentConfigurationBuilder builder) =>
        base.Configure(builder)
            .Number("maxContextWindowTokens", "Maximum context-window tokens", required: true,
                description: "Planning ceiling for Build Release Engineer model requests; set this no higher than the selected model's real context window.",
                minimum: 16_000, step: 1_000,
                defaultValue: DefaultContextWindowTokens)
            .Number("maxOutputTokens", "Maximum output tokens", required: true,
                description: "Budget for each Build Release Engineer model response, including reasoning. Set this within the selected model and provider's supported limits.",
                minimum: MinimumOutputTokens, step: 1_000,
                defaultValue: DefaultOutputTokens,
                lessThanFieldKey: "maxContextWindowTokens");

    protected override ChatOptions? ResponseOptions() =>
        new() { MaxOutputTokens = ResolveOutputTokens(Settings) };

    internal static int ResolveOutputTokens(AgentSettings settings)
    {
        var contextWindow = Math.Max(settings.GetInt32("maxContextWindowTokens", DefaultContextWindowTokens),
            MinimumOutputTokens + 1);
        var output = Math.Max(settings.GetInt32("maxOutputTokens", DefaultOutputTokens),
            MinimumOutputTokens);
        return Math.Min(output, contextWindow - 1);
    }
    protected override string RoleKey => "game-build-release-engineer";
    protected override string ArtifactTypeKey => "video-game.release-plan.v1";
    protected override string RolePrompt => "Own CI and build configuration, certified adapter operations, packaging, provenance, release-readiness evidence, and human-gated publication proposals. Reject version drift and irreproducible output.";
    protected override IReadOnlyList<string> RequiredSections =>
        ["Build Configuration", "Certified Recipe", "Targets", "Packaging", "Provenance", "Release Readiness", "Publication Approval"];
}
