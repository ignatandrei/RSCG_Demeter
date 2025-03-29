using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Diagnostics.Metrics;
using System.Text.Json;

namespace RSCG_Demeter.Tests;

[TestClass]
public class DemeterGeneratorTests
{
    [TestMethod]
    public void Test_Demeter_FindsViolationsInSource()
    {
        // Arrange
        var generator = new Demeter();
        // Set up the test diagnostics
        var driver = CSharpGeneratorDriver.Create(generator);
        var sourceModels = TestSourceBuilder.GenerateDepartmentAndEmployeeModel();
        var sourceProgram = TestSourceBuilder.GenerateDemeterViolations();

        var compilation = TestSourceBuilder.CreateCompilation(sourceModels + sourceProgram);


        // Act
        // Act
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var generatorDiagnostics);
        var diagnostics = generatorDiagnostics.ToList(); 
    
        // Assert
        Assert.IsTrue(diagnostics.Count > 0, "Should detect at least one Demeter violation");
        Assert.IsTrue(diagnostics.All(d => d.Id == "RSCG001"), "All diagnostics should be Demeter violations");

        // Verify we detect the violations from the sample code
        var demeterViolations = diagnostics.Where(d => d.Id == "RSCG001").ToList();

        // Check specific violations we expect to find
        Assert.IsTrue(demeterViolations.Any(d => d.GetMessage().Contains("empAll.Select(it => it.ID).Distinct()")),
            "Should detect violation in empAll.Select().Distinct().OrderBy()");

        Assert.IsTrue(demeterViolations.Any(d => d.GetMessage().Contains("AppDomain.CurrentDomain.GetAssemblies()")),
            "Should detect violation in AppDomain.CurrentDomain.GetAssemblies()");

        // The builder pattern should not report violations for chained method calls that return the same type
        Assert.IsFalse(demeterViolations.Any(d => d.GetMessage().Contains("SetName(\"Ignat\").SetId(1).SetName(\"Andrei\")")),
            "Should not detect violation in builder pattern");
    }

    [TestMethod]
    public void Test_Demeter_GeneratesOutputFile()
    {
        // Arrange
        var generator = new Demeter();
        var outputPath = Path.Combine(Path.GetTempPath(), "demeter_test_output.txt");
        // Clean up
        if (File.Exists(outputPath))
            File.Delete(outputPath);
        var analyzerConfigOptions = new Dictionary<string, string>
        {
            ["build_property.RSCG_Demeter_GenerateFile"] = outputPath,
            ["build_property.ProjectDir"] = Directory.GetCurrentDirectory()
        }.ToImmutableDictionary();

        var configProvider = new TestAnalyzerConfigOptionsProvider(analyzerConfigOptions);
        var driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator.AsSourceGenerator() },
            driverOptions: new GeneratorDriverOptions(
                IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true)
                ,optionsProvider: configProvider
             );

        var sourceModels = TestSourceBuilder.GenerateDepartmentAndEmployeeModel();
        var sourceProgram = TestSourceBuilder.GenerateDemeterViolations();

        var compilation = TestSourceBuilder.CreateCompilation(sourceModels + sourceProgram);

        // Act
        var updatedCompilation = driver.RunGenerators(compilation);

        // Assert
        Assert.IsTrue(File.Exists(outputPath), "Output file should be generated");
        var content = File.ReadAllText(outputPath);
        Assert.IsTrue(content.Contains("DemeterLocations"), "Output should contain DemeterLocations");
        Assert.IsTrue(content.Contains("maxDemeterDots"), "Output should contain maxDemeterDots");
        RootData? rootData = JsonSerializer.Deserialize<RootData>(content);
        Assert.IsNotNull(rootData, "Output should be deserialized to RootData");
        Assert.IsTrue(rootData!.DemeterLocations.Length > 0, "Should have at least one Demeter location");
        Assert.IsTrue(rootData!.maxDemeterDots == 4, $"Should have a 4 , but instead {rootData!.maxDemeterDots} value");
        // Clean up
        if (File.Exists(outputPath))
            File.Delete(outputPath);
    }

    

    private class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly ImmutableDictionary<string, string> _globalOptions;

        public TestAnalyzerConfigOptionsProvider(ImmutableDictionary<string, string> globalOptions)
        {
            _globalOptions = globalOptions;
        }

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return new TestAnalyzerConfigOptions(_globalOptions);
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return new TestAnalyzerConfigOptions(_globalOptions);
        }

        public override AnalyzerConfigOptions GlobalOptions => new TestAnalyzerConfigOptions(_globalOptions);
    }

    private class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly ImmutableDictionary<string, string> _options;

        public TestAnalyzerConfigOptions(ImmutableDictionary<string, string> options)
        {
            _options = options;
        }

        public override bool TryGetValue(string key, out string value)
        {
            return _options.TryGetValue(key, out value);
        }
    }
}
