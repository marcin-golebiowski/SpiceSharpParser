﻿using SpiceSharpParser.ModelReaders.Netlist.Spice.Evaluation;

namespace SpiceSharpParser.ModelReaders.Netlist.Spice
{
    public class SpiceNetlistReaderSettings
    {
        public SpiceNetlistReaderSettings()
        {
            EvaluatorMode = SpiceEvaluatorMode.Spice3f5;
            Mappings = new SpiceObjectMappings();
            Orderer = new SpiceStatementsOrderer();
        }

        /// <summary>
        /// Gets or sets the evaluator mode.
        /// </summary>
        public SpiceEvaluatorMode EvaluatorMode { get; set; }

        /// <summary>
        /// Gets or sets the evaluator random seed.
        /// </summary>
        public int? Seed { get; set; }

        /// <summary>
        /// Gets or sets the entity registry.
        /// </summary>
        public ISpiceObjectMappings Mappings { get; set; }

        /// <summary>
        /// Gets or sets the statements orderer.
        /// </summary>
        public ISpiceStatementsOrderer Orderer { get; set; }
    }
}
