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
            predicate: static (node, _) => node is ClassDeclarationSyntax,
            transform: static (ctx, _) => (ClassDeclarationSyntax)ctx.Node);

        context.RegisterSourceOutput(classes,
            static (ctx, source) => Execute(ctx, source));
    }

    private static void Execute(SourceProductionContext context,
        ClassDeclarationSyntax classDeclarationSyntax)
    {
        if (classDeclarationSyntax.Parent is BaseNamespaceDeclarationSyntax)
        {
            var namespaceName = ((NamespaceDeclarationSyntax)classDeclarationSyntax.Parent).Name.ToString();
            var className = classDeclarationSyntax.Identifier.Text;
            var fileName = $"{namespaceName}{className}.g.cs";


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
