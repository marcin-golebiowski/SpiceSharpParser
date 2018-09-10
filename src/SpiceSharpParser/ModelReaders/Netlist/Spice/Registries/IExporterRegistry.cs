﻿using System.Collections.Generic;
using SpiceSharpParser.ModelReaders.Netlist.Spice.Readers.Controls.Exporters;

namespace SpiceSharpParser.ModelReaders.Netlist.Spice.Registries
{
    /// <summary>
    /// Interface for all exporter registries.
    /// </summary>
    public interface IExporterRegistry : IRegistry
    {
        /// <summary>
        /// Adds an exporter to registy.
        /// </summary>
        /// <param name="exporter">
        /// An exporter to add.
        /// </param>
        void Add(Exporter exporter, bool canOverride = false);

        /// <summary>
        /// Gets a value indicating whether a specified exporter is in registry.
        /// </summary>
        /// <param name="type">Type of exporter.</param>
        /// <returns>
        /// A value indicating whether a specified exporter is in registry.
        /// </returns>
        bool Supports(string type);

        /// <summary>
        /// Gets the exporter by type.
        /// </summary>
        /// <param name="type">Type of exporter.</param>
        /// <returns>
        /// A reference to exporter.
        /// </returns>
        Exporter Get(string type);

        IEnumerator<Exporter> GetEnumerator();
    }
}
