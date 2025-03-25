using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace RSCG_Demeter;

[Generator]
public class Demeter : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {

        var writeToFile = context.AnalyzerConfigOptionsProvider.Select((provider, ct) =>

        {
            var filePath = 
                provider.GlobalOptions.TryGetValue("build_property.RSCG_Demeter_GenerateFile", out var emitLoggingSwitch)
                ? (emitLoggingSwitch ?? "") : "";

            if (string.IsNullOrWhiteSpace(filePath)) return filePath;
#pragma warning disable RS1035 // Do not use APIs banned for analyzers
            if (!Path.IsPathRooted(filePath))
            {
                if(provider.GlobalOptions.TryGetValue("build_property.ProjectDir", out var csproj))
                {
                    if (!string.IsNullOrWhiteSpace(csproj))
                    {
                        filePath = Path.GetFullPath(Path.Combine(csproj, filePath));
                    }
                }

            }
            return filePath;
#pragma warning restore RS1035 // Do not use APIs banned for analyzers
        }
        );
        
        var lines = context.SyntaxProvider.CreateSyntaxProvider(
           predicate: (sn, _) => FindDemeter(sn),
           transform: (ctx, _) => GetDataForGeneration(ctx)

            )
        .Where(it => it != null)
        .Select((it,_)=>it!)
        ;
        
        var comp = context
            .CompilationProvider
            .Combine(lines.Collect())
            .Combine(writeToFile);

        context.RegisterSourceOutput( comp, (spc,source)=>Execute(spc,source.Left.Left,source.Left.Right,source.Right));

    }

    private void Execute(SourceProductionContext spc, Compilation left, ImmutableArray<InvocationExpressionSyntax> invocations, string filePath)
    {
        bool writeToFile = !string.IsNullOrWhiteSpace(filePath);
        if (invocations == null) return;
        var data = invocations.Select(it => it).ToArray();
        if (data.Length == 0) return;
        var dtG = Generated.RSCG_Demeter.TheAssemblyInfo.DateGeneratedUTC.ToString("yyyyMMddHHmmss");
        var nameG = Generated.RSCG_Demeter.TheAssemblyInfo.GeneratedNameNice;
        string maxDemeterDotsString = "Max@#$"; 
        int maxDemeterDots = 0;
        string json = $$"""
{ 
"dateGenerator":"{{dtG}}",
"nameGenerator":"{{nameG}}",
"maxDemeterDots":"{{maxDemeterDotsString}}",
"DemeterLocations": [
""";
        int nr = 0;
        //var root = left.SyntaxTrees.First().GetRoot();
        foreach (var invocation in invocations)
        {
            if (invocation == null) continue;
            var text = invocation.ToFullString();
            if (text == null) continue;

            bool IsProblem = false;
            var exp = invocation.Expression as MemberAccessExpressionSyntax;
            if(exp == null)
            {
                continue;
            }
            var nrDots = 0;
            while (exp != null)
            {
                nrDots++;
                exp = exp.Expression as MemberAccessExpressionSyntax;
                
            }
            if (maxDemeterDots < nrDots) maxDemeterDots = nrDots;
            IsProblem = nrDots > 1;
            //var insides = ExtractAllInsideParentheses(text);
            //
            //foreach (var inside in insides)
            //{
            //    var nrDots = inside.Count(c => c == '.');
            //    if (nrDots < 2) continue;
            //    IsProblem = true;
            //    break;
            //}
            if (!IsProblem) continue;
            var loc = invocation.GetLocation();
            var line = loc.GetLineSpan();
            string textInvoc = (invocation?.ToFullString()) ?? "";
            textInvoc = textInvoc.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
            textInvoc =textInvoc.Replace("\"","\\\"");
            if (writeToFile)
            {
                nr++;
                json += $$"""
{
    "id": {{nr}},
    "startLine":  {{line.StartLinePosition.Line}} , 
    "nrDots": {{nrDots}},
    "endLine":  {{line.EndLinePosition.Line}} ,
    "filePath": "{{loc.SourceTree?.FilePath}}",
    "text": "{{textInvoc}}"
},

""";
            }
            else
            {
                DiagnosticDescriptor dd = new("RSCG001", "Demeter violation", $"Demeter violation found in {text}", "Demeter", DiagnosticSeverity.Error, true);
                Diagnostic diagnostic = Diagnostic.Create(dd, loc);
                spc.ReportDiagnostic(diagnostic);
            }
            //spc.AddSource("andrei.cs", text);
        }
        json += """
]}
""";
        if (writeToFile)
        {
#pragma warning disable RS1035 // Do not use APIs banned for analyzers
            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, filePath));
            }
            json = json.Replace(maxDemeterDotsString, maxDemeterDots.ToString());
            File.WriteAllText(filePath, json);
#pragma warning restore RS1035 // Do not use APIs banned for analyzers

     
        }
    }

    private static string[] ExtractAllInsideParentheses(string input)
    {
        input = "(" + input + ")";
        var matches = Regex.Matches(input, @"\(([^)]*)\)");
        var results = new List<string>();
        foreach (Match match in matches)
        {
            if (match.Success)
            {
                results.Add(match.Groups[0].Value);
            }
        }
        return results.ToArray();
    }

    private InvocationExpressionSyntax? GetDataForGeneration(GeneratorSyntaxContext ctx)
    {
        var node =ctx.Node as InvocationExpressionSyntax;
        if (node == null) return null;
        var text=node.ToFullString();
        if(text == null) return null;
        var noDot = text.Replace(".", "");
        if(text.Length-noDot.Length <2) return null;
        return node;
    }

    private bool FindDemeter(SyntaxNode node)
    {
        if (!(node is InvocationExpressionSyntax))
            return false;

        return true;
    }
}
