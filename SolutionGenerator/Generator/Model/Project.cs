using System;

namespace SolutionGen.Generator.Model
{
    public class Project
    {
        public string Name { get; }
        public Guid Guid { get; }

        public Project(string name, Guid guid)
        {
            Name = name;
            Guid = guid;
        }
    }
}