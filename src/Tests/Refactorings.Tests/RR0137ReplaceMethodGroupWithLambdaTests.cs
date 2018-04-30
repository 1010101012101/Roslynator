﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Roslynator.CSharp.Refactorings;
using Xunit;

#pragma warning disable RCS1090

namespace Roslynator.Refactorings.Tests
{
    public class RR0137ReplaceMethodGroupWithLambdaTests : AbstractCSharpCodeRefactoringVerifier
    {
        public override string RefactoringId { get; } = RefactoringIdentifiers.ReplaceMethodGroupWithLambda;

        [Fact]
        public async Task TestCodeRefactoring_VariableDeclaration()
        {
            await VerifyRefactoringAsync(@"
using System;

public class C
{
    public void Foo()
    {
        Action action1 = [||]VM;
        Action<string> action2 = [||]VM1;
        Action<string, string> action3 = [||]VM2;
    }

    public void Foo2()
    {
        Func<string> func1 = [||]M;
        Func<string, string> func2 = [||]M1;
        Func<string, string, string> func3 = [||]M2;
    }

    public void VM() { }
    public void VM1(string s) { }
    public void VM2(string s1, string s2) { }
    public string M() => null;
    public string M1(string s) => null;
    public string M2(string s1, string s2) => null;
}
", @"
using System;

public class C
{
    public void Foo()
    {
        Action action1 = () => VM();
        Action<string> action2 = f2 => VM1(f2);
        Action<string, string> action3 = (f, g) => VM2(f, g);
    }

    public void Foo2()
    {
        Func<string> func1 = () => M();
        Func<string, string> func2 = f2 => M1(f2);
        Func<string, string, string> func3 = (f, g) => M2(f, g);
    }

    public void VM() { }
    public void VM1(string s) { }
    public void VM2(string s1, string s2) { }
    public string M() => null;
    public string M1(string s) => null;
    public string M2(string s1, string s2) => null;
}
", RefactoringId);
        }

        [Fact]
        public async Task TestCodeRefactoring_SimpleAssignment()
        {
            await VerifyRefactoringAsync(@"
using System;

public class C
{
    public void Foo()
    {
        Action action1 = null;
        Action<string> action2 = null;
        Action<string, string> action3 = null;

        action1 = [||]VM;
        action2 = [||]VM1;
        action3 = [||]VM2;
    }

    public void Foo2()
    {
        Func<string> func1 = null;
        Func<string, string> func2 = null;
        Func<string, string, string> func3 = null;

        func1 = [||]M;
        func2 = [||]M1;
        func3 = [||]M2;
    }

    public void VM() { }
    public void VM1(string s) { }
    public void VM2(string s1, string s2) { }
    public string M() => null;
    public string M1(string s) => null;
    public string M2(string s1, string s2) => null;
}
", @"
using System;

public class C
{
    public void Foo()
    {
        Action action1 = null;
        Action<string> action2 = null;
        Action<string, string> action3 = null;

        action1 = () => VM();
        action2 = f2 => VM1(f2);
        action3 = (f, g) => VM2(f, g);
    }

    public void Foo2()
    {
        Func<string> func1 = null;
        Func<string, string> func2 = null;
        Func<string, string, string> func3 = null;

        func1 = () => M();
        func2 = f2 => M1(f2);
        func3 = (f, g) => M2(f, g);
    }

    public void VM() { }
    public void VM1(string s) { }
    public void VM2(string s1, string s2) { }
    public string M() => null;
    public string M1(string s) => null;
    public string M2(string s1, string s2) => null;
}
", RefactoringId);
        }

        [Fact]
        public async Task TestCodeRefactoring_Argument()
        {
            await VerifyRefactoringAsync(@"
using System;

public class C
{
    public void M(
        Func<string> func1,
        Func<string, string> func2,
        Func<string, string, string> func3)
    {
        M(
            [||]M,
            [||]M1,
            [||]M2);
    }

    public void MM(
        Action action1,
        Action<string> action2,
        Action<string, string> action3)
    {
        MM(
            [||]VM,
            [||]VM1,
            [||]VM2);
    }

    public void VM() { }
    public void VM1(string s) { }
    public void VM2(string s1, string s2) { }
    public string M() => null;
    public string M1(string s) => null;
    public string M2(string s1, string s2) => null;
}
", @"
using System;

public class C
{
    public void M(
        Func<string> func1,
        Func<string, string> func2,
        Func<string, string, string> func3)
    {
        M(
            () => M(),
            f2 => M1(f2),
            (f, g) => M2(f, g));
    }

    public void MM(
        Action action1,
        Action<string> action2,
        Action<string, string> action3)
    {
        MM(
            () => VM(),
            f2 => VM1(f2),
            (f, g) => VM2(f, g));
    }

    public void VM() { }
    public void VM1(string s) { }
    public void VM2(string s1, string s2) { }
    public string M() => null;
    public string M1(string s) => null;
    public string M2(string s1, string s2) => null;
}
", RefactoringId);
        }
    }
}
