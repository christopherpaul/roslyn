// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalScopeBinder : Binder
    {
        private ImmutableArray<LocalSymbol> locals;
        private ImmutableArray<LabelSymbol> labels;

        protected readonly MethodSymbol Owner;

        internal LocalScopeBinder(Binder next)
            : this(null, next)
        {
        }

        internal LocalScopeBinder(MethodSymbol owner, Binder next)
            : this(owner, next, next.Flags)
        {
        }

        internal LocalScopeBinder(MethodSymbol owner, Binder next, BinderFlags flags)
            : base(next, flags)
        {
            this.Owner = owner;
        }

        internal override Symbol ContainingMemberOrLambda
        {
            get
            {
                return this.Owner ?? Next.ContainingMemberOrLambda;
            }
        }

        internal sealed override ImmutableArray<LocalSymbol> Locals
        {
            get
            {
                if (this.locals.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref this.locals, BuildLocals(), default(ImmutableArray<LocalSymbol>));
                }

                return this.locals;
            }
        }

        protected virtual ImmutableArray<LocalSymbol> BuildLocals()
        {
            return ImmutableArray<LocalSymbol>.Empty;
        }

        protected ImmutableArray<LocalSymbol> BuildLocals(StatementSyntax stmt, bool isRoot = true)
        {
            Debug.Assert(stmt != null);
            var walker = new BuildLocalsFromDeclarationsWalker(this, isRoot ? stmt : null);

            walker.Visit(stmt);

            if (walker.Locals != null)
            {
                return walker.Locals.ToImmutableAndFree();
            }

            return ImmutableArray<LocalSymbol>.Empty;
        }

        internal sealed override ImmutableArray<LabelSymbol> Labels
        {
            get
            {
                if (this.labels.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref this.labels, BuildLabels(), default(ImmutableArray<LabelSymbol>));
                }

                return this.labels;
            }
        }

        protected virtual ImmutableArray<LabelSymbol> BuildLabels()
        {
            return ImmutableArray<LabelSymbol>.Empty;
        }

        private SmallDictionary<string, LocalSymbol> lazyLocalsMap;
        private SmallDictionary<string, LocalSymbol> LocalsMap
        {
            get
            {
                if (this.lazyLocalsMap == null && this.Locals.Length > 0)
                {
                    this.lazyLocalsMap = BuildMap(this.Locals);
                }

                return this.lazyLocalsMap;
            }
        }

        private SmallDictionary<string, LabelSymbol> lazyLabelsMap;
        private SmallDictionary<string, LabelSymbol> LabelsMap
        {
            get
            {
                if (this.lazyLabelsMap == null && this.Labels.Length > 0)
                {
                    this.lazyLabelsMap = BuildMap(this.Labels);
                }

                return this.lazyLabelsMap;
            }
        }

        private static SmallDictionary<string, TSymbol> BuildMap<TSymbol>(ImmutableArray<TSymbol> array)
            where TSymbol : Symbol
        {
            Debug.Assert(array.Length > 0);

            var map = new SmallDictionary<string, TSymbol>();

            // NOTE: in a rare case of having two symbols with same name the one closer to the array's start wins.
            for (int i = array.Length - 1; i >= 0; i--)
            {
                var symbol = array[i];
                map[symbol.Name] = symbol;
            }

            return map;
        }

        protected ImmutableArray<LocalSymbol> BuildLocals(SyntaxList<StatementSyntax> statements)
        {
            var walker = new BuildLocalsFromDeclarationsWalker(this);

            foreach (var statement in statements)
            {
                walker.Visit(statement);
            }

            if (walker.Locals != null)
                {
                return walker.Locals.ToImmutableAndFree();
                }

            return ImmutableArray<LocalSymbol>.Empty;
        }

        protected class BuildLocalsFromDeclarationsWalker : CSharpSyntaxWalker 
                {
            public readonly LocalScopeBinder Binder;
            public readonly StatementSyntax RootStmtOpt;
            public ArrayBuilder<LocalSymbol> Locals;

            public BuildLocalsFromDeclarationsWalker(LocalScopeBinder binder, StatementSyntax rootStmtOpt = null)
            {
                this.Binder = binder;
                this.RootStmtOpt = rootStmtOpt;
                }

            public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
            {
                if (Locals == null)
                {
                    Locals = ArrayBuilder<LocalSymbol>.GetInstance();
            }

                LocalDeclarationKind kind;
                bool isImplicit = false;

                switch (node.Parent.CSharpKind())
            {
                    case SyntaxKind.LocalDeclarationStatement:
                        var localDecl = (LocalDeclarationStatementSyntax)node.Parent;
                        kind = localDecl.IsConst ? LocalDeclarationKind.Constant :
                                                  localDecl.IsFixed ? LocalDeclarationKind.Fixed :
                                                                     LocalDeclarationKind.Variable;
                        isImplicit = localDecl.IsImplicit;
                        break;

                    case SyntaxKind.ForStatement:
                        kind = LocalDeclarationKind.For;
                        break;

                    case SyntaxKind.UsingStatement:
                        kind = LocalDeclarationKind.Using;
                        break;

                    case SyntaxKind.FixedStatement:
                        kind = LocalDeclarationKind.Fixed;
                        break;

                    default:
                        throw ExceptionUtilities.Unreachable;
            }

                foreach (var vdecl in node.Variables)
                {
                    var localSymbol = SourceLocalSymbol.MakeLocal(
                        Binder.Owner,
                        Binder,
                        node.Type,
                        vdecl.Identifier,
                        vdecl.Initializer,
                        kind,
                        isImplicit);
                    Locals.Add(localSymbol);

                    Visit(vdecl.Initializer);
                }
        }

            public override void VisitDeclarationExpression(DeclarationExpressionSyntax node)
            {
                if (Locals == null)
                {
                    Locals = ArrayBuilder<LocalSymbol>.GetInstance();
                }

                var localSymbol = SourceLocalSymbol.MakeLocal(
                    Binder.Owner,
                    Binder,
                    node.Type,
                    node.Variable.Identifier,
                    node.Variable.Initializer,
                    LocalDeclarationKind.Variable,
                    isImplicit: false);

                Locals.Add(localSymbol);

                Visit(node.Variable.Initializer);
            }

            public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
            {
                Debug.Assert(false);
                base.VisitVariableDeclarator(node);
            }

            public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
            {
                return;
            }

            public override void VisitBlock(BlockSyntax node)
            {
                if (RootStmtOpt != node)
                {
                    return;
                }

                base.VisitBlock(node);
            }

            public override void VisitForStatement(ForStatementSyntax node)
            {
                if (RootStmtOpt != node)
                {
                    return;
                }

                throw ExceptionUtilities.Unreachable;
            }

            public override void VisitUsingStatement(UsingStatementSyntax node)
            {
                if (RootStmtOpt != node)
                {
                    return;
                }

                Visit(node.Declaration);
                Visit(node.Expression);
            }

            public override void VisitFixedStatement(FixedStatementSyntax node)
            {
                if (RootStmtOpt != node)
                {
                    return;
                }

                Visit(node.Declaration);
            }

            public override void VisitSwitchStatement(SwitchStatementSyntax node)
            {
                if (RootStmtOpt != node)
                {
                    return;
                }

                throw ExceptionUtilities.Unreachable;
            }

            public override void VisitForEachStatement(ForEachStatementSyntax node)
            {
                if (RootStmtOpt != node)
                {
                    return;
                }

                throw ExceptionUtilities.Unreachable;
            }

            public override void VisitWhileStatement(WhileStatementSyntax node)
            {
                if (RootStmtOpt != node)
                {
                    return;
                }

                Visit(node.Condition);
            }

            public override void VisitDoStatement(DoStatementSyntax node)
            {
                if (RootStmtOpt != node)
                {
                    return;
                }

                Visit(node.Condition);
            }

            public override void VisitCatchClause(CatchClauseSyntax node)
            {
                return;
            }

            public override void VisitIfStatement(IfStatementSyntax node)
            {
                if (RootStmtOpt != node)
                {
                    return;
                }

                Visit(node.Condition);
            }
        }

        protected void BuildLabels(SyntaxList<StatementSyntax> statements, ref ArrayBuilder<LabelSymbol> labels)
        {
            foreach (var statement in statements)
            {
                var stmt = statement;
                while (stmt.Kind == SyntaxKind.LabeledStatement)
                {
                    var labeledStatement = (LabeledStatementSyntax)stmt;
                    if (labels == null)
                    {
                        labels = ArrayBuilder<LabelSymbol>.GetInstance();
                    }

                    var labelSymbol = new SourceLabelSymbol(this.Owner, labeledStatement.Identifier);
                    labels.Add(labelSymbol);
                    stmt = labeledStatement.Statement;
                }
            }
        }

        /// <summary>
        /// Call this when you are sure there is a local declaration on this token.  Returns the local.
        /// </summary>
        protected override SourceLocalSymbol LookupLocal(SyntaxToken nameToken)
        {
            LocalSymbol result = null;
            if (LocalsMap != null && LocalsMap.TryGetValue(nameToken.ValueText, out result))
            {
                if (result.IdentifierToken == nameToken) return (SourceLocalSymbol)result;

                // in error cases we might have more than one declaration of the same name in the same scope
                foreach (var local in this.Locals)
                {
                    if (local.IdentifierToken == nameToken)
                    {
                        return (SourceLocalSymbol)local;
                    }
                }
            }

            return base.LookupLocal(nameToken);
        }

        protected override bool CanHaveMultipleMeanings(string name)
        {
            var singelMeaningTable = this.singleMeaningTable;
            if (singelMeaningTable != null)
            {
                lock (singleMeaningTable)
                {
                    if (singleMeaningTable.ContainsKey(name))
                    {
                        // we already have reasons for tracking this name
                        return true;
                    }
                }
            }

            return base.CanHaveMultipleMeanings(name);
        }

        protected override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<Symbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(options.AreValid());
            Debug.Assert(result.IsClear);

            if ((options & LookupOptions.LabelsOnly) != 0)
            {
                var labelsMap = this.LabelsMap;
                if (labelsMap != null)
                {
                    LabelSymbol labelSymbol;
                    if (labelsMap.TryGetValue(name, out labelSymbol))
                    {
                        result.MergeEqual(LookupResult.Good(labelSymbol));
                    }
                }
                return;
            }

            var localsMap = this.LocalsMap;
            if (localsMap != null && (options & LookupOptions.NamespaceAliasesOnly) == 0)
            {
                LocalSymbol localSymbol;
                if (localsMap.TryGetValue(name, out localSymbol))
                {
                    result.MergeEqual(originalBinder.CheckViability(localSymbol, arity, options, null, diagnose, ref useSiteDiagnostics, basesBeingResolved));
                }
            }
        }

        protected override void LookupImplicitSymbolsInSingleBinder(
            LookupResult result, TypeSymbol targetType, ConsList<Symbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(options.AreValid());
            Debug.Assert(result.IsClear);

            var locals = this.Locals;
            for (int i = 0; i < locals.Length; i++)
            {
                LocalSymbol localSymbol = locals[i];
                if (localSymbol.IsImplicit)
                {
                    Conversions conversions = originalBinder.Conversions;
                    TypeSymbol localSymbolType = localSymbol.Type;
                    bool typeMatches = conversions.HasConversionForImplicitParameter(localSymbolType, targetType, ref useSiteDiagnostics);
                    if (typeMatches)
                    {
                        result.MergeEqual(LookupResult.Good(localSymbol));
                    }
                }
            }
        }

        protected override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            Debug.Assert(options.AreValid());

            if ((options & LookupOptions.LabelsOnly) != 0)
            {
                if (this.LabelsMap != null)
                {
                    foreach (var label in this.LabelsMap)
                    {
                        result.AddSymbol(label.Value, label.Key, 0);
                    }
                }
            }
            else if (this.LocalsMap != null && options.CanConsiderLocals())
            {
                foreach (var local in this.LocalsMap)
                {
                    if (originalBinder.CanAddLookupSymbolInfo(local.Value, options, null))
                    {
                        result.AddSymbol(local.Value, local.Key, 0);
                    }
                }
            }
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(CSharpSyntaxNode node)
        {
            throw ExceptionUtilities.Unreachable;
        }
    }
}
