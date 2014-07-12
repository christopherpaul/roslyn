﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class WrappedParameterSymbol : ParameterSymbol
    {
        protected readonly ParameterSymbol underlyingParameter;

        protected WrappedParameterSymbol(ParameterSymbol underlyingParameter)
        {
            Debug.Assert((object)underlyingParameter != null);

            this.underlyingParameter = underlyingParameter;
        }

        public abstract override Symbol ContainingSymbol
        {
            get;
        }

        public override ParameterSymbol OriginalDefinition
        {
            get { return this; }
        }

        #region Forwarded

        public override TypeSymbol Type
        {
            get { return underlyingParameter.Type; }
        }

        public sealed override RefKind RefKind
        {
            get { return underlyingParameter.RefKind; }
        }

        internal sealed override bool IsMetadataIn
        {
            get { return underlyingParameter.IsMetadataIn; }
        }

        internal sealed override bool IsMetadataOut
        {
            get { return underlyingParameter.IsMetadataOut; }
        }

        public sealed override ImmutableArray<Location> Locations
        {
            get { return underlyingParameter.Locations; }
        }

        public sealed override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return underlyingParameter.DeclaringSyntaxReferences; }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return underlyingParameter.GetAttributes();
        }

        internal override void AddSynthesizedAttributes(ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            underlyingParameter.AddSynthesizedAttributes(ref attributes);
        }

        internal sealed override ConstantValue ExplicitDefaultConstantValue
        {
            get { return underlyingParameter.ExplicitDefaultConstantValue; }
        }

        public override int Ordinal
        {
            get { return underlyingParameter.Ordinal; }
        }

        public override bool IsParams
        {
            get { return underlyingParameter.IsParams; }
        }

        internal override bool IsMetadataOptional
        {
            get { return underlyingParameter.IsMetadataOptional; }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return underlyingParameter.IsImplicitlyDeclared; }
        }

        internal override bool IsImplicit
        {
            get { return underlyingParameter.IsImplicit; }
        }

        public sealed override string Name
        {
            get { return underlyingParameter.Name; }
        }

        public override ImmutableArray<CustomModifier> CustomModifiers
        {
            get { return underlyingParameter.CustomModifiers; }
        }

        internal override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get { return underlyingParameter.MarshallingInformation; }
        }

        internal override UnmanagedType MarshallingType
        {
            get { return underlyingParameter.MarshallingType; }
        }

        internal override bool IsIDispatchConstant
        {
            get { return underlyingParameter.IsIDispatchConstant; }
        }

        internal override bool IsIUnknownConstant
        {
            get { return underlyingParameter.IsIUnknownConstant; }
        }

        internal override bool IsCallerLineNumber
        {
            get { return underlyingParameter.IsCallerLineNumber; }
        }

        internal override bool IsCallerFilePath
        {
            get { return underlyingParameter.IsCallerFilePath; }
        }

        internal override bool IsCallerMemberName
        {
            get { return underlyingParameter.IsCallerMemberName; }
        }

        internal sealed override bool HasByRefBeforeCustomModifiers
        {
            get { return underlyingParameter.HasByRefBeforeCustomModifiers; }
        }

        #endregion
    }
}
