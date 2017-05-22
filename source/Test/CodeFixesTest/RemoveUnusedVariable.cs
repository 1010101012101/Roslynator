﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Roslynator.CSharp.CodeFixes.Test
{
    internal static class RemoveUnusedVariable
    {
        private static object Foo()
        {
            object x = null;

            object x1 = null, x2 = null;

            return x1;
        }
    }
}
