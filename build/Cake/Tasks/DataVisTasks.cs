using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using static CakeScript.Startup;
using static CakeScript.CakeAPI;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore.MSBuild;
using Cake.Common.Tools.NuGet.Pack;

namespace CakeScript
{
    partial class Program
    {
        [DependsOn(nameof(PrepareNuget))]
        public void PackDataVis()
        {
            const string packageId = "FastReport.DataVisualization";
            string solutionFile = Path.Combine(solutionDirectory, solutionFilename);
            string usedPackagesVersionPath = Path.Combine(solutionDirectory, "UsedPackages.version");
            string resourcesDir = Path.Combine(solutionDirectory, "Nuget");
            string packCopyDir = Path.Combine(resourcesDir, packageId);

            string nugetDir = Path.Combine(solutionDirectory, "bin", IsRelease ? "nuget" : config);

            // Clean nuget directory for package
            if (DirectoryExists(nugetDir))
            {
                DeleteDirectory(nugetDir, new DeleteDirectorySettings
                {
                    Force = true,
                    Recursive = true
                });
            }

            TargetBuildCore("Clean");

            TargetBuildCore("Restore");

            TargetBuildCore("Build");

            TargetBuildCore("PrepareDataVisPackage");

            // Remove FastReport.Compat library
            DeleteFiles(Path.Combine(nugetDir, "**", "FastReport.Compat.dll"));

            // Get used packages version
            string FRCompatVersion = XmlPeek(usedPackagesVersionPath, "//FRCompatVersion/text()");
            Information($"FRCompatVersion: {FRCompatVersion}");

            var dependencies = new List<NuSpecDependency>();
            AddNuSpecDep("FastReport.Compat", FRCompatVersion, tfmNet40);
            AddNuSpecDepCore("FastReport.Compat", FRCompatVersion);

            var files = new[] {
               new NuSpecContent{Source = Path.Combine(nugetDir, "**", "*.*"), Target = ""},
               new NuSpecContent{Source = Path.Combine(packCopyDir, "**", "*.*"), Target = ""},
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
                License = new NuSpecLicense { Type = "file", Value = "LICENSE.txt" },
                Copyright = "Fast Reports Inc.",
                Tags = new[] { "Chart", "WinForms", "Windows Forms DataVisualization", "DataVisualisation", "Data", "Visualization" },
                RequireLicenseAcceptance = true,
                Symbols = false,
                NoPackageAnalysis = true,
                Files = files,
                Dependencies = dependencies,
                BasePath = nugetDir,
                OutputDirectory = outdir
            };

            // Pack
            var template = Path.Combine(resourcesDir, nuGetPackSettings.Id + ".nuspec");
            NuGetPack(template, nuGetPackSettings);


            // Local functions:

            // For Net Standard 2.0, Core 3.0 and Net 5.0
            void AddNuSpecDepCore(string id, string version)
            {
                AddNuSpecDep(id, version, tfmStandard20);
                AddNuSpecDep(id, version, tfmCore30);
                AddNuSpecDep(id, version, tfmNet5win7);
            }

            void AddNuSpecDep(string id, string version, string tfm)
            {
                dependencies.Add(new NuSpecDependency { Id = id, Version = version, TargetFramework = tfm });
            }

            void TargetBuildCore(string target)
            {
                DotNetMSBuild(solutionFile, new DotNetCoreMSBuildSettings()
                  .SetConfiguration(config)
                  .WithTarget(target)
                  .WithProperty("SolutionDir", solutionDirectory)
                  .WithProperty("SolutionFileName", solutionFilename)
                  .WithProperty("Version", version)
                );
            }

        }
    }
}
