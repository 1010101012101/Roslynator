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
    public class RCS1114RemoveRedundantDelegateCreationTests : AbstractCSharpCodeFixVerifier
    {
        public override DiagnosticDescriptor Descriptor { get; } = DiagnosticDescriptors.RemoveRedundantDelegateCreation;

        public override DiagnosticAnalyzer Analyzer { get; } = new RemoveRedundantDelegateCreationAnalyzer();

        public override CodeFixProvider FixProvider { get; } = new AssignmentExpressionCodeFixProvider();

        [Fact]
        public async Task TestDiagnosticWithCodeFix_EventHandler()
        {
            await VerifyDiagnosticAndFixAsync(@"
using System;

class Foo
{
    void M()
    {
        Changed += [|new EventHandler(Foo_Changed)|];
        Changed -= [|new EventHandler(Foo_Changed)|];
    }

    protected virtual void Foo_Changed(object sender, EventArgs e) { }

    public event EventHandler Changed;
}
", @"
using System;

class Foo
{
    void M()
    {
        Changed += Foo_Changed;
        Changed -= Foo_Changed;
    }

    protected virtual void Foo_Changed(object sender, EventArgs e) { }

    public event EventHandler Changed;
}
");
        }

        [Fact]
        public async Task TestDiagnosticWithCodeFix_EventHandlerOfT()
        {
            await VerifyDiagnosticAndFixAsync(@"
using System;

class Foo
{
    void M()
    {
        Changed += [|new EventHandler<FooEventArgs>(Foo_Changed)|];
        Changed -= [|new EventHandler<FooEventArgs>(Foo_Changed)|];
    }

    protected virtual void Foo_Changed(object sender, FooEventArgs e) { }

    public event EventHandler<FooEventArgs> Changed;

    public class FooEventArgs : EventArgs
    {
    }
}
", @"
using System;

class Foo
{
    void M()
    {
        Changed += Foo_Changed;
        Changed -= Foo_Changed;
    }

    protected virtual void Foo_Changed(object sender, FooEventArgs e) { }

    public event EventHandler<FooEventArgs> Changed;

    public class FooEventArgs : EventArgs
    {
    }
}
");
        }

        [Fact]
        public async Task TestDiagnosticWithCodeFix_CustomEventHandler()
        {
            await VerifyDiagnosticAndFixAsync(@"
using System;

class Foo
{
    void M()
    {
        Changed += [|new FooEventHandler(Foo_Changed)|];
        Changed -= [|new FooEventHandler(Foo_Changed)|];
    }

    protected virtual void Foo_Changed(object sender, FooEventArgs e) { }

    public event FooEventHandler Changed;

    public delegate void FooEventHandler(object sender, FooEventArgs args);

    public class FooEventArgs : EventArgs
    {
    }
}
", @"
using System;

class Foo
{
    void M()
    {
        Changed += Foo_Changed;
        Changed -= Foo_Changed;
    }

    protected virtual void Foo_Changed(object sender, FooEventArgs e) { }

    public event FooEventHandler Changed;

    public delegate void FooEventHandler(object sender, FooEventArgs args);

    public class FooEventArgs : EventArgs
    {
    }
}
");
        }

        [Fact]
        public async Task TestDiagnosticWithCodeFix_TEventArgs()
        {
            await VerifyDiagnosticAndFixAsync(@"
using System;

class Foo<TEventArgs>
{
    void M()
    {
        Changed += [|new EventHandler<TEventArgs>(Foo_Changed)|];
        Changed -= [|new EventHandler<TEventArgs>(Foo_Changed)|];
    }

    protected virtual void Foo_Changed(object sender, TEventArgs e) { }

    public event EventHandler<TEventArgs> Changed;
}
", @"
using System;

class Foo<TEventArgs>
{
    void M()
    {
        Changed += Foo_Changed;
        Changed -= Foo_Changed;
    }

    protected virtual void Foo_Changed(object sender, TEventArgs e) { }

    public event EventHandler<TEventArgs> Changed;
}
");
        }
    }
}
