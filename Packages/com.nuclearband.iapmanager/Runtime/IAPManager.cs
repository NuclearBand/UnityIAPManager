﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Purchasing;

namespace NuclearBand
{
    public class IAPManager : IStoreListener
    {
        public static event Action<string, decimal, string, string?>? OnPurchase;
        public static event Action? OnInit;
        public static List<IAPItem> IAPItems = null!;
        private static IAPManager instance = null!;
        private static Action? OnPurchaseSuccess;
        private static Action? OnPurchaseFail;
        private static IStoreController? controller;
        private static IExtensionProvider extensions = null!;
        public static bool Initialized => controller != null;

        public static void Init(List<IAPItem> iapItems)
        {
            IAPItems = iapItems;
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            foreach (var iapItem in IAPItems)
                builder.AddProduct(iapItem.ID, ProductType.Consumable, new IDs
                {
                    { iapItem.ID, GooglePlay.Name },
                    { iapItem.ID, MacAppStore.Name }
                });

            instance = new IAPManager();
            UnityPurchasing.Initialize(instance, builder);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            IAPManager.controller = controller;
            IAPManager.extensions = extensions;
            OnInit?.Invoke();
        }

        public static void Purchase(string productId, Action? onSuccess, Action? onFail)
        {
#if UNITY_EDITOR
            var iapItem = IAPItems.Find(item => item.ID == productId);
            if (iapItem == null)
            {
                onFail?.Invoke();
                Debug.LogError("IAPManager: ProcessPurchase did not find item");
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
                Debug.Log("IAPManager: BuyProductID FAIL. Not initialized.");
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


        public static void RestorePurchases(Action? onSuccess, Action? onFail)
        {
#if UNITY_IOS
            extensions.GetExtension<IAppleExtensions>().RestoreTransactions(result => OnRestorePurchases(result, onSuccess, onFail);
            );
#endif

#if UNITY_ANDROID
            extensions.GetExtension<IGooglePlayStoreExtensions>().RestoreTransactions(result => OnRestorePurchases(result, onSuccess, onFail));
#endif
        }

        private static void OnRestorePurchases(bool result, Action? onSuccess, Action? onFail)
        {
            if (result)
                onSuccess?.Invoke();
            else
                onFail?.Invoke();
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            var iapItem = IAPItems.Find(item => item.ID == args.purchasedProduct.definition.id);
            if (iapItem == null)
            {
                OnPurchaseFail?.Invoke();
                Debug.LogError("IAPManager: ProcessPurchase did not find item");
                return PurchaseProcessingResult.Pending;
            }

            Debug.Log($"IAPManager: ProcessPurchase: PASS. Product: '{args.purchasedProduct.definition.id}'");

            iapItem.OnBuy();

            OnPurchaseSuccess?.Invoke();
            OnPurchaseSuccess = null;

            OnPurchase?.Invoke(args.purchasedProduct.definition.id, args.purchasedProduct.metadata.localizedPrice, args.purchasedProduct.metadata.isoCurrencyCode, args.purchasedProduct.hasReceipt ? args.purchasedProduct.receipt : null);
            return PurchaseProcessingResult.Complete;
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError($"IAPManager: OnInitializeFailed - {error}");
        }

        public void OnPurchaseFailed(Product i, PurchaseFailureReason p)
        {
            Debug.LogError($"IAPManager: OnPurchaseFailed - {p}");
            OnPurchaseFail?.Invoke();
        }

        public static Product? GetProductInfo(string productId)
        {
            return controller?.products.all.FirstOrDefault(item => item.definition.id == productId);
        }

        public static IAPItem GetIAPItem(string productId)
        {
            return IAPItems.FirstOrDefault(item => item.ID == productId);
        }
    }
}