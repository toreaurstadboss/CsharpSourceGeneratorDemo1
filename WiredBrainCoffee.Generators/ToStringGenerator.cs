using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace WiredBrainCoffee.Generators;

[Generator]
public class ToStringGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classes = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => IsSyntaxTarget(node),
            transform: static (ctx, _) => GetSemanticTarget(ctx))
            .Where(static (target) => target is not null);

        context.RegisterSourceOutput(classes,
            static (ctx, source) => Execute(ctx, source!));

        context.RegisterPostInitializationOutput(
            static (ctx) => PostInitializationOutput(ctx));
    }

    private static bool IsSyntaxTarget(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDeclarationSyntax
            && classDeclarationSyntax.AttributeLists.Count > 0;
    }

    private static ClassDeclarationSyntax? GetSemanticTarget(
        GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        foreach (var attributeListSyntax in classDeclarationSyntax.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                var attributeName = attributeSyntax.Name.ToString();

                if (attributeName == "GenerateToString"
                    || attributeName == "GenerateToStringAttribute")
                {
                    return classDeclarationSyntax;
                }
            }
        }

        return null;
    }

    private static void PostInitializationOutput(
        IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSource("WiredBrainCoffee.Generators.GenerateToStringAttribute.g.cs",
            @"namespace WiredBrainCoffee.Generators
{
    internal class GenerateToStringAttribute : System.Attribute { }
}");
    }

    private static void Execute(SourceProductionContext context,
        ClassDeclarationSyntax classDeclarationSyntax)
    {
        if (classDeclarationSyntax.Parent
            is BaseNamespaceDeclarationSyntax namespaceDeclarationSyntax)
        {
            var namespaceName = namespaceDeclarationSyntax.Name.ToString();
            var className = classDeclarationSyntax.Identifier.Text;
            var fileName = $"{namespaceName}.{className}.g.cs";

            var stringBuilder = new StringBuilder();
            stringBuilder.Append($@"namespace {namespaceName}
{{
    partial class {className}
    {{
        public override string ToString()
        {{
            return $""");

            var first = true;
            foreach (var memberDeclarationSyntax in classDeclarationSyntax.Members)
            {
                if (memberDeclarationSyntax
                    is PropertyDeclarationSyntax propertyDeclarationSyntax
                    && propertyDeclarationSyntax.Modifiers.Any(SyntaxKind.PublicKeyword))
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        stringBuilder.Append("; ");
                    }
                    var propertyName = propertyDeclarationSyntax.Identifier.Text;
                    stringBuilder.Append($"{propertyName}:{{{propertyName}}}");
                }
            }

            stringBuilder.Append($@""";
        }}
    }}
}}
");

            context.AddSource(fileName, stringBuilder.ToString());
        }
    }
}
