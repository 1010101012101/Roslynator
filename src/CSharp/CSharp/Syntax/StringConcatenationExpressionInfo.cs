﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslynator.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Roslynator.CSharp.Syntax.SyntaxInfoHelpers;

namespace Roslynator.CSharp.Syntax
{
    /// <summary>
    /// Provides information about string concatenation, i.e. a binary expression that binds to string '+' operator.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public readonly struct StringConcatenationExpressionInfo : IEquatable<StringConcatenationExpressionInfo>
    {
        private StringConcatenationExpressionInfo(
            BinaryExpressionSyntax binaryExpression,
            TextSpan? span = null)
        {
            BinaryExpression = binaryExpression;
            Span = span;
        }

        /// <summary>
        /// The binary expression that represents the string concatenation.
        /// </summary>
        public BinaryExpressionSyntax BinaryExpression { get; }

        internal TextSpan? Span { get; }

        /// <summary>
        /// Determines whether this struct was initialized with an actual syntax.
        /// </summary>
        public bool Success
        {
            get { return BinaryExpression != null; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get { return ToDebugString(Success, this, BinaryExpression, (Span != null) ? BinaryExpression.ToString(Span.Value) : null); }
        }

        internal StringConcatenationAnalysis Analyze()
        {
            return StringConcatenationAnalysis.Create(this);
        }

        internal static StringConcatenationExpressionInfo Create(
            SyntaxNode node,
            SemanticModel semanticModel,
            bool walkDownParentheses = true,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Create(
                Walk(node, walkDownParentheses) as BinaryExpressionSyntax,
                semanticModel,
                cancellationToken);
        }

        internal static StringConcatenationExpressionInfo Create(
            BinaryExpressionSyntax binaryExpression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!binaryExpression.AsChain().IsStringConcatenation(semanticModel, cancellationToken))
                return default;

            return new StringConcatenationExpressionInfo(binaryExpression);
        }

        internal static StringConcatenationExpressionInfo Create(
            in BinaryExpressionChain chain,
            SemanticModel semanticModel,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!chain.IsStringConcatenation(semanticModel, cancellationToken))
                return default;

            return new StringConcatenationExpressionInfo(chain.BinaryExpression, chain.Span);
        }

        /// <summary>
        /// Returns expressions of this binary expression, including expressions of nested binary expressions of the same kind as parent binary expression.
        /// </summary>
        /// <param name="leftToRight">If true expressions are enumerated as they are displayed in the source code.</param>
        /// <returns></returns>
        [Obsolete("This method is obsolete. Use method 'AsChain' instead.")]
        public IEnumerable<ExpressionSyntax> Expressions(bool leftToRight = false)
        {
            ThrowInvalidOperationIfNotInitialized();

            var chain = new BinaryExpressionChain(BinaryExpression, Span ?? BinaryExpression.FullSpan);

            if (leftToRight)
            {
                return chain.Reverse();
            }
            else
            {
                return chain;
            }
        }

        public BinaryExpressionChain AsChain()
        {
            return new BinaryExpressionChain(BinaryExpression, Span ?? BinaryExpression?.FullSpan ?? default);
        }

        internal InterpolatedStringExpressionSyntax ToInterpolatedStringExpression()
        {
            ThrowInvalidOperationIfNotInitialized();

            StringBuilder sb = StringBuilderCache.GetInstance();

            sb.Append('$');

            bool containsRegular = Analyze().ContainsNonVerbatimExpression;

            if (!containsRegular)
                sb.Append('@');

            sb.Append('"');

            foreach (ExpressionSyntax expression in AsChain().Reverse())
            {
                SyntaxKind kind = expression.Kind();

                StringLiteralExpressionInfo stringLiteral = SyntaxInfo.StringLiteralExpressionInfo(expression);

                if (stringLiteral.Success)
                {
                    int startIndex = sb.Length;

                    if (containsRegular
                        && stringLiteral.IsVerbatim)
                    {
                        sb.Append(stringLiteral.ValueText);
                        sb.Replace(@"\", @"\\", startIndex);
                        sb.Replace("\"", @"\" + "\"", startIndex);
                        sb.Replace("{", "{{", startIndex);
                        sb.Replace("}", "}}", startIndex);
                        sb.Replace("\n", @"\n", startIndex);
                        sb.Replace("\r", @"\r", startIndex);
                    }
                    else
                    {
                        sb.Append(stringLiteral.InnerText);
                        sb.Replace("{", "{{", startIndex);
                        sb.Replace("}", "}}", startIndex);
                    }
                }
                else if (kind == SyntaxKind.InterpolatedStringExpression)
                {
                    var interpolatedString = (InterpolatedStringExpressionSyntax)expression;

                    bool isVerbatimInterpolatedString = interpolatedString.IsVerbatim();

                    foreach (InterpolatedStringContentSyntax content in interpolatedString.Contents)
                    {
                        Debug.Assert(content.IsKind(SyntaxKind.Interpolation, SyntaxKind.InterpolatedStringText), content.Kind().ToString());

                        switch (content.Kind())
                        {
                            case SyntaxKind.InterpolatedStringText:
                                {
                                    var text = (InterpolatedStringTextSyntax)content;

                                    if (containsRegular
                                        && isVerbatimInterpolatedString)
                                    {
                                        int startIndex = sb.Length;
                                        sb.Append(text.TextToken.ValueText);
                                        sb.Replace(@"\", @"\\", startIndex);
                                        sb.Replace("\"", @"\" + "\"", startIndex);
                                        sb.Replace("\n", @"\n", startIndex);
                                        sb.Replace("\r", @"\r", startIndex);
                                    }
                                    else
                                    {
                                        sb.Append(content.ToString());
                                    }

                                    break;
                                }
                            case SyntaxKind.Interpolation:
                                {
                                    sb.Append(content.ToString());
                                    break;
                                }
                        }
                    }
                }
                else
                {
                    sb.Append('{');
                    sb.Append(expression.ToString());
                    sb.Append('}');
                }
            }

            sb.Append("\"");

            return (InterpolatedStringExpressionSyntax)ParseExpression(StringBuilderCache.GetStringAndFree(sb));
        }

        internal LiteralExpressionSyntax ToStringLiteralExpression()
        {
            ThrowInvalidOperationIfNotInitialized();

            StringConcatenationAnalysis analysis = Analyze();

            ThrowIfContainsNonStringLiteralExpression(analysis);

            StringBuilder sb = StringBuilderCache.GetInstance();

            if (!analysis.ContainsNonVerbatimExpression)
                sb.Append('@');

            sb.Append('"');

            foreach (ExpressionSyntax expression in AsChain().Reverse())
            {
                StringLiteralExpressionInfo literal = SyntaxInfo.StringLiteralExpressionInfo(expression);

                if (literal.Success)
                {
                    if (analysis.ContainsNonVerbatimExpression && literal.IsVerbatim)
                    {
                        int startIndex = sb.Length;
                        sb.Append(literal.ValueText);
                        sb.Replace(@"\", @"\\", startIndex);
                        sb.Replace("\"", @"\" + "\"", startIndex);
                        sb.Replace("\n", @"\n", startIndex);
                        sb.Replace("\r", @"\r", startIndex);
                    }
                    else
                    {
                        sb.Append(literal.InnerText);
                    }
                }
            }

            sb.Append('"');

            return (LiteralExpressionSyntax)ParseExpression(StringBuilderCache.GetStringAndFree(sb));
        }

        internal LiteralExpressionSyntax ToMultiLineStringLiteralExpression()
        {
            ThrowInvalidOperationIfNotInitialized();

            ThrowIfContainsNonStringLiteralExpression(Analyze());

            StringBuilder sb = StringBuilderCache.GetInstance();

            sb.Append('@');
            sb.Append('"');

            ExpressionSyntax[] expressions = AsChain().Reverse().ToArray();

            for (int i = 0; i < expressions.Length; i++)
            {
                if (expressions[i].IsKind(SyntaxKind.StringLiteralExpression))
                {
                    var literal = (LiteralExpressionSyntax)expressions[i];

                    int length = sb.Length;

                    sb.Append(literal.Token.ValueText);

                    sb.Replace("\"", "\"\"", length);

                    if (sb.Length > length
                        && sb[sb.Length - 1] == '\n')
                    {
                        sb.Remove(sb.Length - 1, 1);

                        if (sb.Length - length > 1
                            && sb[sb.Length - 1] == '\r')
                        {
                            sb.Remove(sb.Length - 1, 1);
                        }

                        sb.AppendLine();
                    }
                    else if (i < expressions.Length - 1)
                    {
                        TextSpan span = TextSpan.FromBounds(expressions[i].Span.End, expressions[i + 1].SpanStart);

                        if (BinaryExpression.SyntaxTree.IsMultiLineSpan(span))
                            sb.AppendLine();
                    }
                }
            }

            sb.Append('"');

            return (LiteralExpressionSyntax)ParseExpression(StringBuilderCache.GetStringAndFree(sb));
        }

        /// <summary>
        /// Returns the string representation of the underlying syntax, not including its leading and trailing trivia.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return (Span != null)
                ? BinaryExpression.ToString(Span.Value)
                : BinaryExpression.ToString();
        }

        private void ThrowInvalidOperationIfNotInitialized()
        {
            if (BinaryExpression == null)
                throw new InvalidOperationException($"{nameof(StringConcatenationExpressionInfo)} is not initalized.");
        }

        private static void ThrowIfContainsNonStringLiteralExpression(in StringConcatenationAnalysis analysis)
        {
            if (analysis.ContainsNonStringLiteral)
                throw new InvalidOperationException("String concatenation contains an expression that is not a string literal.");
        }

        /// <summary>
        /// Determines whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance. </param>
        /// <returns>true if <paramref name="obj" /> and this instance are the same type and represent the same value; otherwise, false. </returns>
        public override bool Equals(object obj)
        {
            return obj is StringConcatenationExpressionInfo other && Equals(other);
        }

        /// <summary>
        /// Determines whether this instance is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
        public bool Equals(StringConcatenationExpressionInfo other)
        {
            return EqualityComparer<BinaryExpressionSyntax>.Default.Equals(BinaryExpression, other.BinaryExpression)
                && EqualityComparer<TextSpan?>.Default.Equals(Span, other.Span);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            return Hash.Combine(Span.GetHashCode(), Hash.Create(BinaryExpression));
        }

        public static bool operator ==(in StringConcatenationExpressionInfo info1, in StringConcatenationExpressionInfo info2)
        {
            return info1.Equals(info2);
        }

        public static bool operator !=(in StringConcatenationExpressionInfo info1, in StringConcatenationExpressionInfo info2)
        {
            return !(info1 == info2);
        }
    }
}
