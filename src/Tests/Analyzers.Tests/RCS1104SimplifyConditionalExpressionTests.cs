﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslynator.CSharp;
using Roslynator.CSharp.Analysis;
using Roslynator.CSharp.CodeFixes;
using Roslynator.Tests.CSharp;
using Xunit;

#pragma warning disable RCS1090

namespace Roslynator.Analyzers.Tests
{
    public class RCS1104SimplifyConditionalExpressionTests : CSharpCodeFixVerifier
    {
        public override DiagnosticDescriptor Descriptor { get; } = DiagnosticDescriptors.SimplifyConditionalExpression;

        public override DiagnosticAnalyzer Analyzer { get; } = new SimplifyConditionalExpressionAnalyzer();

        public override CodeFixProvider FixProvider { get; } = new ConditionalExpressionCodeFixProvider();

        [Theory]
        [InlineData("f ? true : false", "f")]
        [InlineData("!f ? false : true", "f")]
        [InlineData("((f)) ? ((true)) : ((false))", "f")]
        [InlineData("f ? false : true", "!f")]
        [InlineData("f == g ? false : true", "f != g")]
        [InlineData("f != g ? false : true", "f == g")]

        [InlineData(@"f
            ? true
            : false", "f")]

        [InlineData(@"[|f //a
              /*b*/ ? /*c*/ true //d
                                 /*e*/ : /*f*/ false|] /*g*/", @"f //a
              /*b*/  /*c*/  //d
                                 /*e*/  /*f*/  /*g*/")]
        public async Task TestDiagnosticWithCodeFix(string fixableCode, string fixedCode)
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M(bool f, bool g)
    {
        if ([||]) { }
}
}
", fixableCode, fixedCode);
        }

        [Theory]
        [InlineData("f ? g : false", "f && g")]
        [InlineData(@"[|f
            ? g
            : false|] /**/", @"f
            && g /**/")]
        public async Task TestDiagnosticWithCodeFix_LogicalAnd(string fixableCode, string fixedCode)
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M(bool f, bool g)
    {
        if ([||]) { }
    }
}
", fixableCode, fixedCode);
        }

        [Theory]
        [InlineData("f ? true : g", "f || g")]
        [InlineData(@"[|f
            ? true
            : g|] /**/", @"f
            || g /**/")]
        public async Task TestDiagnosticWithCodeFix_LogicalOr(string fixableCode, string fixedCode)
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M(bool f, bool g)
    {
        if ([||]) { }
    }
}
", fixableCode, fixedCode);
        }

        [Fact]
        public async Task TestNoDiagnostic()
        {
            await VerifyNoDiagnosticAsync(@"
class C
{
    void M(bool f, bool g, bool h)
    {
        if ((f) ? g : h) { }
        if ((f) ? false : g) { }
        if ((f) ? g : true) { }

        if ((f)
#if DEBUG
                ? true
            : false;
#else
                ? false
                : true) { }
#endif
    }
}
");
        }
    }
}
