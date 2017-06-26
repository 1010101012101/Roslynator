﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslynator.CSharp.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StatementCodeFixProvider))]
    [Shared]
    public class StatementCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(
                    CompilerDiagnosticIdentifiers.UnreachableCodeDetected,
                    CompilerDiagnosticIdentifiers.EmptySwitchBlock);
            }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (!Settings.IsAnyCodeFixEnabled(
                CodeFixIdentifiers.RemoveUnreachableCode,
                CodeFixIdentifiers.RemoveEmptySwitchStatement))
            {
                return;
            }

            SyntaxNode root = await context.GetSyntaxRootAsync().ConfigureAwait(false);

            StatementSyntax statement = root
                .FindNode(context.Span, getInnermostNodeForTie: true)?
                .FirstAncestorOrSelf<StatementSyntax>();

            Debug.Assert(statement != null, $"{nameof(statement)} is null");

            if (statement == null)
                return;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                switch (diagnostic.Id)
                {
                    case CompilerDiagnosticIdentifiers.UnreachableCodeDetected:
                        {
                            if (context.Span.Start == statement.SpanStart)
                            {
                                StatementContainer container;
                                if (StatementContainer.TryCreate(statement, out container))
                                {
                                    CodeAction codeAction = CodeAction.Create(
                                        "Remove unreachable code",
                                        cancellationToken =>
                                        {
                                            SyntaxList<StatementSyntax> statements = container.Statements;

                                            int index = statements.IndexOf(statement);

                                            if (index == statements.Count - 1)
                                            {
                                                return context.Document.RemoveStatementAsync(statement, context.CancellationToken);
                                            }
                                            else
                                            {
                                                SyntaxRemoveOptions removeOptions = RemoveHelper.DefaultRemoveOptions;

                                                if (statement.GetLeadingTrivia().All(f => f.IsWhitespaceOrEndOfLineTrivia()))
                                                    removeOptions &= ~SyntaxRemoveOptions.KeepLeadingTrivia;

                                                if (statements.Last().GetTrailingTrivia().All(f => f.IsWhitespaceOrEndOfLineTrivia()))
                                                    removeOptions &= ~SyntaxRemoveOptions.KeepTrailingTrivia;

                                                return context.Document.RemoveNodesAsync(statements.Skip(index), removeOptions, context.CancellationToken);
                                            }
                                        },
                                        GetEquivalenceKey(diagnostic));

                                    context.RegisterCodeFix(codeAction, diagnostic);
                                }
                            }

                            break;
                        }
                    case CompilerDiagnosticIdentifiers.EmptySwitchBlock:
                        {
                            if (!Settings.IsCodeFixEnabled(CodeFixIdentifiers.RemoveEmptySwitchStatement))
                                break;

                            if (!statement.IsKind(SyntaxKind.SwitchStatement))
                                break;

                            var switchStatement = (SwitchStatementSyntax)statement;

                            CodeAction codeAction = CodeAction.Create(
                                "Remove switch statement",
                                cancellationToken => context.Document.RemoveStatementAsync(switchStatement, cancellationToken),
                                GetEquivalenceKey(diagnostic));

                            context.RegisterCodeFix(codeAction, diagnostic);
                            break;
                        }
                }
            }
        }
    }
}
