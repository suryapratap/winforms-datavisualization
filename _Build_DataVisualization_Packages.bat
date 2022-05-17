@echo off
setlocal ENABLEDELAYEDEXPANSION
rem HELP FOR THIS FILE
rem CLONE the fr if need
rem clear the fr if need
rem next run scripts from the fr tools
rem that's all

ECHO NOW TRY TO BUILD FR.DataVisualization

pushd .\build\Cake
   dotnet run --target=PackDataVis --solution-filename=DataViz.sln --config=Release --vers=2022.2.0
   dotnet run --target=PackDataVisSkia --solution-filename=DataViz.Skia.sln --config=Release --vers=2022.2.0
popd