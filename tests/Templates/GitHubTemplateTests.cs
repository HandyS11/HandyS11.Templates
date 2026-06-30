using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.TemplateEngine.Authoring.TemplateVerifier;

namespace HandyS11.Templates.Tests.Templates;

public class GitHubTemplateTests
{
    private static readonly string TemplatePath =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "content", "github"));

    [Fact]
    public async Task GitHub_DefaultInstantiationAsync()
    {
        TemplateVerifierOptions options = new("handys11-github")
        {
            TemplatePath = TemplatePath,
            DisableDiffTool = true
        };

        VerificationEngine engine = new(NullLogger.Instance);
        await engine.Execute(options);
    }

    [Fact]
    public async Task GitHub_WithCustomNameAsync()
    {
        TemplateVerifierOptions options = new("handys11-github")
        {
            TemplatePath = TemplatePath,
            TemplateSpecificArgs = ["--name", "MyProject"],
            DisableDiffTool = true
        };

        VerificationEngine engine = new(NullLogger.Instance);
        await engine.Execute(options);
    }
}
