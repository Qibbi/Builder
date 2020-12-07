using System.Collections.Generic;

namespace Builder
{
    public class Project
    {
        public string Name { get; }
        public List<string> Languages { get; }
        public List<string> Maps { get; }

        public Project(string name)
        {
            Name = name;
            Languages = new List<string>();
            Maps = new List<string>();
        }
    }
}
