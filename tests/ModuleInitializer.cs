using Microsoft.TemplateEngine.Authoring.TemplateVerifier;
using System.Runtime.CompilerServices;

namespace HandyS11.Templates.Tests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    internal static void Initialize() =>
        VerificationEngine.DirectoryVerifier = VerifyXunit.Verifier.VerifyDirectory;
}
