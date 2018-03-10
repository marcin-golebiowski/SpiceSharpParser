﻿using System;
using System.Collections.Generic;
using System.Linq;
using SpiceNetlist.SpiceObjects;
using SpiceNetlist.SpiceSharpConnector.Processors.Evaluation;
using SpiceSharp;
using SpiceSharp.Simulations;

namespace SpiceNetlist.SpiceSharpConnector.Processors.Controls.Simulations
{
    /// <summary>
    /// Processes .DC <see cref="Control"/> from spice netlist object model.
    /// </summary>
    public class DCControl : SimulationControl
    {
        public override string TypeName => "dc";

        /// <summary>
        /// Processes <see cref="Control"/> statement and modifies the context
        /// </summary>
        /// <param name="statement">A statement to process</param>
        /// <param name="context">A context to modify</param>
        public override void Process(Control statement, ProcessingContext context)
        {
            int count = statement.Parameters.Count / 4;
            switch (statement.Parameters.Count - (4 * count))
            {
                case 0:
                    if (statement.Parameters.Count == 0)
                    {
                        throw new Exception("Source st.Name expected");
                    }

                    break;

                case 1: throw new Exception("Start value expected");
                case 2: throw new Exception("Stop value expected");
                case 3: throw new Exception("Step value expected");
            }

            // Format: .DC SRCNAM VSTART VSTOP VINCR [SRC2 START2 STOP2 INCR2]
            List<SweepConfiguration> sweeps = new List<SweepConfiguration>();

            for (int i = 0; i < count; i++)
            {
                SweepConfiguration sweep = new SweepConfiguration(
                    statement.Parameters.GetString(4 * i).ToLower(),
                    context.ParseDouble(statement.Parameters.GetString((4 * i) + 1).ToLower()),
                    context.ParseDouble(statement.Parameters.GetString((4 * i) + 2).ToLower()),
                    context.ParseDouble(statement.Parameters.GetString((4 * i) + 3).ToLower()));

                sweeps.Add(sweep);
            }

            Dc dc = new Dc((context.Simulations.Count() + 1) + " - DC", sweeps);
            dc.OnParameterSearch += (sender, e) => {
                string sweepParameterName = e.Name.Name;
                if (context.AvailableParameters.ContainsKey(sweepParameterName))
                {
                    e.Result = new DynamicParameter(context.Evaluator, sweepParameterName);
                }
            };

            SetBaseParameters(dc.BaseConfiguration, context);
            SetDcParameters(dc.DcConfiguration, context);

            context.AddSimulation(dc);
        }

        private void SetDcParameters(DcConfiguration dCConfiguration, ProcessingContext context)
        {
            if (context.SimulationConfiguration.SweepMaxIterations.HasValue)
            {
                dCConfiguration.SweepMaxIterations = context.SimulationConfiguration.SweepMaxIterations.Value;
            }
        }
    }

    // prototype
    public class DynamicParameter : SpiceSharp.Parameter
    {
        public Evaluator Evaluator { get; }

        public string SweepParameterName { get; }

        public DynamicParameter(Evaluator evaluator, string sweepParameterName)
        {
            SweepParameterName = sweepParameterName;
            Evaluator = evaluator;
        }

        public override object Clone()
        {
            return base.Clone();
        }

        public override void CopyFrom(SpiceSharp.Parameter source)
        {
            base.CopyFrom(source);
        }

        public override void CopyTo(SpiceSharp.Parameter target)
        {
            base.CopyTo(target);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override void Set(double value)
        {
            base.Set(value);

            Evaluator.Parameters[SweepParameterName] = value;
            Evaluator.Refresh();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
