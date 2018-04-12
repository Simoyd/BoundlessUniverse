using MathNet.Spatial.Euclidean;

namespace BoundlessUniverse
{
    /// <summary>
    /// Object used per planet to store data for the engine execution.
    /// </summary>
    class PlanetInfo
    {
        /// <summary>
        /// Name of the planet
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Current location of the planet in 3D space
        /// </summary>
        public Vector3D Location { get; set; }

        /// <summary>
        /// Distances to other planets from this planet
        /// </summary>
        public BindRule[] Rules { get; set; }

        /// <summary>
        /// The current cumulative difference between the rule distances and the current distances
        /// </summary>
        public Vector3D CurForce { get; set; }
    }

}
