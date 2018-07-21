﻿using System.Collections.Generic;
using SpiceSharp.Simulations;
using SpiceSharpParser.Models.Netlist.Spice.Objects;
using SpiceSharpParser.ModelsReaders.Netlist.Spice.Context;

namespace SpiceSharpParser.ModelReaders.Netlist.Spice.Context
{
    public interface IParameterSweepUpdater
    {
        /// <summary>
        /// Sets sweep parameters for the simulation.
        /// </summary>
        /// <param name="simulation">Simulation to set.</param>
        /// <param name="context">Reading context.</param>
        /// <param name="parameterValues">Parameter values.</param>
        void Update(BaseSimulation simulation, IReadingContext context, List<KeyValuePair<Parameter, double>> parameterValues);
    }
}