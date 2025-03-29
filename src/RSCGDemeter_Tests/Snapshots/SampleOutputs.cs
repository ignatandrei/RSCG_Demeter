namespace RSCG_Demeter.Tests.Snapshots;

/// <summary>
/// This class contains sample outputs from the Demeter analyzer for verification
/// </summary>
internal class SampleOutputs
{
    // Sample JSON output from the Demeter analyzer
    public const string ExpectedDemeterOutput = @"{
  ""dateGenerator"": ""20231226120000"",
  ""nameGenerator"": ""RSCG_Demeter"",
  ""maxDemeterDots"": 3,
  ""locationsFound"": 3,
  ""DemeterLocations"": [
    {
      ""id"": 1,
      ""startLine"": 23,
      ""nrDots"": 2,
      ""endLine"": 23,
      ""filePath"": ""TestFile.cs"",
      ""text"": ""await Task.Run(dep.GetEmployees);""
    },
    {
      ""id"": 2,
      ""startLine"": 26,
      ""nrDots"": 3,
      ""endLine"": 26,
      ""filePath"": ""TestFile.cs"",
      ""text"": ""var filteredIds = new List<int>(empAll.Select(it => it.ID).Distinct().OrderBy(it => it));""
    },
    {
      ""id"": 3,
      ""startLine"": 32,
      ""nrDots"": 2,
      ""endLine"": 34,
      ""filePath"": ""TestFile.cs"",
      ""text"": ""var assemblies = AppDomain.CurrentDomain.GetAssemblies()\r\n                .Where(it => data.Any(a => !(it.FullName?.StartsWith(a) ?? false)))\r\n                .Distinct()""
    }
  ]
}";

    // Sample for expected diagnostic message
    public const string ExpectedDiagnosticMessage = "Demeter violation 2 found in dep.Employees.Where(it => it.Name.StartsWith(\"a\"))";
}
