// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    [Flags]
    internal enum DeclarationModifiers
    {
        None = 0,
        Abstract = 1 << 0,
        Sealed = 1 << 1,
        Static = 1 << 2,
        New = 1 << 3,
        Public = 1 << 4,
        Protected = 1 << 5,
        Internal = 1 << 6,
        ProtectedInternal = 1 << 7, // the two keywords together are treated as one modifier
        Private = 1 << 8,
        ReadOnly = 1 << 9,
        Const = 1 << 10,
        Volatile = 1 << 11,

        Extern = 1 << 12,
        Partial = 1 << 13,
        Unsafe = 1 << 14,
        Fixed = 1 << 15,
        Virtual = 1 << 16, // used for method binding
        Override = 1 << 17, // "

        Indexer = 1 << 18, // not a real modifier, but used to record that indexer syntax was used. Sharing this bit with PrimaryCtor.
        PrimaryCtor = 1 << 18, // not a real modifier, but used to record that this is a primary constructor. Sharing this bit with Indexer.

        Async = 1 << 19,
        Implicit = 1 << 20,

        All = (Implicit | (Implicit - 1)), // all modifiers
        Unset = 1 << 21, // used when a modifiers value hasn't yet been computed

        AccessibilityMask = Private | Protected | Internal | ProtectedInternal | Public,
    }
}
