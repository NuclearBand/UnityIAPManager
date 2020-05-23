using System.Collections.Generic;

namespace NuclearBand
{
    public interface IAPItem
    {
        string ID { get; }
        void OnBuy();
    }
}