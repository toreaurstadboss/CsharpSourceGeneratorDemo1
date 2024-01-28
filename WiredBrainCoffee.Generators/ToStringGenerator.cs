using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WiredBrainCoffee.Generators;

[Generator]
public class ToStringGenerator : IIncrementalGenerator
{

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classes = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => IsSyntaxTarget(node),
            transform: static (ctx, _) => GetSemanticTarget(ctx))
            .Where(static target => target is not null);

        context.RegisterSourceOutput(classes,
            static (ctx, source) => Execute(ctx, source!));

        context.RegisterPostInitializationOutput(
            static (ctx) => PostInitializationOutput(ctx));
    }

    private static ClassDeclarationSyntax? GetSemanticTarget(GeneratorSyntaxContext ctx)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)ctx.Node;
        foreach (var attributeListSyntax in classDeclarationSyntax.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                var attributeName = attributeSyntax.Name.ToString();
                if (attributeName == "GenerateToString" || attributeName == "GenerateToStringAttribute")
                {
                    return classDeclarationSyntax;
                }
            }                          
        }
        return null;
    }

    private static bool IsSyntaxTarget(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDeclarationSyntax
            && classDeclarationSyntax.AttributeLists.Any();
    }

    private static void PostInitializationOutput(
        IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSource("WiredBrainCoffee.Generators.GenerateToStringAttribute.g.cs",
            @"namespace WiredBrainCoffee.Generators
{
    public partial class GenerateToStringAttribute : System.Attribute { }
}");
    }

    private static void Execute(SourceProductionContext context,
        ClassDeclarationSyntax classDeclarationSyntax)
    {
        if (classDeclarationSyntax.Parent is BaseNamespaceDeclarationSyntax)
        {
            var namespaceName = ((NamespaceDeclarationSyntax)classDeclarationSyntax.Parent).Name.ToString();
            var className = classDeclarationSyntax.Identifier.Text;
            var fileName = $"{namespaceName}.{className}.g.cs";

            var properties = classDeclarationSyntax.ChildNodes().OfType<PropertyDeclarationSyntax>().ToList();

            var sb = new StringBuilder();
            sb.Append($@"namespace {namespaceName};
    partial class {className}
    {{

        public override string ToString() {{
            return $""");

            var first = true; 

            foreach (var member in classDeclarationSyntax.Members)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append("; ");
                }
                if (member is PropertyDeclarationSyntax propertyDeclarationSyntax)
                {
                    if (propertyDeclarationSyntax.Modifiers.Any(SyntaxKind.PublicKeyword))
                    {

                        sb.Append($"{propertyDeclarationSyntax.Identifier.Text}:{{{propertyDeclarationSyntax.Identifier.Text}}}");
                    }
                    else if (propertyDeclarationSyntax.Modifiers.Any(SyntaxKind.InternalKeyword))
                    {
                        sb.Append($"{propertyDeclarationSyntax.Identifier.Text}: Cannot show contents of property: it is internal");
                    }
                }

            }

            sb.Append($@""";

        }}

    }}
");


            string createdToStringMethod = sb.ToString();
            context.AddSource(fileName, createdToStringMethod);
        }
    }
}
