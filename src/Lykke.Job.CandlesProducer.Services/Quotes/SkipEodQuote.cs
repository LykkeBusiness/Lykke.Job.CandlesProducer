// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Lykke.Job.CandlesProducer.Services.Quotes
{
    public class SkipEodQuote
    {
        public bool Value { get; }

        public SkipEodQuote(bool value)
        {
            Value = value;
        }

        public static implicit operator bool(SkipEodQuote skipEodQuote)
        {
            return skipEodQuote.Value;
        }
    }
}