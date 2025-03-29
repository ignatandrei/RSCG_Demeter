using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

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
        int maxDemeterDots = 0;
        RootData rootData = new();
        
        int nr = 0;
        List<FileLinePositionSpan> locations = new();
        //var root = left.SyntaxTrees.First().GetRoot();
        foreach (var invocation in invocations)
        {
            List<ITypeSymbol> types = new();
            if (invocation == null) continue;
            var text = invocation.ToFullString();
            if (text == null) continue;
            var retType = ReturnType(left.GetSemanticModel(invocation.SyntaxTree), invocation);
            if (retType != null) types.Add(retType);
            bool IsProblem = false;

            SyntaxNode? exp = invocation.Expression as MemberAccessExpressionSyntax;
            //if(exp == null) exp = invocation.Expression as InvocationExpressionSyntax;
            if (exp == null)
            {
                continue;
            }

            var nrDots = 0;
            while (exp != null)
            {

                if (exp is InvocationExpressionSyntax i)
                {
                    retType = ReturnType(left.GetSemanticModel(i.SyntaxTree), i);
                    if (retType != null)
                    {
                        if (types.Contains(retType))
                        {
                            //builder model
                            nrDots--;
                        }
                        else
                        {
                            types.Add(retType);
                        }
                    }
                    exp = i.Expression;
                }

                if (exp is MemberAccessExpressionSyntax m)
                {

                    exp = m.Expression;
                    nrDots++;
                    continue;
                }
                else
                    break;


            }
            if (maxDemeterDots < nrDots) maxDemeterDots = nrDots;
            IsProblem = nrDots > 1;
            if (!IsProblem) continue;
            var loc = invocation.GetLocation();
            var line = loc.GetLineSpan();
            if (locations.Any(it =>
            {
                return
                it.StartLinePosition == line.StartLinePosition
                &&
                it.Path == line.Path;
            })) continue;
            locations.Add(line);
            string textInvoc = (invocation?.ToFullString()) ?? "";
            //textInvoc = textInvoc.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
            var demeter = new Demeterlocation
            {
                id = ++nr,
                nrDots = nrDots,
                filePath = loc.SourceTree?.FilePath ?? "",
                text = textInvoc
            };
            demeter.SetLocation(loc);
            rootData.AddDemeterLocation(demeter);
        }
        if (writeToFile)
        {
#pragma warning disable RS1035 // Do not use APIs banned for analyzers
            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, filePath));
            }
            rootData.maxDemeterDots=maxDemeterDots;
            JsonSerializerOptions options = new() { WriteIndented = true };
            File.WriteAllText(filePath,JsonSerializer.Serialize(rootData,options) );
#pragma warning restore RS1035 // Do not use APIs banned for analyzers

     
        }
        else
        {
            foreach(var item in rootData.DemeterLocations)
            {
                var loc = item.location;
                DiagnosticDescriptor dd = new("RSCG001", "Demeter violation", $"Demeter violation {item.nrDots} found in {item.text}", "Demeter", DiagnosticSeverity.Error, true);
                Diagnostic diagnostic = Diagnostic.Create(dd, loc);
                spc.ReportDiagnostic(diagnostic);
            }

        }
    }

    private ITypeSymbol? ReturnType(SemanticModel semanticModel, InvocationExpressionSyntax invocation)
    {
        var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (methodSymbol == null) return null;

        var returnType = methodSymbol.ReturnType;
        //var receiverType = methodSymbol.ReceiverType;

        return returnType;
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
