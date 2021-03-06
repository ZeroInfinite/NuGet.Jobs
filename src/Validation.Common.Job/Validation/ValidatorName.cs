﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Jobs.Validation
{
    public static class ValidatorName
    {
        public const string Vcs = "VcsValidator";
        public const string PackageCertificate = "PackageCertificatesValidator";
        public const string ScanAndSign = "ScanAndSign";
        public const string ScanOnly = "ScanOnly";
        public const string PackageSignatureProcessor = "PackageSigningValidator";
        public const string PackageSignatureValidator = "PackageSigningValidator2";

        public const string SymbolScan = "SymbolScan";

        public const string SymbolsValidator = "SymbolsValidator";
        public const string SymbolsIngester = "SymbolsIngester";
    }
}
