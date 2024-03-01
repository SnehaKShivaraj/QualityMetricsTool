using System.Collections.Generic;
using System.Configuration;

namespace ServiceLayer
{
    class CustomConfiguration : ConfigurationSection
    {
        private const string git = "git";

        [ConfigurationProperty(git)]
        public GitElements GitElements => (GitElements)base[git];
    }

    class GitElements : ConfigurationElementCollection, IEnumerable<GitElement>
    {
        public GitElements()
        {
            AddElementName = "add";
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new GitElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((GitElement)element).Repository;
        }

        IEnumerator<GitElement> IEnumerable<GitElement>.GetEnumerator()
        {
            foreach (var key in this.BaseGetAllKeys())
            {
                yield return (GitElement)BaseGet(key);
            }
        }
    }

    class GitElement : ConfigurationElement
    {
        private const string repository = "repository";
        private const string targetBranches = "targetBranches";

        [ConfigurationProperty(repository, IsRequired = true, IsKey = true)]
        public string Repository => (string)base[repository];

        [ConfigurationProperty(targetBranches, IsRequired = false)]
        private string TargetBranchesInternal => (string)base[targetBranches];

        public ICollection<string> TargetBranches => TargetBranchesInternal?.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
    }
}
