using MathNet.Numerics.Statistics;
using MathNet.Spatial.Euclidean;
using MathNet.Spatial.Units;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BoundlessUniverse
{
    /// <summary>
    /// Class used to determine the best fit location of all planets based on measured distances from in-game
    /// </summary>
    class Engine
    {
        /// <summary>
        /// Used to place planets for original position
        /// </summary>
        private static readonly Random r = new Random();

        /// <summary>
        /// Attempts to find unique solutions. Does not return.
        /// </summary>
        public void Run()
        {
            // Read the rules data from disk.
            List<BindRule> rules = new RulesFile().Data;

            // Get a list of all planets
            string[] planets = rules.Select(cur => cur.PlanetOne).Union(rules.Select(cur => cur.PlanetTwo)).ToArray();

            // Create a PlanetInfo object for each planet in a dictionary for easy lookup
            // Ensure each planet has all the rules relating to it
            Dictionary<string, PlanetInfo> planetLocations = planets.Select(cur => new PlanetInfo
            {
                Name = cur,
                Rules = rules.Where(curRule => curRule.PlanetOne == cur || curRule.PlanetTwo == cur).ToArray(),
            }).ToDictionary(cur => cur.Name, cur => cur);

            List<Dictionary<string, Vector3D>> allSolutions = new List<Dictionary<string, Vector3D>>();

            int curAttempt = 0;

            // Loop forever
            while (true)
            {
                ++curAttempt;

                // Find an arangement of planets relative to eachother
                FindSolution(curAttempt, planets, planetLocations);

                // Align the planets in a standard way for 2 reasons:
                //   1. So we can check if it's duplicate
                //   2. For better 2D viewing
                Dictionary<string, Vector3D> finalSolution = FlattenSolution(planets, planetLocations);

                // Check to see if the solution is duplicate
                bool duplicate = false;
                foreach (Dictionary<string, Vector3D> oldSolution in allSolutions)
                {
                    if (!planets.Any(cur => (oldSolution[cur] - finalSolution[cur]).Length > 0.001))
                    {
                        duplicate = true;
                        break;
                    }
                }

                // Measure the quality and output the solution
                if (!duplicate)
                {
                    allSolutions.Add(finalSolution);
                    double quality = rules.Average(cur => Math.Abs((finalSolution[cur.PlanetOne] - finalSolution[cur.PlanetTwo]).Length - cur.Distance));
                    OutputSolution(allSolutions.Count, quality, finalSolution);
                }
            }
        }

        /// <summary>
        /// Moves planets relative to eachother until they fit the in-game distances as close as possible
        /// </summary>
        /// <param name="curAttempt">The current attempt number for display</param>
        /// <param name="planets">array of all planet names</param>
        /// <param name="planetLocations">The planet location data.</param>
        private void FindSolution(int curAttempt, string[] planets, Dictionary<string, PlanetInfo> planetLocations)
        {
            // Randomly setup all the planets to start.
            foreach (PlanetInfo curInfo in planetLocations.Values)
            {
                curInfo.Location = new Vector3D(r.NextDouble(), r.NextDouble(), r.NextDouble());
            }

            // Tracks the current cycle for display. A cycle is 100 iterations.
            int curCycle = 0;

            // Stopwatch used to output progress without limiting performance due to console output
            Stopwatch sw = new Stopwatch();
            sw.Restart();

            // Loop until things stop moving
            while (true)
            {
                ++curCycle;

                // Save the locations so we can compare later to see if things are still moving
                Vector3D[] prevLocations = planets.Select(cur => planetLocations[cur].Location).ToArray();

                for (int i = 0; i < 100; ++i)
                {
                    // *** STEP ONE ***
                    // Loop through all planets, and find the cumulative difference
                    // between the actual distance and current distance
                    foreach (PlanetInfo curInfo in planetLocations.Values)
                    {
                        // Clear the force from the last iteration for the current planet
                        curInfo.CurForce = new Vector3D();

                        // Go through the distances to all other planets from the current planet
                        foreach (BindRule curRule in curInfo.Rules)
                        {
                            // Current planet might be PlanetOne or PlanetTwo, so find
                            // the other planet so we can look up it's current position
                            string otherPlanet = curRule.PlanetOne;
                            if (otherPlanet == curInfo.Name)
                            {
                                otherPlanet = curRule.PlanetTwo;
                            }

                            // Get the current direction and distance
                            Vector3D direction = curInfo.Location - planetLocations[otherPlanet].Location;
                            double distance = direction.Length;
                            direction = direction.Normalize().ToVector3D();

                            // Get the difference between the current and actual
                            double difference = curRule.Distance - distance;

                            // Add the direction with magnitude of the difference to the total
                            curInfo.CurForce += difference * direction;
                        }
                    }

                    // Output progress
                    if (sw.ElapsedMilliseconds > 100)
                    {
                        Console.Write($"\rAttempt: {curAttempt} - Iteration: {curCycle * 100} - Accuracy: {planetLocations.Values.Select(cur => cur.CurForce.Length).Max():0.####}        ");
                        sw.Restart();
                    }

                    // *** STEP TWO ***
                    // Move each planet in the correct direction
                    foreach (PlanetInfo curInfo in planetLocations.Values)
                    {
                        // Limit the movements to 0.0001 blinksecs per iteration
                        if (curInfo.CurForce.Length > 0.0001)
                        {
                            curInfo.CurForce = curInfo.CurForce.Normalize().ToVector3D();
                            curInfo.CurForce = 0.0001 * curInfo.CurForce;
                        }

                        // Add the movement to the current location
                        curInfo.Location += curInfo.CurForce;
                    }
                }

                // If we havn't moved enough, then we've found a solution so break out
                if (!planets.Select((cur, index) => prevLocations[index] - planetLocations[cur].Location).Select(cur => cur.Length).Any(cur => cur > 0.0005))
                {
                    //Console.WriteLine($"\rFound a solution after {curCycle * 100:#,##0} iterations.");
                    break;
                }
            }
        }

        /// <summary>
        /// Aligns the solution in a standard way to be as "flat" as possible for 2D viewing
        /// </summary>
        /// <param name="planets">array of all planet names</param>
        /// <param name="planetLocations">The planet location data.</param>
        /// <returns>The "flattest" alignment</returns>
        private Dictionary<string, Vector3D> FlattenSolution(string[] planets, Dictionary<string, PlanetInfo> planetLocations)
        {
            Dictionary<string, Vector3D> bestResult = new Dictionary<string, Vector3D>();
            double resultQuality = double.MaxValue;
            string[] bestAlignment = new string[] { string.Empty, string.Empty, string.Empty };

            // Loop through all permutations of 3 planets
            for (int i = 0; i < planets.Length; ++i)
            {
                for (int j = i + 1; j < planets.Length; ++j)
                {
                    for (int k = j + 1; k < planets.Length; ++k)
                    {
                        // Align the solution to the current selected planets
                        string[] curAlignment = new string[] { planets[i], planets[j], planets[k] };
                        FlattenAlignment(curAlignment, planetLocations);

                        // Measure how "flat" the solution is (lower is better)
                        double curQuality = Statistics.StandardDeviation(planetLocations.Values.Select(cur => cur.Location.Z));

                        // Keep the best solution as we go through all permutations
                        if (curQuality < resultQuality)
                        {
                            resultQuality = curQuality;
                            bestResult = planetLocations.Values.ToDictionary(cur => cur.Name, cur => cur.Location);
                            bestAlignment = curAlignment;
                        }
                    }
                }
            }

            //Console.WriteLine($"\r\nFlattest alignment: {{ {bestAlignment[0]}, {bestAlignment[1]}, {bestAlignment[2]} }} - Flatten Quality: {resultQuality:0.####}\r\n");

            return bestResult;
        }

        /// <summary>
        /// Aligns the solution using the specified planets
        /// </summary>
        /// <remarks>WARNING!!! 3D MATH!!!</remarks>
        /// <param name="alignment">The planets to align using.</param>
        /// <param name="planetLocations">The planet location data.</param>
        private void FlattenAlignment(string[] alignment, Dictionary<string, PlanetInfo> planetLocations)
        {
            // ** STEP ONE **
            // Translate everything such that the first alignment planet is at { 0, 0, 0 }
            Vector3D offset = planetLocations[alignment[0]].Location;

            if (offset.Length > 0)
            {
                foreach (PlanetInfo curInfo in planetLocations.Values)
                {
                    curInfo.Location -= offset;
                }
            }

            // ** STEP TWO **
            // Rotate everything such that the second alignment planet is on the x-axis (without translating the previous alignment planets)
            Vector3D xAxis = new Vector3D(1, 0, 0);

            Angle angle = planetLocations[alignment[1]].Location.AngleTo(xAxis);
            Vector3D rotationAxis = xAxis.CrossProduct(planetLocations[alignment[1]].Location);

            if (rotationAxis.Length > 0)
            {
                rotationAxis = rotationAxis.Normalize().ToVector3D();

                foreach (PlanetInfo curInfo in planetLocations.Values)
                {
                    curInfo.Location = curInfo.Location.Rotate(rotationAxis, -angle);
                }
            }

            // ** STEP THREE **
            // Rotate everything such that the third alignment planet is on the x/y-plane (without translating the previous alignment planets)
            Point3D planetOne = planetLocations[alignment[0]].Location.ToPoint3D();
            Point3D planetTwo = planetLocations[alignment[1]].Location.ToPoint3D();
            Point3D planetThree = planetLocations[alignment[2]].Location.ToPoint3D();

            Line3D oneTwoLine = new Line3D(planetOne, planetTwo);
            Line3D lineToThird = oneTwoLine.LineTo(planetThree, false);

            Vector3D vectorToThird = (lineToThird.EndPoint - lineToThird.StartPoint);
            Vector3D destLine = new Vector3D(vectorToThird.Y < 0 ? -vectorToThird.X : vectorToThird.X, vectorToThird.Y < 0 ? -vectorToThird.Y : vectorToThird.Y, 0);

            angle = destLine.AngleTo(vectorToThird);
            rotationAxis = destLine.CrossProduct(vectorToThird);

            if (rotationAxis.Length > 0)
            {
                rotationAxis = rotationAxis.Normalize().ToVector3D();

                foreach (PlanetInfo curInfo in planetLocations.Values)
                {
                    curInfo.Location = curInfo.Location.Rotate(rotationAxis, -angle);
                }
            }

            // ** STEP FOUR **
            // Invert the Z coordinates of all planets to ensure the first non-aligned planet has a positive Z location
            if (planetLocations.Values.First(cur => !alignment.Contains(cur.Name)).Location.Z < 0)
            {
                foreach (PlanetInfo curInfo in planetLocations.Values)
                {
                    curInfo.Location = new Vector3D(curInfo.Location.X, curInfo.Location.Y, -curInfo.Location.Z);
                }
            }

            // Just double check the alignment worked...
            foreach (string curAlignment in alignment)
            {
                if (Math.Round(planetLocations[curAlignment].Location.Z,4) != 0)
                {
                    throw new Exception("Alignment Failed!!!");
                }
            }
        }

        /// <summary>
        /// Writes a solution to the console
        /// </summary>
        /// <param name="solutionNumber">The solution number</param>
        /// <param name="planetLocations">The planet location data.</param>
        private void OutputSolution(int solutionNumber, double quality, Dictionary<string, Vector3D> planetLocations)
        {
            Console.WriteLine($"\rSolution Number: {solutionNumber}                               ");
            Console.WriteLine($"Solution Quality: {quality:0.##}");

            Console.WriteLine("\r\nPlanet,x,y,z");

            foreach (KeyValuePair<string, Vector3D> curInfo in planetLocations)
            {
                Console.WriteLine($"{curInfo.Key},{curInfo.Value.X:0.##},{curInfo.Value.Y:0.##},{curInfo.Value.Z:0.##}");
            }

            Console.WriteLine("");
        }
    }
}
