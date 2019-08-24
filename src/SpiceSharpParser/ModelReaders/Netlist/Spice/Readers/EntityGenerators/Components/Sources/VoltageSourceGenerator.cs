﻿using System;
using System.Collections.Generic;
using System.Linq;
using SpiceSharp.Circuits;
using SpiceSharp.Components;
using SpiceSharpBehavioral.Components.BehavioralBehaviors;
using SpiceSharpBehavioral.Parsers;
using SpiceSharpParser.Common.Evaluation;
using SpiceSharpParser.ModelReaders.Netlist.Spice.Context;
using SpiceSharpParser.ModelReaders.Netlist.Spice.Custom;
using SpiceSharpParser.ModelReaders.Netlist.Spice.Exceptions;
using SpiceSharpParser.ModelReaders.Netlist.Spice.Readers.Controls.Exporters.VoltageExports;
using SpiceSharpParser.Models.Netlist.Spice.Objects;
using SpiceSharpParser.Models.Netlist.Spice.Objects.Parameters;
using Component = SpiceSharp.Components.Component;

namespace SpiceSharpParser.ModelReaders.Netlist.Spice.Readers.EntityGenerators.Components.Sources
{
    /// <summary>
    /// Voltage sources generator.
    /// </summary>
    public class VoltageSourceGenerator : SourceGenerator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VoltageSourceGenerator"/> class.
        /// </summary>
        public VoltageSourceGenerator()
        {
        }

        /// <summary>
        /// Gets generated types.
        /// </summary>
        /// <returns>
        /// Generated types.
        /// </returns>
        public override IEnumerable<string> GeneratedTypes => new List<string>() { "V", "H", "E" };

        public override SpiceSharp.Components.Component Generate(string componentIdentifier, string originalName, string type, ParameterCollection parameters, IReadingContext context)
        {
            switch (type.ToLower())
            {
                case "v": return GenerateVoltageSource(componentIdentifier, parameters, context);
                case "h": return GenerateCurrentControlledVoltageSource(componentIdentifier, parameters, context);
                case "e": return GenerateVoltageControlledVoltageSource(componentIdentifier, parameters, context);
            }

            return null;
        }

        /// <summary>
        /// Generates new voltage controlled voltage source: EName.
        /// </summary>
        /// <param name="name">The name of voltage source to generate.</param>
        /// <param name="parameters">The parameters for voltage source.</param>
        /// <param name="context">The reading context.</param>
        /// <returns>
        /// A new instance of voltage controlled voltage source.
        /// </returns>
        protected SpiceSharp.Components.Component GenerateVoltageControlledVoltageSource(
            string name,
            ParameterCollection parameters,
            IReadingContext context)
        {
            if (parameters.Count == 5
                && parameters.IsValueString(0)
                && parameters.IsValueString(1)
                && parameters.IsValueString(2)
                && parameters.IsValueString(3)
                && parameters.IsValueString(4))
            {
                var vcvs = new VoltageControlledVoltageSource(name);
                context.CreateNodes(vcvs, parameters);
                context.SetParameter(vcvs, "gain", parameters.GetValueString(4));

                return vcvs;
            }
            else
            {
                if (parameters.Count == 3
                    && parameters[0] is PointParameter pp1
                    && pp1.Values.Count() == 2
                    && parameters[1] is PointParameter pp2
                    && pp2.Values.Count() == 2)
                {
                    var vcvsNodes = new ParameterCollection();
                    vcvsNodes.Add(pp1.Values.Items[0]);
                    vcvsNodes.Add(pp1.Values.Items[1]);
                    vcvsNodes.Add(pp2.Values.Items[0]);
                    vcvsNodes.Add(pp2.Values.Items[1]);

                    var vcvs = new VoltageControlledVoltageSource(name);
                    context.CreateNodes(vcvs, vcvsNodes);
                    context.SetParameter(vcvs, "gain", parameters.GetString(2));
                    return vcvs;
                }
                else
                {
                    return CreateCustomVoltageSource(name, parameters, context, true);
                }
            }
        }

        /// <summary>
        /// Generates new current controlled voltage source HName.
        /// </summary>
        /// <param name="name">The name of voltage source to generate.</param>
        /// <param name="parameters">The parameters for voltage source.</param>
        /// <param name="context">The reading context.</param>
        /// <returns>
        /// A new instance of current controlled voltage source.
        /// </returns>
        protected SpiceSharp.Components.Component GenerateCurrentControlledVoltageSource(
            string name,
            ParameterCollection parameters,
            IReadingContext context)
        {
            if (parameters.Count == 4
                && parameters.IsValueString(0)
                && parameters.IsValueString(1)
                && parameters.IsValueString(2) && parameters[2].Image.ToLower() != "value"
                && parameters.IsValueString(3))
            {
                var ccvs = new CurrentControlledVoltageSource(name);
                context.CreateNodes(ccvs, parameters);
                ccvs.ControllingName = context.ComponentNameGenerator.Generate(parameters.GetString(2));
                context.SetParameter(ccvs, "gain", parameters.GetString(3));
                return ccvs;
            }
            else
            {
                return CreateCustomVoltageSource(name, parameters, context, false);
            }
        }

        /// <summary>
        /// Generates new voltage source.
        /// </summary>
        /// <param name="name">The name of voltage source to generate.</param>
        /// <param name="parameters">The parameters for voltage source.</param>
        /// <param name="context">The reading context.</param>
        /// <returns>
        /// A new instance of voltage source.
        /// </returns>
        protected SpiceSharp.Components.Component GenerateVoltageSource(string name, ParameterCollection parameters, IReadingContext context)
        {
            var vs = new VoltageSource(name);
            context.CreateNodes(vs, parameters);
            SetSourceParameters(name, parameters, context, vs);
            return vs;
        }

        protected Component CreateCustomVoltageSource(
            string name,
            ParameterCollection parameters,
            IReadingContext context,
            bool isVoltageControlled)
        {
            var parser = CreateParser(context);

            if (parameters.Any(p => p is AssignmentParameter ap && ap.Name.ToLower() == "value"))
            {
                var entity = new BehavioralVoltageSource(name);
                context.CreateNodes(entity, parameters);
                entity.SetParameter("parser", parser);

                var baseParameters = entity.ParameterSets.Get<BaseParameters>();
                var valueParameter = (AssignmentParameter)parameters.Single(p => p is AssignmentParameter ap && ap.Name.ToLower() == "value");
                baseParameters.Expression = valueParameter.Value;

                return entity;
            }

            if (parameters.Any(p => p is WordParameter ap && ap.Image.ToLower() == "value"))
            {
                var entity = new BehavioralVoltageSource(name);
                entity.SetParameter("parser", parser);
                context.CreateNodes(entity, parameters);
                var baseParameters = entity.ParameterSets.Get<BaseParameters>();
                var expressionParameter = parameters.FirstOrDefault(p => p is ExpressionParameter);
                if (expressionParameter != null)
                {
                    baseParameters.Expression = expressionParameter.Image;
                }

                return entity;
            }

            if (parameters.Any(p => p is WordParameter bp && bp.Image.ToLower() == "poly"))
            {
                var entity = new VoltageSource(name);
                context.CreateNodes(entity, parameters);
                parameters = parameters.Skip(VoltageSource.VoltageSourcePinCount);
                var dimension = 1;
                var expression = CreatePolyExpression(dimension, parameters.Skip(1), isVoltageControlled);
                context.SetParameter(entity, "dc", expression);

                return entity;
            }

            if (parameters.Any(p => p is BracketParameter bp && bp.Name.ToLower() == "poly"))
            {
                var polyParameter = (BracketParameter)parameters.Single(p => p is BracketParameter bp && bp.Name.ToLower() == "poly");

                if (polyParameter.Parameters.Count != 1)
                {
                    throw new WrongParametersCountException(name, "poly expects one argument => dimension");
                }

                var entity = new VoltageSource(name);
                context.CreateNodes(entity, parameters);
                parameters = parameters.Skip(VoltageSource.VoltageSourcePinCount);
                var dimension = (int)context.EvaluateDouble(polyParameter.Parameters[0].Image);
                var expression = CreatePolyExpression(dimension, parameters.Skip(1), isVoltageControlled);
                context.SetParameter(entity, "dc", expression);
                return entity;
            }

            var tableParameter = parameters.FirstOrDefault(p => p.Image.ToLower() == "table");
            if (tableParameter != null)
            {
                int tableParameterPosition = parameters.IndexOf(tableParameter);
                if (tableParameterPosition == parameters.Count - 1)
                {
                    throw new WrongParametersCountException(name, "table expects expression parameter");
                }

                var nextParameter = parameters[tableParameterPosition + 1];

                if (nextParameter is ExpressionEqualParameter eep)
                {
                    var entity = new VoltageSource(name);
                    context.CreateNodes(entity, parameters);
                    var tableParameterName = name + "_table_variable";
                    context.SetParameter(tableParameterName, eep.Expression);
                    string expression = ExpressionFactory.CreateTableExpression(tableParameterName, eep.Points);
                    context.SetParameter(entity, "dc", expression);
                    return entity;
                }
                else
                {
                    throw new WrongParameterTypeException(name, "table expects expression equal parameter");
                }
            }

            return null;
        }
    }
}
