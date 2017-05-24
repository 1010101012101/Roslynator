﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CSharp.Refactorings;

namespace Roslynator.CSharp.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddBreakStatementToSwitchSectionCodeFixProvider))]
    [Shared]
    public class AddBreakStatementToSwitchSectionCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(
                    CSharpErrorCodes.ControlCannotFallThroughFromOneCaseLabelToAnother,
                    CSharpErrorCodes.ControlCannotFallOutOfSwitchFromFinalCaseLabel);
            }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (!Settings.IsCodeFixEnabled(CodeFixIdentifiers.AddBreakStatementToSwitchSection))
                return;

            SyntaxNode root = await context.GetSyntaxRootAsync().ConfigureAwait(false);

            SwitchSectionSyntax switchSection = root
                .FindNode(context.Span, getInnermostNodeForTie: true)?
                .FirstAncestorOrSelf<SwitchSectionSyntax>();

            Debug.Assert(switchSection != null, $"{nameof(switchSection)} is null");

            if (switchSection == null)
                return;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                switch (diagnostic.Id)
                {
                    case CSharpErrorCodes.ControlCannotFallThroughFromOneCaseLabelToAnother:
                    case CSharpErrorCodes.ControlCannotFallOutOfSwitchFromFinalCaseLabel:
                        {
                            CodeAction codeAction = CodeAction.Create(
                                "Add break statement",
                                cancellationToken => AddBreakStatementToSwitchSectionRefactoring.RefactorAsync(context.Document, switchSection, cancellationToken),
                                CodeFixIdentifiers.AddBreakStatementToSwitchSection + EquivalenceKeySuffix);

                            context.RegisterCodeFix(codeAction, diagnostic);
                            break;
                        }
                }
            }
        }
    }
}
