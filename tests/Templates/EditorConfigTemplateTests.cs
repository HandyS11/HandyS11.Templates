using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.TemplateEngine.Authoring.TemplateVerifier;

namespace HandyS11.Templates.Tests.Templates;

public class EditorConfigTemplateTests
{
    private static readonly string TemplatePath =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "content",
            "editorconfig"));

    [Fact]
    public async Task EditorConfig_DefaultInstantiationAsync()
    {
        TemplateVerifierOptions options = new("handys11-editorconfig")
        {
            TemplatePath = TemplatePath,
            DisableDiffTool = true
        };

        VerificationEngine engine = new(NullLogger.Instance);
        await engine.Execute(options);
    }
}
