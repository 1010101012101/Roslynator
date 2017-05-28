﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CSharp.Helpers;

namespace Roslynator.CSharp.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ArgumentCodeFixProvider))]
    [Shared]
    public class ArgumentCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(
                    CompilerDiagnosticIdentifiers.ArgumentMustBePassedWithOutKeyword,
                    CompilerDiagnosticIdentifiers.CannotConvertArgumentType);
            }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (!Settings.IsAnyCodeFixEnabled(
                CodeFixIdentifiers.AddOutModifierToArgument,
                CodeFixIdentifiers.CreateSingletonArray))
            {
                return;
            }

            SyntaxNode root = await context.GetSyntaxRootAsync().ConfigureAwait(false);

            ArgumentSyntax argument = root
                .FindNode(context.Span, getInnermostNodeForTie: true)?
                .FirstAncestorOrSelf<ArgumentSyntax>();

            Debug.Assert(argument != null, $"{nameof(argument)} is null");

            if (argument == null)
                return;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                switch (diagnostic.Id)
                {
                    case CompilerDiagnosticIdentifiers.ArgumentMustBePassedWithOutKeyword:
                        {
                            if (Settings.IsCodeFixEnabled(CodeFixIdentifiers.AddOutModifierToArgument))
                            {
                                CodeAction codeAction = CodeAction.Create(
                               "Add 'out' modifier",
                               cancellationToken =>
                               {
                                   ArgumentSyntax newArgument = argument
                                       .WithRefOrOutKeyword(CSharpFactory.OutKeyword())
                                       .WithFormatterAnnotation();

                                   return context.Document.ReplaceNodeAsync(argument, newArgument, context.CancellationToken);
                               },
                               CodeFixIdentifiers.AddOutModifierToArgument + EquivalenceKeySuffix);

                                context.RegisterCodeFix(codeAction, diagnostic);
                            }

                            break;
                        }
                    case CompilerDiagnosticIdentifiers.CannotConvertArgumentType:
                        {
                            if (Settings.IsCodeFixEnabled(CodeFixIdentifiers.CreateSingletonArray))
                            {
                                ExpressionSyntax expression = argument.Expression;

                                if (expression?.IsMissing == false)
                                {
                                    SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

                                    ITypeSymbol typeSymbol = semanticModel.GetTypeSymbol(expression);

                                    if (typeSymbol?.IsErrorType() == false)
                                    {
                                        foreach (ITypeSymbol typeSymbol2 in DetermineParameterTypeHelper.DetermineParameterTypes(argument, semanticModel, context.CancellationToken))
                                        {
                                            if (!typeSymbol.Equals(typeSymbol2)
                                                && typeSymbol2.IsArrayType())
                                            {
                                                var arrayType = (IArrayTypeSymbol)typeSymbol2;

                                                if (semanticModel.IsImplicitConversion(expression, arrayType.ElementType))
                                                {
                                                    CodeAction codeAction = CodeAction.Create(
                                                        "Create singleton array",
                                                        cancellationToken => CreateSingletonArrayRefactoring.RefactorAsync(context.Document, expression, arrayType.ElementType, semanticModel, cancellationToken),
                                                        CodeFixIdentifiers.CreateSingletonArray + EquivalenceKeySuffix);

                                                    context.RegisterCodeFix(codeAction, diagnostic);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            break;
                        }
                }
            }
        }
    }
}
