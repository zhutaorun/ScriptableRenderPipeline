// C# script to generate source code files
// Use the dotnet script command (https://github.com/filipw/dotnet-script)
// Example: dotnet script GenerateUIClasses.csx -- MyClass.cs -o TargetDir

#r "nuget: Microsoft.Extensions.CommandLineUtils, 1.1.1"
#r "nuget: Microsoft.CodeAnalysis, 2.9.0"
#r "nuget: Microsoft.CodeAnalysis.CSharp, 2.9.0"
#r "nuget: Microsoft.CodeAnalysis.CSharp.Scripting, 2.9.0"

using Microsoft.Extensions.CommandLineUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;

var app = new CommandLineApplication();
app.HelpOption("-h|--help");

// Define the commands
var sourceFilesArg = app.Argument(
    "sourceFile",
    "Source file to parse for generation",
    true
);

var targetDirOption = app.Option(
    "-o|--output <outputDir>",
    "Output directory for generated files",
    CommandOptionType.MultipleValue
);

app.OnExecute(() =>
{
    foreach (var source in sourceFilesArg.Values)
    {
        var result = GenerateFiles(source, targetDirOption.Value());
        if (result != 0)
            return result;
    }
    return 0;
});

app.Execute(Args.ToArray());


// Entry point for code generation
int GenerateFiles(string sourceFile, string targetDir)
{
    try
    {
        var sourceCode = File.ReadAllText(sourceFile);
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var walker = new Walker();
        walker.Visit(syntaxTree.GetRoot());
        foreach (var parsedType in walker.parsedTypes)
        {
            var className = parsedType.declaration.Identifier.ToString();
            var serializedClassName = "Serialized" + className;
            var targetFile = Path.Combine(targetDir, serializedClassName + ".g.cs");
            var targetContent = GenerateSerializedClass(parsedType);
            File.WriteAllText(targetFile, targetContent);
            Console.WriteLine($"{sourceFile} -> {targetFile}");
        }
            
    }
    catch (Exception e)
    {
        Console.Error.WriteLine(e);
        return 1;
    }
    return 0;
}

string GenerateSerializedClass(Walker.ParsedType parsedType)
{
    var className = parsedType.declaration.Identifier.ToString();
    var genClassName = $"Serialized{className}";

    var genFields = new StringBuilder();
    foreach (var property in parsedType.publicInstanceProperties)
    {
        
    }

    return $@"using UnityEditor;
using UnityEngine;
    
namespace {parsedType.@namespace}
{{
    public partial class {genClassName}
    {{
        internal protected serializedObject serializedObject;

{genFields}

        public {genClassName}(SerializedObject serializedObject)
        {{
            this.serializedObject = serializedObject;

        }}
    }}
}}";
}


class Walker : CSharpSyntaxWalker
{
    public class ParsedType
    {
        public string @namespace;
        public ClassDeclarationSyntax declaration;
        public List<PropertyDeclarationSyntax> publicInstanceProperties 
            = new List<PropertyDeclarationSyntax>();
    }

    Stack<string> m_Namespaces = new Stack<string>();
    ParsedType m_CurrentType = null;

    public List<ParsedType> parsedTypes = new List<ParsedType>();

    public Walker(SyntaxWalkerDepth depth = SyntaxWalkerDepth.Node)
        : base(depth)
    {
    }

    public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        m_Namespaces.Push(node.Name.ToString());
        base.VisitNamespaceDeclaration(node);
        m_Namespaces.Pop();
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        m_CurrentType = new ParsedType
        {
            declaration = node,
            @namespace = ComputeCurrentNamespace()
        };
        parsedTypes.Add(m_CurrentType);
        base.VisitClassDeclaration(node);
        m_CurrentType = null;
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        if (ContainsKind(node.Modifiers, SyntaxKind.PublicKeyword)
            && !ContainsKind(node.Modifiers, SyntaxKind.StaticKeyword))
            m_CurrentType.publicInstanceProperties.Add(node);
        base.VisitPropertyDeclaration(node);
    }

    string ComputeCurrentNamespace()
    {
        var namespaceBuilder = new StringBuilder();
        foreach (var @namespace in m_Namespaces)
        {
            if (namespaceBuilder.Length > 0)
                namespaceBuilder.Append('.');
            namespaceBuilder.Append(@namespace);
        }
        return namespaceBuilder.ToString();
    }

    bool ContainsKind(SyntaxTokenList tokenList, SyntaxKind kind)
    {
        foreach (var token in tokenList)
        {
            if (token.IsKind(kind))
                return true;
        }
        return false;
    }
}