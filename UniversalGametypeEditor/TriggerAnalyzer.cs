using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TriggerAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TriggerAnalyzer";

    private static readonly LocalizableString Title = "Trigger Analyzer";
    private static readonly LocalizableString MessageFormat = "Trigger '{0}' detected";
    private static readonly LocalizableString Description = "Analyzes triggers in the code";
    private const string Category = "Syntax";

    private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        // Check if the method return type is "Trigger"
        if (methodDeclaration.ReturnType.ToString() == "Trigger")
        {
            var diagnostic = Diagnostic.Create(Rule, methodDeclaration.GetLocation(), methodDeclaration.Identifier.Text);
            context.ReportDiagnostic(diagnostic);

            // Process the trigger
            ProcessTrigger(methodDeclaration);
        }
    }

    private void ProcessTrigger(MethodDeclarationSyntax methodDeclaration)
    {
        // Implement the logic to process the trigger
        // This can include creating a new trigger, processing conditions, and actions
    }
}
