using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.TemplateEngine.Authoring.TemplateVerifier;
using System.Text.Json;
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

    // The snapshot tests skip the post-actions (--files-only), so their fallback text is checked here: a failed
    // post-action prints it for the user to copy-paste, so it must name the real generated paths (renamed by the
    // engine from Project1), the package id, and its pinned version.
    [Fact]
    public void PostActions_ManualInstructionsAreRunnableCommands()
    {
        using var template = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(TemplatePath, ".template.config", "template.json")));
        JsonElement[] postActions = [.. template.RootElement.GetProperty("postActions").EnumerateArray()];

        Assert.Equal(8, postActions.Length);
        foreach (var action in postActions)
        {
            Assert.True(action.GetProperty("applyFileRenamesToManualInstructions").GetBoolean());
            var text = action.GetProperty("manualInstructions")[0].GetProperty("text").GetString()!;
            Assert.DoesNotContain("<", text, StringComparison.Ordinal);
            Assert.Contains("tools/Project1.", text, StringComparison.Ordinal);

            var args = action.GetProperty("args");
            if (!args.TryGetProperty("reference", out var reference))
            {
                continue;
            }

            Assert.Contains($" package {reference.GetString()}", text, StringComparison.Ordinal);
            if (args.TryGetProperty("version", out var version))
            {
                Assert.Contains($" --version {version.GetString()}", text, StringComparison.Ordinal);
            }
            else
            {
                Assert.DoesNotContain("--version", text, StringComparison.Ordinal);
            }
        }
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
