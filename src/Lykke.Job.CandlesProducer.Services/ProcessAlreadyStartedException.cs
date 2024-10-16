using System;

namespace Lykke.Job.CandlesProducer.Services;

public sealed class ProcessAlreadyStartedException : Exception
{
    public ProcessAlreadyStartedException(string message) : base(message)
    {
    }
}
