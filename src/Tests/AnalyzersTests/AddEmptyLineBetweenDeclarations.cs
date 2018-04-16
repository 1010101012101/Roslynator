﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma warning disable RCS1023, RCS1025, RCS1100 

using System;

namespace Roslynator.CSharp.Analyzers.Tests
{
    internal static class AddEmptyLineBetweenDeclarations
    {
        private static class Foo
        {
            public static void M1()
            {
            }
            public static void M2()
            {
            }

            [Obsolete]
            public static string P1 { get; set; }
            public static string P2 { get; set; }
            [Obsolete]
            public static string P3 { get; set; }
        }
        private static class Foo2
        {
            public static void M1()
            {
            }
            /// <summary>
            /// x
            /// </summary>
            public static void M2()
            {
            }
        }
        private static class Foo3
        {
            public static void M1() { }
            /// <summary>
            /// x
            /// </summary>
            public static void M2() { }
        }
        private enum EnumName
        {
            A = 0,
            /// <summary>
            /// x
            /// </summary>
            B = 1,

            [Obsolete]
            C = 2,
            D = 3,
            [Obsolete]
            E = 4,
        }

        //n

        private static class Foo4
        {
            public static void M1() { }

            /// <summary>
            /// x
            /// </summary>
            public static void M2() { }

            public static string P1 { get; set; }
            public static string P2 { get; set; }
            public static string P3 { get; set; }
        }

        private static class Foo5
        {
            public static void M1()
            {
            }

            /// <summary>
            /// x
            /// </summary>
            public static void M2()
            {
            }
        }

        private enum EnumName2
        {
            A = 0,

            /// <summary>
            /// x
            /// </summary>
            B = 1,

            C = 2,
            D = 3,
            E = 4,
        }

        private enum EnumName4
        {
            A = 0, B = 1,
        }
    }
}
