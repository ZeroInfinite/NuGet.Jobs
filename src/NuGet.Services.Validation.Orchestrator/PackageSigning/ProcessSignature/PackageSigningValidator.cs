﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Jobs.Validation;
using NuGet.Jobs.Validation.PackageSigning.Storage;
using NuGet.Jobs.Validation.Storage;
using NuGet.Services.Validation.Orchestrator.Telemetry;

namespace NuGet.Services.Validation.PackageSigning.ProcessSignature
{
    /// <summary>
    /// The validator that ensures the package's repository signature is valid.
    /// </summary>
    [ValidatorName(ValidatorName.PackageSigningValidator)]
    public class PackageSigningValidator : BaseProcessSignature, IValidator
    {
        private readonly IValidatorStateService _validatorStateService;
        private readonly IProcessSignatureEnqueuer _signatureVerificationEnqueuer;
        private readonly ISimpleCloudBlobProvider _blobProvider;
        private readonly ITelemetryService _telemetryService;
        private readonly ILogger<PackageSigningValidator> _logger;

        public PackageSigningValidator(
            IValidatorStateService validatorStateService,
            IProcessSignatureEnqueuer signatureVerificationEnqueuer,
            ISimpleCloudBlobProvider blobProvider,
            ITelemetryService telemetryService,
            ILogger<PackageSigningValidator> logger)
          : base(validatorStateService, signatureVerificationEnqueuer, blobProvider, telemetryService, logger)
        {
            _validatorStateService = validatorStateService ?? throw new ArgumentNullException(nameof(validatorStateService));
            _signatureVerificationEnqueuer = signatureVerificationEnqueuer ?? throw new ArgumentNullException(nameof(signatureVerificationEnqueuer));
            _blobProvider = blobProvider ?? throw new ArgumentNullException(nameof(blobProvider));
            _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override bool RequiresRepositorySignature => true;

        public override async Task<IValidationResult> GetResultAsync(IValidationRequest request)
        {
            var result = await base.GetResultAsync(request);

            return Validate(result);
        }

        public override async Task<IValidationResult> StartAsync(IValidationRequest request)
        {
            var result = await base.StartAsync(request);

            return Validate(result);
        }

        private IValidationResult Validate(IValidationResult result)
        {
            /// The package signing validator runs after the <see cref="PackageSigningProcessor" />.
            /// All author signing validation issues should have been caught by the processor, so a failed validation
            /// should only happen if the repository signature is invalid. In addition, the Process Signature job
            /// will only modify the package if the repository signature is unacceptable.
            if (result.Status == ValidationStatus.Failed || result.NupkgUrl != null)
            {
                _logger.LogCritical(
                    "Unexpected validation result in package signing validator. This may be caused by an invalid repository " +
                    "signature. Status = {ValidationStatus}, Nupkg URL = {NupkgUrl}, validation issues = {Issues}",
                    result.Status,
                    result.NupkgUrl,
                    result.Issues.Select(i => i.IssueCode));

                throw new InvalidOperationException("Package signing validator has an unexpected validation result");
            }

            /// Suppress all validation issues. The <see cref="PackageSigningProcessor"/> should
            /// have already reported any issues related to the author signature. Customers should
            /// not be notified of validation issues due to the repository signature.
            if (result.Issues.Count != 0)
            {
                _logger.LogWarning(
                    "Ignoring {ValidationIssueCount} validation issues from result. Issues: {Issues}",
                    result.Issues.Count,
                    result.Issues.Select(i => i.IssueCode));

                return new ValidationResult(result.Status);
            }

            return result;
        }
    }
}