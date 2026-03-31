using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.TemplateEngine.Authoring.TemplateVerifier;

namespace HandyS11.Templates.Tests.Templates;

public class SolutionTemplateTests
{
    private static readonly string TemplatePath =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "content", "solution"));

    [Fact]
    public async Task Solution_DefaultInstantiationAsync()
    {
        TemplateVerifierOptions options = new("handys11-solution")
        {
            TemplatePath = TemplatePath,
            DisableDiffTool = true
        };

        VerificationEngine engine = new(NullLogger.Instance);
        await engine.Execute(options);
    }

    [Fact]
    public async Task Solution_WithCustomNameAsync()
    {
        TemplateVerifierOptions options = new("handys11-solution")
        {
            TemplatePath = TemplatePath,
            TemplateSpecificArgs = ["--name", "MyProject"],
            DisableDiffTool = true
        };

        VerificationEngine engine = new(NullLogger.Instance);
        await engine.Execute(options);
    }
}
