﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Roslynator.Metadata;
using System.Collections.Generic;

namespace Roslynator.CodeGeneration
{
    public class Generator
    {
        private ImmutableArray<AnalyzerDescriptor> _analyzers;
        private ImmutableArray<RefactoringDescriptor> _refactorings;
        private ImmutableArray<CodeFixDescriptor> _codeFixes;
        private ImmutableArray<CompilerDiagnosticDescriptor> _compilerDiagnostics;

        public Generator(string rootPath, StringComparer comparer = null)
        {
            RootPath = rootPath;
            Comparer = comparer ?? StringComparer.CurrentCulture;
        }

        public string RootPath { get; }

        public StringComparer Comparer { get; }

        public ImmutableArray<AnalyzerDescriptor> Analyzers
        {
            get
            {
                if (_analyzers.IsDefault)
                {
                    //TODO: fix bug RCS1207 "Use method group"
                    IEnumerable<AnalyzerDescriptor> analyzers = Directory
                        .EnumerateFiles(GetPath("Analyzers"), "Analyzers.*.xml", SearchOption.TopDirectoryOnly)
                        .Where(filePath => Path.GetFileName(filePath) != "Analyzers.Template.xml")
                        .SelectMany(filePath => MetadataFile.ReadAllAnalyzers(filePath));

                    _analyzers = MetadataFile.ReadAllAnalyzers(GetPath(@"Analyzers\Analyzers.xml")).AddRange(analyzers);
                }

                return _analyzers;
            }
        }

        public ImmutableArray<RefactoringDescriptor> Refactorings
        {
            get
            {
                if (_refactorings.IsDefault)
                    _refactorings = MetadataFile.ReadAllRefactorings(GetPath(@"Refactorings\Refactorings.xml"));

                return _refactorings;
            }
        }

        public ImmutableArray<CodeFixDescriptor> CodeFixes
        {
            get
            {
                if (_codeFixes.IsDefault)
                    _codeFixes = MetadataFile.ReadAllCodeFixes(GetPath(@"CodeFixes\CodeFixes.xml"));

                return _codeFixes;
            }
        }

        public ImmutableArray<CompilerDiagnosticDescriptor> CompilerDiagnostics
        {
            get
            {
                if (_compilerDiagnostics.IsDefault)
                    _compilerDiagnostics = MetadataFile.ReadAllCompilerDiagnostics(GetPath(@"CodeFixes\Diagnostics.xml"));

                return _compilerDiagnostics;
            }
        }

        public string GetPath(string relativePath)
        {
            return Path.Combine(RootPath, relativePath);
        }
    }
}
