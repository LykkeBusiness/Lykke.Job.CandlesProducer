namespace Lykke.Job.CandlesProducer.Contract.Candles;

public enum CandlesErrorCodesContract
{
    None = 0,
    NotFound = 1,
    InvalidLowOrHighPrice = 2,
    ProductNotFound = 3,
    PriceTypeNotSupported = 4,
    TimeIntervalNotSupported = 5,
}