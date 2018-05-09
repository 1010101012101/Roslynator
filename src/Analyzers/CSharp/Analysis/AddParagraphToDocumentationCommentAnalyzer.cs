﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Roslynator.CSharp.Analysis
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AddParagraphToDocumentationCommentAnalyzer : BaseDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(DiagnosticDescriptors.AddParagraphToDocumentationComment); }
        }

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            base.Initialize(context);

            context.RegisterSyntaxNodeAction(AnalyzeSingleLineDocumentationCommentTrivia, SyntaxKind.SingleLineDocumentationCommentTrivia);
        }

        private static void AnalyzeSingleLineDocumentationCommentTrivia(SyntaxNodeAnalysisContext context)
        {
            var documentationComment = (DocumentationCommentTriviaSyntax)context.Node;

            if (!documentationComment.IsPartOfMemberDeclaration())
                return;

            XmlElementSyntax summaryElement = documentationComment.SummaryElement();

            if (summaryElement == null)
                return;

            SyntaxList<XmlNodeSyntax> content = summaryElement.Content;

            if (!content.Any())
                return;

            (TextSpan span1, TextSpan span2, IList<TextSpan> spans) = FindFixableSpan(content, stopOnFirstMatch: true);

            if (span2.End > 0)
            {
                context.ReportDiagnostic(
                    DiagnosticDescriptors.AddParagraphToDocumentationComment,
                    Location.Create(documentationComment.SyntaxTree, TextSpan.FromBounds(span1.Start, span2.End)));
            }
        }

        internal static (TextSpan span1, TextSpan span2, List<TextSpan> spans)
            FindFixableSpan(SyntaxList<XmlNodeSyntax> nodes, bool stopOnFirstMatch = false)
        {
            int index = -1;
            int endIndex = -1;
            int index2 = -1;
            int endIndex2 = -1;

            List<TextSpan> spans = null;

            SyntaxNodeOrToken last = default;

            var state = State.BeforeParagraph;

            foreach (XmlNodeSyntax node in nodes)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.XmlElement:
                        {
                            var xmlElement = (XmlElementSyntax)node;

                            if (xmlElement.IsLocalName("para", StringComparison.OrdinalIgnoreCase))
                                return (default, default, spans);

                            switch (state)
                            {
                                case State.BeforeParagraph:
                                    {
                                        state = State.Paragraph;
                                        index = node.SpanStart;
                                        last = node;
                                        break;
                                    }
                                case State.Paragraph:
                                    {
                                        last = node;
                                        break;
                                    }
                                case State.NewLine:
                                case State.WhiteSpaceAfterNewLine:
                                    {
                                        state = State.Paragraph;
                                        last = node;
                                        break;
                                    }
                                case State.WhiteSpaceBetweenParagraphs:
                                    {
                                        state = State.Paragraph;
                                        index2 = node.SpanStart;
                                        last = node;
                                        break;
                                    }
                            }

                            break;
                        }
                    case SyntaxKind.XmlEmptyElement:
                        {
                            var xmlEmptyElement = (XmlEmptyElementSyntax)node;

                            if (xmlEmptyElement.IsLocalName("para", StringComparison.OrdinalIgnoreCase))
                                return (default, default, spans);

                            switch (state)
                            {
                                case State.BeforeParagraph:
                                    {
                                        state = State.Paragraph;
                                        index = node.SpanStart;
                                        last = node;
                                        break;
                                    }
                                case State.Paragraph:
                                    {
                                        last = node;
                                        break;
                                    }
                                case State.NewLine:
                                case State.WhiteSpaceAfterNewLine:
                                    {
                                        state = State.Paragraph;
                                        last = node;
                                        break;
                                    }
                                case State.WhiteSpaceBetweenParagraphs:
                                    {
                                        state = State.Paragraph;
                                        index2 = node.SpanStart;
                                        last = node;
                                        break;
                                    }
                            }

                            break;
                        }
                    case SyntaxKind.XmlText:
                        {
                            var xmlText = (XmlTextSyntax)node;

                            foreach (SyntaxToken token in xmlText.TextTokens)
                            {
                                switch (token.Kind())
                                {
                                    case SyntaxKind.XmlTextLiteralToken:
                                        {
                                            switch (state)
                                            {
                                                case State.BeforeParagraph:
                                                    {
                                                        if (!StringUtility.IsEmptyOrWhitespace(token.ValueText))
                                                        {
                                                            state = State.Paragraph;
                                                            index = token.SpanStart;
                                                            last = token;
                                                        }

                                                        break;
                                                    }
                                                case State.Paragraph:
                                                    {
                                                        last = token;
                                                        break;
                                                    }
                                                case State.NewLine:
                                                    {
                                                        if (StringUtility.IsEmptyOrWhitespace(token.ValueText))
                                                        {
                                                            state = State.WhiteSpaceAfterNewLine;
                                                        }
                                                        else
                                                        {
                                                            state = State.Paragraph;
                                                            last = token;
                                                        }

                                                        break;
                                                    }
                                                case State.WhiteSpaceAfterNewLine:
                                                    {
                                                        if (!StringUtility.IsEmptyOrWhitespace(token.ValueText))
                                                        {
                                                            state = State.Paragraph;
                                                            last = token;
                                                        }

                                                        break;
                                                    }
                                                case State.WhiteSpaceBetweenParagraphs:
                                                    {
                                                        if (!StringUtility.IsEmptyOrWhitespace(token.ValueText))
                                                        {
                                                            state = State.Paragraph;
                                                            index2 = token.SpanStart;
                                                            last = token;
                                                        }

                                                        break;
                                                    }
                                            }

                                            break;
                                        }
                                    case SyntaxKind.XmlTextLiteralNewLineToken:
                                        {
                                            switch (state)
                                            {
                                                case State.BeforeParagraph:
                                                    {
                                                        break;
                                                    }
                                                case State.Paragraph:
                                                    {
                                                        state = State.NewLine;
                                                        break;
                                                    }
                                                case State.NewLine:
                                                case State.WhiteSpaceAfterNewLine:
                                                    {
                                                        if (index2 != -1)
                                                        {
                                                            endIndex2 = last.Span.End;

                                                            if (!stopOnFirstMatch)
                                                            {
                                                                if (spans == null)
                                                                    spans = new List<TextSpan>() { TextSpan.FromBounds(index, endIndex) };

                                                                spans.Add(TextSpan.FromBounds(index2, endIndex2));
                                                                index = index2;
                                                                index2 = -1;
                                                                endIndex2 = -1;
                                                            }
                                                            else
                                                            {
                                                                return (TextSpan.FromBounds(index, endIndex), TextSpan.FromBounds(index2, endIndex2), default);
                                                            }
                                                        }

                                                        endIndex = last.Span.End;
                                                        last = default;
                                                        state = State.WhiteSpaceBetweenParagraphs;
                                                        break;
                                                    }
                                                case State.WhiteSpaceBetweenParagraphs:
                                                    {
                                                        break;
                                                    }
                                            }

                                            break;
                                        }
                                }
                            }

                            break;
                        }
                }
            }

            if (index2 == -1)
                return (default, default, spans);

            Debug.Assert(last != default);

            endIndex2 = last.Span.End;

            if (stopOnFirstMatch)
                return (TextSpan.FromBounds(index, endIndex), TextSpan.FromBounds(index2, endIndex2), default);

            if (spans == null)
                spans = new List<TextSpan>() { TextSpan.FromBounds(index, endIndex) };

            spans.Add(TextSpan.FromBounds(index2, endIndex2));

            return (default, default, spans);
        }

        private enum State
        {
            BeforeParagraph,
            Paragraph,
            NewLine,
            WhiteSpaceAfterNewLine,
            WhiteSpaceBetweenParagraphs,
        }
    }
}
