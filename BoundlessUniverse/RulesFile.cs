using System.Collections.Generic;
using System.Linq;

namespace BoundlessUniverse
{
    /// <summary>
    /// Class used to handle persistance of the distances between the planets
    /// </summary>
    class RulesFile : PersistanceFileAbstraction<List<BindRule>>
    {
        /// <summary>
        /// Creates a new instance of RulesFile
        /// </summary>
        public RulesFile() : base("rules.xml") { }

        /// <summary>
        /// Expose the entire raw data
        /// </summary>
        public List<BindRule> Data
        {
            get
            {
                // Return a clone of the collection to ensure thread safety
                lock (persistanceFile)
                {
                    return persistanceFile.Data.ToList();
                }
            }
        }
    }
}
