﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslynator.CSharp.CodeFixes;
using Xunit;

#pragma warning disable RCS1090

namespace Roslynator.CSharp.Analysis.Tests
{
    public class RCS1240MakeClassSealedTests : AbstractCSharpCodeFixVerifier
    {
        public override DiagnosticDescriptor Descriptor { get; } = DiagnosticDescriptors.MakeClassSealed;

        public override DiagnosticAnalyzer Analyzer { get; } = new MakeClassSealedAnalyzer();

        public override CodeFixProvider FixProvider { get; } = new ClassDeclarationCodeFixProvider();

        [Fact]
        public async Task Test_ClassWithoutAccessibilityModifier()
        {
            await VerifyDiagnosticAndFixAsync(@"
class [|C|]
{
    private C()
    {
    }
}
", @"
sealed class C
{
    private C()
    {
    }
}
");
        }

        [Fact]
        public async Task Test_ClassWithAccessibilityModifier()
        {
            await VerifyDiagnosticAndFixAsync(@"
public class [|C|]
{
    private C()
    {
    }
}
", @"
public sealed class C
{
    private C()
    {
    }
}
");
        }

        [Fact]
        public async Task Test_ClassWithTwoConstructors()
        {
            await VerifyDiagnosticAndFixAsync(@"
public class [|C|]
{
    private C()
    {
    }

    private C(object p)
    {
    }
}
", @"
public sealed class C
{
    private C()
    {
    }

    private C(object p)
    {
    }
}
");
        }

        [Fact]
        public async Task TestNoDiagnostic_StaticClass()
        {
            await VerifyNoDiagnosticAsync(@"
static class C
{
    static C()
    {
    }
}
");
        }

        [Fact]
        public async Task TestNoDiagnostic_SealedClass()
        {
            await VerifyNoDiagnosticAsync(@"
sealed class C
{
    private C()
    {
    }
}
");
        }

        [Fact]
        public async Task TestNoDiagnostic_ProtectedConstructor()
        {
            await VerifyNoDiagnosticAsync(@"
class C
{
    protected C()
    {
    }
}
");
        }

        [Fact]
        public async Task TestNoDiagnostic_NoExplicitConstructor()
        {
            await VerifyNoDiagnosticAsync(@"
class C
{
}
");
        }
    }
}
