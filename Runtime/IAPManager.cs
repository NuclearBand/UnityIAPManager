using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

namespace NuclearBand
{
    public class IAPManager : IStoreListener
    {
        public static event Action<string, decimal, string, string> OnPurchase;

        public static IAPBank IAPBank;
        private static IAPManager instance;
        private static Action OnPurchaseSuccess;
        private static Action OnPurchaseFail;
        private static IStoreController controller;
        private static IExtensionProvider extensions;
        public static bool Initialized => controller != null;

        public static void Init(IAPBank iapBank)
        {
            IAPBank = iapBank;
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            foreach (var iapItem in iapBank.IAPItems)
                builder.AddProduct(iapItem.ID, ProductType.Consumable, new IDs
                {
                    {iapItem.ID, GooglePlay.Name},
                    {iapItem.ID, MacAppStore.Name}
                });

            instance = new IAPManager();
            UnityPurchasing.Initialize(instance, builder);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            IAPManager.controller = controller;
            IAPManager.extensions = extensions;
        }

        public static void Purchase(string productId, Action onSuccess, Action onFail)
        {
#if UNITY_EDITOR
            var iapItem = IAPBank.IAPItems.Find(item => item.ID == productId);
            if (iapItem == null)
            {
                onFail?.Invoke();
                Debug.LogError("ProcessPurchase did not find item");
                return;
            }
            iapItem.OnBuy();
            onSuccess?.Invoke();
            return;
#endif
            OnPurchaseSuccess = onSuccess;
            OnPurchaseFail = onFail;
            if (controller == null)
            {
                Debug.Log("BuyProductID FAIL. Not initialized.");
                OnPurchaseFail?.Invoke();
                return;
            }

            var product = controller.products.WithID(productId);
            if (product != null && product.availableToPurchase)
                controller.InitiatePurchase(product);
            else
            {
                Debug.Log("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
                OnPurchaseFail?.Invoke();
            }
        }


        public static void RestorePurchases()
        {
            extensions.GetExtension<IAppleExtensions>().RestoreTransactions(result =>
            {
                if (result)
                {
                }
                else
                {
                }
            });
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            OnPurchase?.Invoke(args.purchasedProduct.definition.id, args.purchasedProduct.metadata.localizedPrice, args.purchasedProduct.metadata.isoCurrencyCode, args.purchasedProduct.hasReceipt ? args.purchasedProduct.receipt : null);
            var iapItem = IAPBank.IAPItems.Find(item => item.ID == args.purchasedProduct.definition.id);
            if (iapItem == null)
            {
                OnPurchaseFail?.Invoke();
                Debug.LogError("ProcessPurchase did not find item");
                return PurchaseProcessingResult.Pending;
            }

            Debug.Log($"ProcessPurchase: PASS. Product: '{args.purchasedProduct.definition.id}'");

            iapItem.OnBuy();
            
            OnPurchaseSuccess?.Invoke();
            OnPurchaseSuccess = null;

            return PurchaseProcessingResult.Complete;
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError("OnInitializeFailed");
        }

        public void OnPurchaseFailed(Product i, PurchaseFailureReason p)
        {
            Debug.LogError("OnPurchaseFailed");
            OnPurchaseFail?.Invoke();
        }

        public static Product GetProductInfo(string productId)
        {
            return controller.products.all.FirstOrDefault(item => item.definition.id == productId);
        }

        public static IAPItem GetIAPItem(string productId)
        {
            return IAPBank.IAPItems.FirstOrDefault(item => item.ID == productId);
        }
    }
}