To compile the .tt template:
    cd SolutionGenerator\SolutionGenerator
    
    # restore is only needed if there is an error indicating so
    dotnet restore
    
    dotnet tt -c SolutionGen.Templates.DotNetSolution -o .\Templates\DotNetSolution.cs .\Templates\DotNetSolution.tt
    dotnet tt -c SolutionGen.Templates.DotNetProject -o .\Templates\DotNetProject.cs .\Templates\DotNetProject.tt