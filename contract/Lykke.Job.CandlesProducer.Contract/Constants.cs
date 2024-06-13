// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

using JetBrains.Annotations;

namespace Lykke.Job.CandlesProducer.Contract
{
    /// <summary>
    /// Contract constants
    /// </summary>
    [PublicAPI]
    public static class Constants
    {
        /// <summary>
        /// Semver compatible contract version, but only major and minor parts are used
        /// </summary>
        public static readonly Version ContractVersion = new Version(4, 1, 1);
    }
}
