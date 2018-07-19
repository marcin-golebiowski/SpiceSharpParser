﻿using System.Collections.Generic;
using SpiceSharp.Circuits;
using SpiceSharp.Simulations;
using SpiceSharpParser.Common;
using SpiceSharpParser.Models.Netlist.Spice.Objects;

namespace SpiceSharpParser.ModelsReaders.Netlist.Spice.Context
{
    public interface IReadingContext
    {
        /// <summary>
        /// Gets the context name.
        /// </summary>
        string ContextName { get; }

        /// <summary>
        /// Gets the parent of the context.
        /// </summary>
        IReadingContext Parent { get;  }

        /// <summary>
        /// Gets the children of the processing context.
        /// </summary>
        ICollection<IReadingContext> Children { get; }

        /// <summary>
        /// Gets the list of available subcircuit for the context.
        /// </summary>
        ICollection<SubCircuit> AvailableSubcircuits { get; }

        /// <summary>
        /// Gets the result service for the context.
        /// </summary>
        IResultService Result { get; }

        /// <summary>
        /// Gets the node name generator.
        /// </summary>
        INodeNameGenerator NodeNameGenerator { get; }

        /// <summary>
        /// Gets the object name generator.
        /// </summary>
        IObjectNameGenerator ObjectNameGenerator { get; }

        /// <summary>
        /// Gets the evaluator for the reading of the model.
        /// </summary>
        IEvaluator ReadingEvaluator { get; }

        /// <summary>
        /// Gets the dictionary of evaluators for simulations.
        /// </summary>
        IDictionary<Simulation, IEvaluator> SimulationEvaluators { get; }

        /// <summary>
        /// Parses an expression to double.
        /// </summary>
        /// <param name="expression">Expression to parse</param>
        /// <returns>
        /// A value of expression.
        /// </returns>
        double ParseDouble(string expression);

        /// <summary>
        /// Sets voltage initial condition for node
        /// </summary>
        /// <param name="nodeName">Name of node</param>
        /// <param name="expression">Expression</param>
        void SetICVoltage(string nodeName, string expression);

        /// <summary>
        /// Sets voltage guess condition for node.
        /// </summary>
        /// <param name="nodeName">Name of node</param>
        /// <param name="expression">Expression</param>
        void SetNodeSetVoltage(string nodeName, string expression);

        /// <summary>
        /// Sets the parameter of entity and enables updates.
        /// </summary>
        /// <param name="entity">An entity of parameter</param>
        /// <param name="parameterName">A parameter name</param>
        /// <param name="expression">An expression</param>
        /// <returns>
        /// True if parameter has been set
        /// </returns>
        bool SetEntityParameter(Entity entity, string parameterName, string expression, Simulation sim = null);

        /// <summary>
        /// Sets the parameter of entity.
        /// </summary>
        /// <param name="entity">An entity of parameter</param>
        /// <param name="parameterName">A parameter name</param>
        /// <param name="object">An parameter value</param>
        /// <returns>
        /// True if the parameter has been set.
        /// </returns>
        bool SetParameter(Entity entity, string parameterName, object @object);

        /// <summary>
        /// Finds model in the context and in parent contexts.
        /// </summary>
        /// <param name="modelName">Name of model to find</param>
        /// <returns>
        /// A reference to model.
        /// </returns>
        T FindModel<T>(string modelName)
            where T : Entity;

        /// <summary>
        /// Creates nodes for a component.
        /// </summary>
        /// <param name="component">A component</param>
        /// <param name="parameters">Parameters of component</param>
        void CreateNodes(SpiceSharp.Components.Component component, ParameterCollection parameters);

        /// <summary>
        /// Gets the evaluator for the simulation with given name.
        /// </summary>
        /// <param name="simulation">Simulation.</param>
        IEvaluator GetSimulationEvaluator(Simulation simulation);

        /// <summary>
        /// Creates a simulation evaluator.
        /// </summary>
        /// <param name="simulation">A simulation.</param>
        /// <param name="name">A name of evaluator.</param>
        void CreateSimulationEvaluator(Simulation simulation, string name);

        /// <summary>
        /// Creates simulation evaluator if it's not created.
        /// </summary>
        /// <param name="simulation">A simulation.</param>
        /// <param name="name">A name of evaluator.</param>
        void EnsureSimulationEvaluator(Simulation simulation, string name);
    }
}
