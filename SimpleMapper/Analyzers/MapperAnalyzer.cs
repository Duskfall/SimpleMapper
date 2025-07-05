#if ANALYZER
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MapperAnalyzer : DiagnosticAnalyzer
{
    // Rule: Detect ANY constructor parameters in mappers  
    public static readonly DiagnosticDescriptor ConstructorParametersInMapperRule = new DiagnosticDescriptor(
        id: "SM001",
        title: "Constructor parameters in mapper",
        messageFormat: "Mapper '{0}' should not have constructor parameters. Mappers should be pure data transformation functions with no dependencies. Move async logic and services to your service layer.",
        category: "SimpleMapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Mappers should not inject any services or have dependencies. They should be pure functions for data transformation only.");

    // Rule: Detect Task/async methods in mappers
    public static readonly DiagnosticDescriptor AsyncMethodInMapperRule = new DiagnosticDescriptor(
        id: "SM002", 
        title: "Async method in mapper",
        messageFormat: "Mapper '{0}' contains async method '{1}'. Mappers should be synchronous. Move async logic to service layer.",
        category: "SimpleMapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Mappers should contain only synchronous mapping logic.");

    // Rule: Detect .Result or .Wait() calls in mappers
    public static readonly DiagnosticDescriptor BlockingAsyncCallRule = new DiagnosticDescriptor(
        id: "SM003",
        title: "Blocking async call in mapper", 
        messageFormat: "Mapper '{0}' contains blocking async call '{1}'. This can cause deadlocks. Move async logic to service layer.",
        category: "SimpleMapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Blocking async calls in mappers can cause deadlocks and performance issues.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ConstructorParametersInMapperRule, AsyncMethodInMapperRule, BlockingAsyncCallRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        
        // Check if this class is a mapper (inherits from BaseMapper or implements IMapper)
        if (!IsMapperClass(classDeclaration, semanticModel))
            return;

        var className = classDeclaration.Identifier.ValueText;

        // Check constructor parameters - mappers should have none
        foreach (var constructor in classDeclaration.Members.OfType<ConstructorDeclarationSyntax>())
        {
            // Skip the parameterless constructor
            if (constructor.ParameterList.Parameters.Count > 0)
            {
                var diagnostic = Diagnostic.Create(
                    ConstructorParametersInMapperRule,
                    constructor.GetLocation(),
                    className);
                context.ReportDiagnostic(diagnostic);
            }
        }

        // Check for async methods
        foreach (var method in classDeclaration.Members.OfType<MethodDeclarationSyntax>())
        {
            if (method.Modifiers.Any(SyntaxKind.AsyncKeyword) || 
                method.ReturnType?.ToString().Contains("Task") == true)
            {
                var diagnostic = Diagnostic.Create(
                    AsyncMethodInMapperRule,
                    method.GetLocation(),
                    className,
                    method.Identifier.ValueText);
                context.ReportDiagnostic(diagnostic);
            }

            // Check for .Result or .Wait() calls
            CheckForBlockingAsyncCalls(method, context, className);
        }
    }

    private static bool IsMapperClass(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)
    {
        var symbol = semanticModel.GetDeclaredSymbol(classDeclaration);
        if (symbol == null) return false;

        // Check if inherits from BaseMapper<,> or implements IMapper<,>
        var baseTypes = symbol.AllInterfaces.Concat(new[] { symbol.BaseType }).Where(t => t != null);
        
        return baseTypes.Any(t => 
            t.Name.Contains("Mapper") ||
            (t.IsGenericType && t.Name == "IMapper"));
    }

    private static void CheckForBlockingAsyncCalls(MethodDeclarationSyntax method, SyntaxNodeAnalysisContext context, string className)
    {
        var memberAccesses = method.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
        
        foreach (var access in memberAccesses)
        {
            var memberName = access.Name.Identifier.ValueText;
            if (memberName == "Result" || memberName == "Wait")
            {
                var diagnostic = Diagnostic.Create(
                    BlockingAsyncCallRule,
                    access.GetLocation(),
                    className,
                    access.ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
#endif 