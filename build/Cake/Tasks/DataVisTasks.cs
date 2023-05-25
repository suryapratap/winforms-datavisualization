using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using static CakeScript.Startup;
using static CakeScript.CakeAPI;
using Cake.Common.IO;
using Cake.Common.Tools.NuGet.Pack;
using Cake.Common.Tools.DotNet.MSBuild;
using Cake.Common.Tools.DotNet.Pack;
using Cake.Common.Tools.DotNet.Build;
using System.ComponentModel;

namespace CakeScript;

partial class Program
{
    [DependsOn(nameof(PrepareNuget))]
    public void PackDataVis()
    {
        const string packageId = "FastReport.DataVisualization";
        string usedPackagesVersionPath = Path.Combine(solutionDirectory, "UsedPackages.version");
        string resourcesDir = Path.Combine(solutionDirectory, "Nuget");
        string packCopyDir = Path.Combine(resourcesDir, packageId);

        string srcDir = Path.Combine(solutionDirectory, "src");
        string dataVisAnyDir = Path.Combine(srcDir, packageId);
        string dataVisAnyProj = Path.Combine(dataVisAnyDir, "Chart.csproj");
        string dataVisWinDir = Path.Combine(srcDir, packageId + "-Windows");
        string dataVisWinProj = Path.Combine(dataVisWinDir, "Chart-Win.csproj");

        string tmpDir = Path.Combine(solutionDirectory, "tmp");
        if (DirectoryExists(tmpDir))
        {
            DeleteDirectory(tmpDir, new DeleteDirectorySettings
            {
                Force = true,
                Recursive = true
            });
        }

        DotNetClean(dataVisAnyProj);
        DotNetClean(dataVisWinProj);

        var buildSettings = new DotNetBuildSettings
        {
            Configuration = config,
            NoRestore = false,
            MSBuildSettings = new DotNetMSBuildSettings
            {
                Version = version,
            }.WithProperty("SolutionDir", solutionDirectory)
            .WithProperty("SolutionFileName", solutionFilename)
            .WithProperty("BaseOutputPath", tmpDir),
        };

        DotNetBuild(dataVisAnyProj, buildSettings);
        DotNetBuild(dataVisWinProj, buildSettings);

        string emptyFilePath = Path.Combine(tmpDir, "lib", "netcoreapp3.0", "_._");
        Directory.GetParent(emptyFilePath).Create();
        File.Create(emptyFilePath).Close();

        if (!File.Exists(emptyFilePath))
            throw new Exception($"Empty file wasn't created. '{emptyFilePath}'");


        // Remove FastReport.Compat library
        DeleteFiles(Path.Combine(tmpDir, "**", "FastReport.Compat.*"));

        // Get used packages version
        string FRCompatVersion = XmlPeek(usedPackagesVersionPath, "//FRCompatVersion/text()");
        Information($"FRCompatVersion: {FRCompatVersion}");

        var dependencies = new List<NuSpecDependency>();
        AddNuSpecDepAll("FastReport.Compat", FRCompatVersion);

        const string license = "LICENSE.txt";
        var files = new[] {
           new NuSpecContent{Source = Path.Combine(tmpDir, "**", "*.*"), Target = ""},
           new NuSpecContent{Source = Path.Combine(packCopyDir, "**", "*.*"), Target = ""},
           new NuSpecContent{Source = Path.Combine(solutionDirectory, FRLOGO192PNG), Target = "" },
           new NuSpecContent{Source = Path.Combine(solutionDirectory, license), Target = "" },
        };


        var nuGetPackSettings = new NuGetPackSettings
        {
            Id = packageId,
            Version = version,
            Authors = new[] { "Fast Reports Inc." },
            Owners = new[] { "Fast Reports Inc." },
            Description = "Charting library",
            Repository = new NuGetRepository { Type = "GIT", Url = "https://github.com/FastReports/winforms-datavisualization" },
            ProjectUrl = new Uri("https://www.fast-report.com/en/product/fast-report-net"),
            Icon = FRLOGO192PNG,
            IconUrl = new Uri("https://raw.githubusercontent.com/FastReports/FastReport.Compat/master/frlogo-big.png"),
            ReleaseNotes = new[] { "See the latest changes on https://github.com/FastReports/winforms-datavisualization" },
            License = new NuSpecLicense { Type = "file", Value = license },
            Copyright = "Fast Reports Inc.",
            Tags = new[] { "Chart", "WinForms", "Windows Forms DataVisualization", "DataVisualisation", "Data", "Visualization" },
            RequireLicenseAcceptance = true,
            Symbols = false,
            NoPackageAnalysis = true,
            Files = files,
            Dependencies = dependencies,
            BasePath = tmpDir,
            OutputDirectory = outdir
        };

        // Pack
        var template = Path.Combine(resourcesDir, nuGetPackSettings.Id + ".nuspec");
        NuGetPack(template, nuGetPackSettings);


        // Local functions:

        // For Net Standard 2.0, Core 3.0 and Net 5.0
        void AddNuSpecDepAll(string id, string version)
        {
            AddNuSpecDep(id, version, tfmNet40);
            AddNuSpecDep(id, version, tfmStandard20);
            AddNuSpecDep(id, version, tfmCore30);
            AddNuSpecDep(id, version, tfmNet5win7);
        }

        void AddNuSpecDep(string id, string version, string tfm)
        {
            dependencies.Add(new NuSpecDependency { Id = id, Version = version, TargetFramework = tfm });
        }
    }

    [DependsOn(nameof(PrepareNuget))]
    public void PackDataVisSkia()
    {
        const string packageId = "FastReport.DataVisualization.Skia";
        string projectFile = Path.Combine(solutionDirectory, "src", packageId, "Chart.Skia.csproj");

        TargetBuildCore("Clean");

        var packSettings = new DotNetPackSettings
        {
            Configuration = config,
            OutputDirectory = outdir,
            MSBuildSettings = new DotNetMSBuildSettings
            {
                Version = version,
            }
        };

        DotNetPack(projectFile, packSettings);


        // Local functions:

        void TargetBuildCore(string target)
        {
            DotNetMSBuild(projectFile, new DotNetMSBuildSettings()
              .SetConfiguration(config)
              .WithTarget(target)
              .WithProperty("SolutionDir", solutionDirectory)
              .WithProperty("SolutionFileName", solutionFilename)
              .WithProperty("Version", version)
            );
        }
    }

}
