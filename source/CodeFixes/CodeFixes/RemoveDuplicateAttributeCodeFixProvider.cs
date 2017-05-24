﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslynator.CSharp.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveDuplicateAttributeCodeFixProvider))]
    [Shared]
    public class RemoveDuplicateAttributeCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CSharpErrorCodes.DuplicateAttribute); }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (!Settings.IsCodeFixEnabled(CodeFixIdentifiers.RemoveDuplicateAttribute))
                return;

            SyntaxNode root = await context.GetSyntaxRootAsync().ConfigureAwait(false);

            AttributeSyntax attribute = root
                .FindNode(context.Span, getInnermostNodeForTie: true)?
                .FirstAncestorOrSelf<AttributeSyntax>();

            Debug.Assert(attribute != null, $"{nameof(attribute)} is null");

            if (attribute == null)
                return;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                switch (diagnostic.Id)
                {
                    case CSharpErrorCodes.DuplicateAttribute:
                        {
                            SyntaxNode parent = attribute.Parent;

                            if (parent.IsKind(SyntaxKind.AttributeList))
                            {
                                var attributeList = (AttributeListSyntax)parent;

                                CodeAction codeAction = CodeAction.Create(
                                    "Remove duplicate attribute",
                                    cancellationToken =>
                                    {
                                        SeparatedSyntaxList<AttributeSyntax> attributes = attributeList.Attributes;

                                        if (attributes.Count == 1)
                                        {
                                            return context.Document.RemoveNodeAsync(attributeList, SyntaxRemoveOptions.KeepUnbalancedDirectives, cancellationToken);
                                        }
                                        else
                                        {
                                            return context.Document.RemoveNodeAsync(attribute, SyntaxRemoveOptions.KeepUnbalancedDirectives, cancellationToken);
                                        }
                                    },
                                    CodeFixIdentifiers.RemoveDuplicateAttribute + EquivalenceKeySuffix);

                                context.RegisterCodeFix(codeAction, diagnostic);
                            }

                            break;
                        }
                }
            }
        }
    }
}
