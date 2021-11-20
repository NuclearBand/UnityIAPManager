#nullable enable

namespace NuclearBand
{
    public interface IAPItem
    {
        string ID { get; }
        void OnBuy();
    }
}