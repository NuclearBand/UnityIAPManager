using System.Collections.Generic;

namespace NuclearBand
{
    public interface IAPBank
    {
        List<IAPItem> IAPItems { get; }
    }

    public interface IAPItem
    {
        string ID { get; }
        void OnBuy();
    }
}