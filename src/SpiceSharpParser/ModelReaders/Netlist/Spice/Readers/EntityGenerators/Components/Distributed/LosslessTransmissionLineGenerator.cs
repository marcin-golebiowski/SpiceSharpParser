﻿using System.Collections.Generic;
using SpiceSharp.Components;
using SpiceSharpParser.Common.Evaluation;
using SpiceSharpParser.ModelReaders.Netlist.Spice.Context;
using SpiceSharpParser.Models.Netlist.Spice.Objects;
using SpiceSharpParser.Models.Netlist.Spice.Objects.Parameters;

namespace SpiceSharpParser.ModelReaders.Netlist.Spice.Readers.EntityGenerators.Components.Distributed
{
    public class LosslessTransmissionLineGenerator : ComponentGenerator
    {
        public override IEnumerable<string> GeneratedTypes => new List<string> { "t" };

        public override SpiceSharp.Components.Component Generate(string name, string originalName, string type, ParameterCollection parameters, IReadingContext context)
        {
            var losslessLine = new LosslessTransmissionLine(name);
            context.CreateNodes(losslessLine, parameters);

            parameters = parameters.Skip(4);

            foreach (Parameter parameter in parameters)
            {
                if (parameter is AssignmentParameter ap)
                {
                    var paramName = ap.Name.ToLower();

                    if (paramName == "z0" || paramName == "zo")
                    {
                        context.SetParameter(losslessLine, "z0", ap.Value);
                    }
                    else if (paramName == "f")
                    {
                        context.SetParameter(losslessLine, "f", ap.Value);
                    }
                    else if (paramName == "td")
                    {
                        context.SetParameter(losslessLine, "td", ap.Value);
                    }
                    else if (paramName == "reltol")
                    {
                        context.SetParameter(losslessLine, "reltol", ap.Value);
                    }
                    else if (paramName == "abstol")
                    {
                        context.SetParameter(losslessLine, "abstol", ap.Value);
                    }
                    else
                    {
                        throw new UnknownParameterException() { Name = paramName };
                    }
                }
            }

            return losslessLine;
        }
    }
}