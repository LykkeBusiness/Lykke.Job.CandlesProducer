// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.CandlesProducer.Settings
{
    public class CqrsContextNamesSettings
    {
        [Optional] public string CorporateActionsBroker { get; set; } = nameof(CorporateActionsBroker);
        [Optional] public string CandlesProducer { get; set; } = nameof(CandlesProducer);
        [Optional] public string CandlesHistoryWriter { get; set; } = nameof(CandlesHistoryWriter);
    }
}
