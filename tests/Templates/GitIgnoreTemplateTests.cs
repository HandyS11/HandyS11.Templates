using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.TemplateEngine.Authoring.TemplateVerifier;

namespace HandyS11.Templates.Tests.Templates;

public class GitIgnoreTemplateTests
{
    private static readonly string TemplatePath =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "content", "gitignore"));

    [Fact]
    public async Task GitIgnore_DefaultInstantiationAsync()
    {
        TemplateVerifierOptions options = new("handys11-gitignore")
        {
            TemplatePath = TemplatePath,
            DisableDiffTool = true
        };

        VerificationEngine engine = new(NullLogger.Instance);
        await engine.Execute(options);
    }
}
