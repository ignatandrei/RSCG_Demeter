
using Microsoft.CodeAnalysis;

namespace RSCG_Demeter;

internal class RootData
{
    public RootData()
    {
        dateGenerator = Generated.RSCG_Demeter.TheAssemblyInfo.DateGeneratedUTC.ToString("yyyyMMddHHmmss");
        nameGenerator = Generated.RSCG_Demeter.TheAssemblyInfo.GeneratedNameNice;
    }
    public string dateGenerator { get; set; } 
    public string nameGenerator { get; set; } 
    public int maxDemeterDots { get; set; }
    public int locationsFound { get { return demeterLocations?.Count ?? 0; } set { } }
    public Demeterlocation[] DemeterLocations { 
        get
        {
            return demeterLocations.ToArray(); 
        }
        set
        {
            demeterLocations = value.ToList();
        }
    }

    List<Demeterlocation> demeterLocations = [];
    public void AddDemeterLocation(Demeterlocation loc)
    {
        if (loc.nrDots > maxDemeterDots) maxDemeterDots = loc.nrDots;
        demeterLocations.Add(loc);
    }
}

internal class Demeterlocation
{
    internal Location? location;
    public void SetLocation(Location loc)
    {
        location= loc;
        var line = loc.GetLineSpan();
        endLine = line.EndLinePosition.Line;
        startLine = line.StartLinePosition.Line;
                
    }
    public int id { get; set; }
    public int startLine { get; set; }
    public int nrDots { get; set; }
    public int endLine { get; set; }
    public string filePath { get; set; }=string.Empty;
    public string text { get; set; } = string.Empty;
}

