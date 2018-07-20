﻿using System;
using System.Collections.Generic;
using SpiceSharp;
using SpiceSharp.Circuits;
using SpiceSharp.Simulations;
using SpiceSharpParser.Common;
using SpiceSharpParser.Models.Netlist.Spice.Objects;

namespace SpiceSharpParser.ModelsReaders.Netlist.Spice.Context
{
    /// <summary>
    /// Reading context.
    /// </summary>
    public class ReadingContext : IReadingContext
    {
        private static object locker = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadingContext"/> class.
        /// </summary>
        /// <param name="contextName">Name of the context.</param>
        /// <param name="readingEvaluator">Evaluator for the context.</param>
        /// <param name="resultService">Result service for the context.</param>
        /// <param name="nodeNameGenerator">Node name generator for the context.</param>
        /// <param name="objectNameGenerator">Object name generator for the context.</param>
        /// <param name="parent">Parent of th econtext.</param>
        public ReadingContext(
            string contextName,
            IEvaluator readingEvaluator,
            IResultService resultService,
            INodeNameGenerator nodeNameGenerator,
            IObjectNameGenerator objectNameGenerator,
            IReadingContext parent = null)
        {
            ContextName = contextName ?? throw new ArgumentNullException(nameof(contextName));
            Result = resultService ?? throw new ArgumentNullException(nameof(resultService));
            ReadingEvaluator = readingEvaluator ?? throw new ArgumentNullException(nameof(readingEvaluator));
            NodeNameGenerator = nodeNameGenerator ?? throw new ArgumentNullException(nameof(nodeNameGenerator));
            ObjectNameGenerator = objectNameGenerator ?? throw new ArgumentNullException(nameof(objectNameGenerator));
            Parent = parent;

            if (Parent != null)
            {
                AvailableSubcircuits = new List<SubCircuit>(Parent.AvailableSubcircuits);
                SimulationEvaluators = new Dictionary<Simulation, IEvaluator>(Parent.SimulationEvaluators);
            }
            else
            {
                AvailableSubcircuits = new List<SubCircuit>();
                SimulationEvaluators = new Dictionary<Simulation, IEvaluator>();
            }

            Children = new List<IReadingContext>();
        }

        /// <summary>
        /// Gets or sets the name of context.
        /// </summary>
        public string ContextName { get; protected set; }

        /// <summary>
        /// Gets or sets the reading evaluator.
        /// </summary>
        public IEvaluator ReadingEvaluator { get; protected set; }

        /// <summary>
        /// Gets or sets evaluators.
        /// </summary>
        public IDictionary<Simulation, IEvaluator> SimulationEvaluators { get; protected set; }

        /// <summary>
        /// Gets or sets the parent of context.
        /// </summary>
        public IReadingContext Parent { get; protected set; }

        /// <summary>
        /// Gets available subcircuits in context.
        /// </summary>
        public ICollection<SubCircuit> AvailableSubcircuits { get; }

        /// <summary>
        /// Gets or sets the result service.
        /// </summary>
        public IResultService Result { get; protected set; }

        /// <summary>
        /// Gets or sets the node name generator.
        /// </summary>
        public INodeNameGenerator NodeNameGenerator { get; protected set; }

        /// <summary>
        /// Gets or sets the object name generator.
        /// </summary>
        public IObjectNameGenerator ObjectNameGenerator { get; protected set; }

        /// <summary>
        /// Gets or sets the children of the reading context.
        /// </summary>
        public ICollection<IReadingContext> Children { get; protected set; }

        /// <summary>
        /// Sets voltage initial condition for node.
        /// </summary>
        /// <param name="nodeName">Name of node.</param>
        /// <param name="expression">Expression.</param>
        public void SetICVoltage(string nodeName, string expression)
        {
            var fullNodeName = NodeNameGenerator.Generate(nodeName);
            var initialValue = ReadingEvaluator.EvaluateDouble(expression);

            Result.SetInitialVoltageCondition(fullNodeName, initialValue);

            ReadingEvaluator.AddAction("ICV - " + nodeName, expression, (simulation, value) => Result.SetInitialVoltageCondition(nodeName, value));
        }

        /// <summary>
        /// Sets voltage guess condition for node.
        /// </summary>
        /// <param name="nodeName">Name of node.</param>
        /// <param name="expression">Expression.</param>
        public void SetNodeSetVoltage(string nodeName, string expression)
        {
            foreach (var simulation in Result.Simulations)
            {
                simulation.Nodes.NodeSets[nodeName] = ReadingEvaluator.EvaluateDouble(expression);
            }
        }

        /// <summary>
        /// Parses an expression to double.
        /// </summary>
        /// <param name="expression">Expression to parse.</param>
        /// <returns>
        /// A value of expression..
        /// </returns>
        public double ParseDouble(string expression)
        {
            try
            {
                return ReadingEvaluator.EvaluateDouble(expression);
            }
            catch (Exception ex)
            {
                throw new Exception("Exception during evaluation of expression: " + expression, ex);
            }
        }

        /// <summary>
        /// Sets the parameter of entity and enables updates.
        /// </summary>
        /// <param name="entity">An entity of parameter.</param>
        /// <param name="parameterName">A parameter name.</param>
        /// <param name="expression">An expression.</param>
        /// <returns>
        /// True if the parameter has been set.
        /// </returns>
        public bool SetEntityParameter(Entity entity, string parameterName, string expression, Simulation simulation = null)
        {
            var evaluator = simulation != null ? GetSimulationEvaluator(simulation) : ReadingEvaluator;

            double value;
            try
            {
                value = evaluator.EvaluateDouble(expression);
            }
            catch (Exception ex)
            {
                Result.AddWarning("Exception during parsing expression '" + expression + "': " + ex);
                return false;
            }

            bool wasSet = false;
            if (simulation != null)
            {
                simulation.EntityParameters.GetEntityParameters(entity.Name).GetParameter(parameterName.ToLower()).Value = value;
                wasSet = true;
            }
            else
            {
                wasSet = entity.SetParameter(parameterName.ToLower(), value);
            }

            if (wasSet)
            {
                Action<Simulation, double> propertySetter = (Simulation simulationParam, double newValue) =>
                {
                    simulationParam.EntityParameters.GetEntityParameters(entity.Name).GetParameter(parameterName.ToLower()).Value = newValue;
                };

                // re-evaluation makes sense only if there is a setter
                if (propertySetter != null)
                {
                    evaluator.AddAction(entity.Name + "-" + parameterName.ToLower(), expression, propertySetter);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the parameter of entity.
        /// </summary>
        /// <param name="entity">An entity of parameter.</param>
        /// <param name="parameterName">A parameter name.</param>
        /// <param name="object">An parameter value.</param>
        /// <returns>
        /// True if the parameter has been set.
        /// </returns>
        public bool SetParameter(Entity entity, string parameterName, object @object)
        {
            return entity.SetParameter(parameterName.ToLower(), @object);
        }

        /// <summary>
        /// Finds model in the context and in parent contexts.
        /// </summary>
        /// <param name="modelName">Name of model to find.</param>
        /// <returns>
        /// A reference to model.
        /// </returns>
        public T FindModel<T>(string modelName)
            where T : Entity
        {
            IReadingContext context = this;
            while (context != null)
            {
                var modelNameToSearch = context.ObjectNameGenerator.Generate(modelName);

                Entity model;
                if (Result.FindObject(modelNameToSearch, out model))
                {
                    return (T)model;
                }

                context = context.Parent;
            }

            return null;
        }

        /// <summary>
        /// Creates nodes for a component.
        /// </summary>
        /// <param name="component">A component.</param>
        /// <param name="parameters">Parameters of component.</param>
        public void CreateNodes(SpiceSharp.Components.Component component, ParameterCollection parameters)
        {
            Identifier[] nodes = new Identifier[component.PinCount];
            for (var i = 0; i < component.PinCount; i++)
            {
                string pinName = parameters.GetString(i);
                nodes[i] = NodeNameGenerator.Generate(pinName);
            }

            component.Connect(nodes);
        }

        /// <summary>
        /// Gets the simulation evaluator.
        /// </summary>
        public IEvaluator GetSimulationEvaluator(Simulation simulation)
        {
            IReadingContext context = this;
            while (context.Parent != null)
            {
                context = context.Parent;
            }

            lock (locker)
            {
                if (context.SimulationEvaluators.ContainsKey(simulation))
                {
                    return context.SimulationEvaluators[simulation];
                }
            }
            throw new Exception("Missing a simulation evaluator");
        }

        /// <summary>
        /// Creates simulation evaluator.
        /// </summary>
        public void CreateSimulationEvaluator(Simulation simulation, string name)
        {
            lock (locker)
            {
                if (SimulationEvaluators.ContainsKey(simulation))
                {
                    throw new Exception("There is already a simulation evaluator");
                }

                SimulationEvaluators[simulation] = ReadingEvaluator.CreateClonedEvaluator(name);
                SimulationEvaluators[simulation].Simulation = simulation;
            }
        }

        /// <summary>
        /// Ensures that simulation evaluator is created.
        /// </summary>
        public void EnsureSimulationEvaluator(Simulation simulation, string name)
        {
            lock (locker)
            {
                if (SimulationEvaluators.ContainsKey(simulation) == false)
                {
                    SimulationEvaluators[simulation] = ReadingEvaluator.CreateClonedEvaluator(name);
                    SimulationEvaluators[simulation].Simulation = simulation;
                }
            }
        }
    }
}
