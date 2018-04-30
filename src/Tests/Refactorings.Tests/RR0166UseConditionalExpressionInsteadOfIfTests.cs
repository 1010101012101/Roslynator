﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Roslynator.CSharp.Refactorings;
using Xunit;

#pragma warning disable RCS1090

namespace Roslynator.Refactorings.Tests
{
    public class RR0166UseConditionalExpressionInsteadOfIfTests : RoslynatorCSharpCodeRefactoringVerifier
    {
        public override string RefactoringId { get; } = RefactoringIdentifiers.UseConditionalExpressionInsteadOfIf;

        [Theory]
        [InlineData("if (f) { z = x; } else { z = y; }", "z = (f) ? x : y;")]
        [InlineData("if (f) z = x; else z = y;", "z = (f) ? x : y;")]
        public async Task TestRefactoring_IfElseToAssignmentWithConditionalExpression(string fixableCode, string fixedCode)
        {
            await VerifyRefactoringAsync(@"
class C
{
    void M(bool f, string x, string y, string z)
    {
        [||]
    }
}
", fixableCode, fixedCode, RefactoringId);
        }

        [Fact]
        public async Task TestRefactoring_AssignmentAndIfElseToAssignmentWithConditionalExpression()
        {
            await VerifyRefactoringAsync(@"
class C
{
    void M(bool f, string x, string y, string z)
    {
[|        z = null;
        if (f)
        {
            z = x;
        }
        else
        {
            z = y;
        }|]
    }
}
", @"
class C
{
    void M(bool f, string x, string y, string z)
    {
        z = (f) ? x : y;
    }
}
", RefactoringId);
        }

        [Fact]
        public async Task TestRefactoring_LocalDeclarationAndIfElseToAssignmentWithConditionalExpression()
        {
            await VerifyRefactoringAsync(@"
class C
{
    void M(bool f, string x, string y)
    {
[|        string z = null;
        if (f)
        {
            z = x;
        }
        else
        {
            z = y;
        }|]
    }
}
", @"
class C
{
    void M(bool f, string x, string y)
    {
        string z = (f) ? x : y;
    }
}
", RefactoringId);
        }

        [Theory]
        [InlineData("if (f) { return x; } else { return y; }", "return (f) ? x : y;")]
        [InlineData("if (f) return x; else return y;", "return (f) ? x : y;")]
        [InlineData("if (f) { return x; } return y;", "return (f) ? x : y;")]
        [InlineData("if (f) return x; return y;", "return (f) ? x : y;")]
        public async Task TestRefactoring_IfToReturnWithConditionalExpression(string fixableCode, string fixedCode)
        {
            await VerifyRefactoringAsync(@"
class C
{
    string M(bool f, string x, string y, string z)
    {
        [||]
    }
}
", fixableCode, fixedCode, RefactoringId);
        }

        [Theory]
        [InlineData("if (f) { yield return x; } else { yield return y; }", "yield return (f) ? x : y;")]
        [InlineData("if (f) yield return x; else yield return y;", "yield return (f) ? x : y;")]
        public async Task TestRefactoring_IfElseToYieldReturnWithConditionalExpression(string fixableCode, string fixedCode)
        {
            await VerifyRefactoringAsync(@"
using System.Collections.Generic;

class C
{
    IEnumerable<string> M(bool f, string x, string y, string z)
    {
        [||]
    }
}
", fixableCode, fixedCode, RefactoringId);
        }

        [Fact]
        public async Task TestNoRefactoring_IfElseToAssignmentWithConditionalExpression()
        {
            await VerifyNoRefactoringAsync(@"
class C
{
    void M(bool f)
    {
        int? ni;
        [||]if (f)
        {
            ni = null;
        }
        else
        {
            ni = 1;
        }
    }
}
", RefactoringId);
        }

        [Fact]
        public async Task TestNoRefactoring_LocalDeclarationAndIfElseAssignmentWithConditionalExpression()
        {
            await VerifyNoRefactoringAsync(@"
class C
{
    void M(bool f)
    {
[|        int? ni;
        if (f)
        {
            ni = null;
        }
        else
        {
            ni = 1;
        }|]
    }
}
", RefactoringId);
        }

        [Fact]
        public async Task TestNoRefactoring_AssignmentAndIfElseToAssignmentWithConditionalExpression()
        {
            await VerifyNoRefactoringAsync(@"
class C
{
    void M(bool f)
    {
        int? ni;
[|        ni = null;
        if (f)
        {
            ni = null;
        }
        else
        {
            ni = 1;
        }|]
    }
}
", RefactoringId);
        }

        [Fact]
        public async Task TestNoRefactoring_IfElseToYieldReturnWithConditionalExpression()
        {
            await VerifyNoRefactoringAsync(@"
using System.Collections.Generic;

class C
{
    IEnumerable<int?> M(bool f)
    {
[|        if (f)
        {
            yield return null;
        }
        else
        {
            yield return 1;
        }|]
    }
}
", RefactoringId);
        }

        [Fact]
        public async Task TestNoRefactoring_IfElseToReturnWithConditionalExpression()
        {
            await VerifyNoRefactoringAsync(@"
class C
{
    int? M(bool f)
    {
[|        if (f)
        {
            return null;
        }
        else
        {
            return 1;
        }|]
    }
}
", RefactoringId);
        }

        [Fact]
        public async Task TestNoRefactoring_IfReturnToReturnWithConditionalExpression()
        {
            await VerifyNoRefactoringAsync(@"
class C
{
    int? M(bool f)
    {
[|        if (f)
        {
            return null;
        }

        return 1;|]
    }
}
", RefactoringId);
        }
    }
}
