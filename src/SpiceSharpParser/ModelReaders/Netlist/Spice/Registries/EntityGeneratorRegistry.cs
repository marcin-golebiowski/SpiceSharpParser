﻿using System;
using SpiceSharpParser.ModelReaders.Netlist.Spice.Readers.EntityGenerators;

namespace SpiceSharpParser.ModelReaders.Netlist.Spice.Registries
{
    /// <summary>
    /// Registry for <see cref="EntityGenerator"/>s
    /// </summary>
    public class EntityGeneratorRegistry : BaseRegistry<EntityGenerator>, IEntityGeneratorRegistry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityGeneratorRegistry"/> class.
        /// </summary>
        public EntityGeneratorRegistry()
        {
        }

        /// <summary>
        /// Adds generator to the registry (all generated types)
        /// </summary>
        /// <param name="generator">
        /// A generator to add
        /// </param>
        public override void Add(EntityGenerator generator, bool canOverride = false)
        {
            foreach (var type in generator.GetGeneratedSpiceTypes())
            {
                if (ElementsByType.ContainsKey(type) && canOverride == false)
                {
                    throw new Exception("There is a generator for : " + type);
                }

                ElementsByType[type] = generator;
            }

            Elements.Add(generator);
        }
    }
}
