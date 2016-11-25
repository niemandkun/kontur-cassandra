﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using Benchmarks.Benchmarks;

namespace Benchmarks
{
    class BenchmarkFinder
    {
        public IEnumerable<BenchmarksFixture> GetBenchmarks(Assembly assembly)
        {
            var factory = new AttributeMethodMapper<BenchmarksFixture>();

            return GetTypesWith<BenchmarkClassAttribute>(assembly)
                .Select(Activator.CreateInstance)
                .Select(x => factory.CreateInstance(x));
        }

        IEnumerable<Type> GetTypesWith<TAttribute>(Assembly assembly)
            where TAttribute : Attribute
        {
            return assembly.ExportedTypes
                .Where(t => t.GetCustomAttribute<TAttribute>() != null)
                .Where(x => !x.IsAbstract && !x.IsInterface)
                .ToList();
        }
    }
}