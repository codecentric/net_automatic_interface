﻿using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AutomaticInterface;

internal static class RegisterAttributesExtensions
{
    public static IncrementalGeneratorInitializationContext RegisterDefaultAttribute(
        this IncrementalGeneratorInitializationContext context
    )
    {
        context.RegisterPostInitializationOutput(static postInitializationContext =>
        {
            postInitializationContext.AddSource(
                $"{AutomaticInterfaceGenerator.DefaultAttributeName}.Attribute.g.cs",
                SourceText.From(
                    $$$"""
                    // <auto-generated />
                    using System;

                    namespace AutomaticInterface
                    {
                        /// <summary>
                        /// Use source generator to automatically create a Interface from this class
                        /// </summary>
                        [AttributeUsage(AttributeTargets.Class)]
                        internal sealed class {{{AutomaticInterfaceGenerator.DefaultAttributeName}}}Attribute : Attribute
                        {
                            internal {{{AutomaticInterfaceGenerator.DefaultAttributeName}}}Attribute(string namespaceName = "", bool asInternal = false) { }
                        }
                    }
                    """,
                    Encoding.UTF8
                )
            );
        });
        return context;
    }

    public static IncrementalGeneratorInitializationContext RegisterIgnoreAttribute(
        this IncrementalGeneratorInitializationContext context
    )
    {
        context.RegisterPostInitializationOutput(static postInitializationContext =>
        {
            postInitializationContext.AddSource(
                $"{AutomaticInterfaceGenerator.IgnoreAutomaticInterfaceAttributeName}.Attribute.g.cs",
                SourceText.From(
                    $$$"""
                    // <auto-generated />
                    using System;

                    namespace AutomaticInterface
                    {
                        /// <summary>
                        /// Ignore this member in a generated Interface from this class
                        /// </summary>
                        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event)]
                        internal sealed class {{{AutomaticInterfaceGenerator.IgnoreAutomaticInterfaceAttributeName}}}Attribute : Attribute
                        {
                            internal {{{AutomaticInterfaceGenerator.IgnoreAutomaticInterfaceAttributeName}}}Attribute() { }
                        }
                    }
                    """,
                    Encoding.UTF8
                )
            );
        });
        return context;
    }
}
