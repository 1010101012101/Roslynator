﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslynator.CSharp.SyntaxWalkers;

namespace Roslynator.CSharp.Analysis
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MarkParameterWithInModifierAnalyzer : BaseDiagnosticAnalyzer
    {
        private static readonly SymbolDisplayFormat _symbolDisplayFormat = new SymbolDisplayFormat(kindOptions: SymbolDisplayKindOptions.IncludeTypeKeyword);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(DiagnosticDescriptors.MarkParameterWithInModifier); }
        }

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            base.Initialize(context);
            context.EnableConcurrentExecution();

            //TODO: AnalyzeIndexerDeclaration
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeConstructorDeclaration, SyntaxKind.ConstructorDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeOperatorDeclaration, SyntaxKind.OperatorDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeConversionOperatorDeclaration, SyntaxKind.ConversionOperatorDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeLocalFunction, SyntaxKind.LocalFunctionStatement);
        }

        private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;

            if (methodDeclaration.Modifiers.ContainsAny(SyntaxKind.AsyncKeyword, SyntaxKind.OverrideKeyword))
                return;

            Analyze(context, methodDeclaration, methodDeclaration.ParameterList, methodDeclaration.BodyOrExpressionBody());
        }

        private static void AnalyzeConstructorDeclaration(SyntaxNodeAnalysisContext context)
        {
            var constructorDeclaration = (ConstructorDeclarationSyntax)context.Node;

            Analyze(context, constructorDeclaration, constructorDeclaration.ParameterList, constructorDeclaration.BodyOrExpressionBody());
        }

        private static void AnalyzeOperatorDeclaration(SyntaxNodeAnalysisContext context)
        {
            var operatorDeclaration = (OperatorDeclarationSyntax)context.Node;

            Analyze(context, operatorDeclaration, operatorDeclaration.ParameterList, operatorDeclaration.BodyOrExpressionBody());
        }

        private static void AnalyzeConversionOperatorDeclaration(SyntaxNodeAnalysisContext context)
        {
            var operatorDeclaration = (ConversionOperatorDeclarationSyntax)context.Node;

            Analyze(context, operatorDeclaration, operatorDeclaration.ParameterList, operatorDeclaration.BodyOrExpressionBody());
        }

        private static void AnalyzeLocalFunction(SyntaxNodeAnalysisContext context)
        {
            var localFunction = (LocalFunctionStatementSyntax)context.Node;

            if (localFunction.Modifiers.Contains(SyntaxKind.AsyncKeyword))
                return;

            Analyze(context, localFunction, localFunction.ParameterList, localFunction.BodyOrExpressionBody());
        }

        private static void Analyze(
            SyntaxNodeAnalysisContext context,
            SyntaxNode declaration,
            ParameterListSyntax parameterList,
            CSharpSyntaxNode bodyOrExpressionBody)
        {
            if (parameterList == null)
                return;

            if (bodyOrExpressionBody == null)
                return;

            if (!parameterList.Parameters.Any())
                return;

            SemanticModel semanticModel = context.SemanticModel;
            CancellationToken cancellationToken = context.CancellationToken;

            var methodSymbol = (IMethodSymbol)semanticModel.GetDeclaredSymbol(declaration, cancellationToken);

            SyntaxWalker walker = null;

            foreach (IParameterSymbol parameter in methodSymbol.Parameters)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (parameter.RefKind == RefKind.None)
                {
                    ITypeSymbol type = parameter.Type;

                    if (type.TypeKind == TypeKind.Struct
                        && type
                            .ToDisplayParts(_symbolDisplayFormat)
                            .Any(f => f.Kind == SymbolDisplayPartKind.Keyword && f.ToString() == "readonly"))
                    {
                        if (walker == null)
                        {
                            if (methodSymbol.ImplementsInterfaceMember(allInterfaces: true))
                                return;

                            walker = SyntaxWalker.GetInstance();
                        }

                        walker.Parameters.Add(parameter.Name, parameter);
                    }
                }
            }

            if (walker == null)
                return;

            walker.SemanticModel = semanticModel;
            walker.CancellationToken = cancellationToken;

            if (bodyOrExpressionBody is BlockSyntax body)
            {
                walker.VisitBlock(body);
            }
            else
            {
                walker.VisitArrowExpressionClause((ArrowExpressionClauseSyntax)bodyOrExpressionBody);
            }

            foreach (KeyValuePair<string, IParameterSymbol> kvp in walker.Parameters)
            {
                var parameter = (ParameterSyntax)kvp.Value.GetSyntax(cancellationToken);

                context.ReportDiagnostic(DiagnosticDescriptors.MarkParameterWithInModifier, parameter.Identifier);
            }

            SyntaxWalker.Free(walker);
        }

        private class SyntaxWalker : AssignedExpressionWalker
        {
            [ThreadStatic]
            private static SyntaxWalker _cachedInstance;

            private bool _isInAssignedExpression;
            private int _localFunctionNesting;
            private int _anonymousFunctionNesting;

            public Dictionary<string, IParameterSymbol> Parameters { get; } = new Dictionary<string, IParameterSymbol>();

            public SemanticModel SemanticModel { get; set; }

            public CancellationToken CancellationToken { get; set; }

            public void Reset()
            {
                _isInAssignedExpression = false;
                _localFunctionNesting = 0;
                _anonymousFunctionNesting = 0;

                SemanticModel = null;
                CancellationToken = default;
                Parameters.Clear();
            }

            protected override bool ShouldVisit
            {
                get { return Parameters.Count > 0; }
            }

            public override void VisitIdentifierName(IdentifierNameSyntax node)
            {
                CancellationToken.ThrowIfCancellationRequested();

                string name = node.Identifier.ValueText;

                if (Parameters.TryGetValue(name, out IParameterSymbol parameterSymbol)
                    && SemanticModel.GetSymbol(node, CancellationToken).Equals(parameterSymbol))
                {
                    if (_isInAssignedExpression
                        || _localFunctionNesting > 0
                        || _anonymousFunctionNesting > 0)
                    {
                        Parameters.Remove(name);
                    }
                }

                base.VisitIdentifierName(node);
            }

            public override void VisitAssignedExpression(ExpressionSyntax expression)
            {
                Debug.Assert(!_isInAssignedExpression);

                _isInAssignedExpression = true;
                Visit(expression);
                _isInAssignedExpression = false;
            }

            public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
            {
                _anonymousFunctionNesting++;
                base.VisitAnonymousMethodExpression(node);
                _anonymousFunctionNesting--;
            }

            public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
            {
                _anonymousFunctionNesting++;
                base.VisitSimpleLambdaExpression(node);
                _anonymousFunctionNesting--;
            }

            public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
            {
                _anonymousFunctionNesting++;
                base.VisitParenthesizedLambdaExpression(node);
                _anonymousFunctionNesting--;
            }

            public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
            {
                _localFunctionNesting++;
                base.VisitLocalFunctionStatement(node);
                _localFunctionNesting--;
            }

            public static SyntaxWalker GetInstance()
            {
                SyntaxWalker walker = _cachedInstance;

                if (walker != null)
                {
                    _cachedInstance = null;
                    return walker;
                }
                else
                {
                    return new SyntaxWalker();
                }
            }

            public static void Free(SyntaxWalker walker)
            {
                walker.Reset();
                _cachedInstance = walker;
            }
        }
    }
}
