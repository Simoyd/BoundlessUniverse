using System;
using System.Xml.Serialization;

namespace BoundlessUniverse
{
    /// <summary>
    /// Class to store the measured blinksec distances from in-game
    /// </summary>
    [Serializable]
    public class BindRule
    {
        /// <summary>
        /// Planet to measure from
        /// </summary>
        [XmlAttribute]
        public string PlanetOne { get; set; }

        /// <summary>
        /// Planet to measure to
        /// </summary>
        [XmlAttribute]
        public string PlanetTwo { get; set; }

        /// <summary>
        /// Distance between the two planets in blinksecs
        /// </summary>
        [XmlAttribute]
        public double Distance { get; set; }
    }

}
