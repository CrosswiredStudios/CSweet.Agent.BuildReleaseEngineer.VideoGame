using CSweet.VideoGame.AgentKit;

namespace CSweet.Agent.BuildReleaseEngineer.VideoGame;

public sealed class SpecialistAgent : VideoGameSpecialistAgentBase
{
    public override string AgentId => "com.csweet.video-game-build-release-engineer";
    public override string Version => "1.0.0";
    public override string PrimaryCapability => "video-game.build-release-engineer.execute.v1";
    protected override string RoleKey => "game-build-release-engineer";
    protected override string ArtifactTypeKey => "video-game.release-plan.v1";
    protected override string RolePrompt => "Own CI and build configuration, certified adapter operations, packaging, provenance, release-readiness evidence, and human-gated publication proposals. Reject version drift and irreproducible output.";
    protected override IReadOnlyList<string> RequiredSections =>
        ["Build Configuration", "Certified Recipe", "Targets", "Packaging", "Provenance", "Release Readiness", "Publication Approval"];
}
