﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslynator.CSharp.CodeFixes;
using Roslynator.Tests;
using Xunit;

#pragma warning disable RCS1090

namespace Roslynator.CSharp.Analysis.Tests
{
    public class RCS1218SimplifyCodeBranchingTests : AbstractCSharpCodeFixVerifier
    {
        public override DiagnosticDescriptor Descriptor { get; } = DiagnosticDescriptors.SimplifyCodeBranching;

        public override DiagnosticAnalyzer Analyzer { get; } = new SimplifyCodeBranchingAnalyzer();

        public override CodeFixProvider FixProvider { get; } = new SimplifyCodeBranchingCodeFixProvider();

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.SimplifyCodeBranching)]
        public async Task Test_IfElse_WithBraces()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        [|if (f1)
        {
        }
        else
        {
            M();
        }|]
    }
}
", @"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        if (!f1)
        {
            M();
        }
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.SimplifyCodeBranching)]
        public async Task Test_IfElse_WithoutBraces()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        [|if (f1)
        {
        }
        else
            M();|]
    }
}
", @"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        if (!f1)
            M();
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.SimplifyCodeBranching)]
        public async Task Test_IfElseIf_WithBraces()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        [|if (f1)
        {
        }
        else if (f2)
        {
            M();
        }|]
    }
}
", @"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        if (!f1 && f2)
        {
            M();
        }
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.SimplifyCodeBranching)]
        public async Task Test_IfElseIf_WithoutBraces()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        [|if (f1)
        {
        }
        else if (f2)
            M();|]
    }
}
", @"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        if (!f1 && f2)
            M();
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.SimplifyCodeBranching)]
        public async Task Test_While_IfElseWithBraces()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        while (true)
        {
            [|if (f1)
            {
                M();
            }
            else
            {
                break;
            }|]
        }
    }
}
", @"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        while (f1)
        {
            M();
        }
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.SimplifyCodeBranching)]
        public async Task Test_While_IfElseWithoutBraces()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        while (true)
        {
            [|if (f1)
                M();
            else
                break;|]
        }
    }
}
", @"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        while (f1)
        {
            M();
        }
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.SimplifyCodeBranching)]
        public async Task Test_While_IfElseWithMultipleStatements()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        while (true)
        {
            [|if (f1)
            {
                M();
                M();
            }
            else
            {
                break;
            }|]
        }
    }
}
", @"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        while (f1)
        {
            M();
            M();
        }
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.SimplifyCodeBranching)]
        public async Task Test_While_EmbeddedIfElseWithSingleStatement()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        while (true)
            [|if (f1)
            {
                M();
            }
            else
            {
                break;
            }|]
    }
}
", @"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        while (f1)
        {
            M();
        }
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.SimplifyCodeBranching)]
        public async Task Test_While_EmbeddedIfElseWithMultipleStatements()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        while (true)
            [|if (f1)
            {
                M();
                M();
            }
            else
            {
                break;
            }|]
    }
}
", @"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        while (f1)
        {
            M();
            M();
        }
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.SimplifyCodeBranching)]
        public async Task Test_While_IfWithBraces_LastStatement()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        while (true)
        {
            M();

            [|if (f1)
            {
                break;
            }|]
        }
    }
}
", @"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        do
        {
            M();
        }
        while (!f1);
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.SimplifyCodeBranching)]
        public async Task Test_While_IfWithoutBraces_LastStatement()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        while (true)
        {
            M();

            [|if (f1)
                break;|]
        }
    }
}
", @"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        do
        {
            M();
        }
        while (!f1);
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.SimplifyCodeBranching)]
        public async Task Test_While_IfWithBraces_FirstStatement()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        while (true)
        {
            [|if (f1)
            {
                break;
            }|]
            M();
        }
    }
}
", @"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        while (!f1)
        {
            M();
        }
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.SimplifyCodeBranching)]
        public async Task Test_While_IfWithoutBraces_FirstStatement()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        while (true)
        {
            [|if (f1)
                break;|]
            M();
        }
    }
}
", @"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        while (!f1)
        {
            M();
        }
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.SimplifyCodeBranching)]
        public async Task Test_Do_IfWithBraces_LastStatement()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        do
        {
            M();

            [|if (f1)
            {
                break;
            }|]
        }
        while (true);
    }
}
", @"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        do
        {
            M();
        }
        while (!f1);
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.SimplifyCodeBranching)]
        public async Task Test_Do_IfWithoutBraces_LastStatement()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        do
        {
            M();

            [|if (f1)
                break;|]
        }
        while (true);
    }
}
", @"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        do
        {
            M();
        }
        while (!f1);
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.SimplifyCodeBranching)]
        public async Task Test_Do_IfWithBraces_FirstStatement()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        do
        {
            [|if (f1)
            {
                break;
            }|]
            M();
        }
        while (true);
    }
}
", @"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        while (!f1)
        {
            M();
        }
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.SimplifyCodeBranching)]
        public async Task Test_Do_IfWithoutBraces_FirstStatement()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        do
        {
            [|if (f1)
                break;|]
            M();
        }
        while (true);
    }
}
", @"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        while (!f1)
        {
            M();
        }
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.SimplifyCodeBranching)]
        public async Task TestNoDiagnostic()
        {
            await VerifyNoDiagnosticAsync(@"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        if (f1)
        {
        }
        else
        {
        }

        if (f1)
        {
        }
        else if (f2)
        {
        }

        if (f1)
        {
            M();
        }
        else
        {
            M();
        }

        if (f1)
        {
            M();
        }
        else if (f2)
        {
            M();
        }

        if (f1)
        {
        }
        else if (f2)
        {
            M();
        }
        else
        {
        }

        if ()
        {
        }
        else if (f2)
        {
            M();
        }

        if (f1)
        {
        }
        else if ()
        {
            M();
        }
    }
}
", options: Options.AddAllowedCompilerDiagnosticId("CS1525"));
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.SimplifyCodeBranching)]
        public async Task TestNoDiagnostic_While()
        {
            await VerifyNoDiagnosticAsync(@"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        while (true)
        {
            if (f1)
            {
                break;
            }
        }

        while (f1)
        {
            if (f2)
            {
                break;
            }
        }

        while (true)
        {
            if (f1)
            {
                break;
            }
            else
            {
            }
        }

        while ()
        {
            if (f1)
            {
                break;
            }
        }

        while (true)
        {
            if ()
            {
                break;
            }
        }

        while (f1)
        {
            M();

            if (f2)
            {
                break;
            }
        }

        while (f1)
        {
            M();

            if (f2)
            {
                return;
            }
        }

        while ()
        {
            M();

            if (f1)
            {
                break;
            }
        }

        while (f1)
        {
            M();

            if ()
            {
                break;
            }
        }

        while (f1)
        {
            M();

            if (f2)
            {
                return;
            }
        }

        while ()
        {
            M();

            if (f1)
            {
                break;
            }
        }

        while (f1)
        {
            M();

            if ()
            {
                break;
            }
        }
    }
}
", options: Options.AddAllowedCompilerDiagnosticId("CS1525"));
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.SimplifyCodeBranching)]
        public async Task TestNoDiagnostic_Do()
        {
            await VerifyNoDiagnosticAsync(@"
class C
{
    void M(bool f1 = false, bool f2 = false)
    {
        do
        {
            M();

            if (f2)
            {
                break;
            }

        } while (f1);

        do
        {
            M();

            if (f2)
            {
                return;
            }
        }
        while (f1);

        do
        {
            M();

            if (f1)
            {
                break;
            }
        }
        while ();

        do
        {
            M();

            if ()
            {
                break;
            }
        }
        while (f1);

        do
        {
            M();

            if (f2)
            {
                break;
            }

        } while (f1);

        do
        {
            M();

            if (f2)
            {
                return;
            }

        } while (f1);

        do
        {
            M();

            if (f1)
            {
                break;
            }

        } while ();

        do
        {
            M();

            if ()
            {
                break;
            }

        } while (f1);
    }
}
", options: Options.AddAllowedCompilerDiagnosticId("CS1525"));
        }
    }
}
