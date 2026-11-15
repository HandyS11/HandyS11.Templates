using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.TemplateEngine.Authoring.TemplateVerifier;
using System.Text.RegularExpressions;

namespace HandyS11.Templates.Tests.Templates;

public partial class AspireTemplateTests
{
    private const string SourceUserSecretsId = "c7c079e4-191d-478f-8ed2-6559fc6cab16";
    private const string ScrubbedUserSecretsId = "00000000-0000-0000-0000-000000000000";

    private static readonly string TemplatePath =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "content", "aspire"));

    // --files-only skips the post-actions (network + solution lookup); fixed ports keep the snapshots stable.
    private static readonly string[] FixedArgs =
    [
        "--files-only",
        "--appHostHttpPort", "15001", "--appHostHttpsPort", "17001",
        "--appHostOtlpHttpPort", "19001", "--appHostOtlpHttpsPort", "21001",
        "--appHostResourceHttpPort", "20001", "--appHostResourceHttpsPort", "22001",
    ];

    [GeneratedRegex("<UserSecretsId>(?<id>[^<]*)</UserSecretsId>")]
    private static partial Regex UserSecretsIdRegex();

    [Fact]
    public async Task Aspire_DefaultInstantiationAsync()
    {
        List<string> userSecretsIds = [];
        TemplateVerifierOptions options = new TemplateVerifierOptions("handys11-aspire")
        {
            TemplatePath = TemplatePath,
            TemplateSpecificArgs = FixedArgs,
            DoNotAppendTemplateArgsToScenarioName = true,
            DisableDiffTool = true
        }.WithCustomScrubbers(UserSecretsIdScrubber(userSecretsIds));

        VerificationEngine engine = new(NullLogger.Instance);
        await engine.Execute(options);

        AssertUserSecretsIdRegenerated(userSecretsIds);
    }

    [Fact]
    public async Task Aspire_WithCustomNameAsync()
    {
        List<string> userSecretsIds = [];
        TemplateVerifierOptions options = new TemplateVerifierOptions("handys11-aspire")
        {
            TemplatePath = TemplatePath,
            TemplateSpecificArgs = ["--name", "MyProject", .. FixedArgs],
            DoNotAppendTemplateArgsToScenarioName = true,
            ScenarioName = "MyProject",
            DisableDiffTool = true
        }.WithCustomScrubbers(UserSecretsIdScrubber(userSecretsIds));

        VerificationEngine engine = new(NullLogger.Instance);
        await engine.Execute(options);

        AssertUserSecretsIdRegenerated(userSecretsIds);
    }

    // The UserSecretsId is a new GUID on every instantiation: record it, then replace it so the snapshot is stable.
    private static ScrubbersDefinition UserSecretsIdScrubber(List<string> captured) =>
        ScrubbersDefinition.Empty.AddScrubber(sb =>
        {
            string content = sb.ToString();
            string scrubbed = UserSecretsIdRegex().Replace(content, match =>
            {
                captured.Add(match.Groups["id"].Value);
                return $"<UserSecretsId>{ScrubbedUserSecretsId}</UserSecretsId>";
            });
            sb.Clear().Append(scrubbed);
        }, "csproj");

    private static void AssertUserSecretsIdRegenerated(List<string> captured)
    {
        string id = Assert.Single(captured);
        Assert.True(Guid.TryParse(id, out _), $"'{id}' is not a GUID");
        Assert.NotEqual(Guid.Parse(SourceUserSecretsId), Guid.Parse(id));
    }
}
