using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class IAPManager : MonoBehaviour, IDetailedStoreListener
{
    public MenuManager menuManager;
    public PlayerManager playerManager;

    private IStoreController storeController;

    private const string EnergyKey = "PlayerEnergy";
    private const string MaxEnergyKey = "PlayerMaxEnergy";

    private const string ProductEnergyRefill = "com.bidatalab.lettersnext.energyrefill";
    private const string ProductEnergySlot   = "com.bidatalab.lettersnext.energyslot";
    private const string ProductVIP          = "com.bidatalab.lettersnext.vip";

    private void Start()
    {
        if (menuManager == null)
        {
            Debug.LogError("⚠️ MenuManager reference missing in IAPManager!");
            return;
        }

        menuManager.Energies = PlayerPrefs.GetInt(EnergyKey, menuManager.Energies);
        menuManager.MaxEnergy = PlayerPrefs.GetInt(MaxEnergyKey, menuManager.MaxEnergy);
        menuManager.SetEnergy();
        menuManager.UpdateUI();

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.AddProduct(ProductEnergyRefill, ProductType.Consumable);
        builder.AddProduct(ProductEnergySlot,   ProductType.Consumable);
        builder.AddProduct(ProductVIP,          ProductType.NonConsumable);
        UnityPurchasing.Initialize(this, builder);
    }

    public void BuyEnergyRefill() => Purchase(ProductEnergyRefill);
    public void BuyEnergySlot()   => Purchase(ProductEnergySlot);
    public void BuyVIP()          => Purchase(ProductVIP);

    private void Purchase(string productId)
    {
        if (storeController == null)
        {
            Debug.LogError("❌ Store not initialized yet.");
            return;
        }
        storeController.InitiatePurchase(productId);
    }

    // Called by Unity IAP when store is ready
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        Debug.Log("✅ IAP Initialized.");
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError("❌ IAP Init failed: " + error);
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError($"❌ IAP Init failed: {error} — {message}");
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        switch (args.purchasedProduct.definition.id)
        {
            case ProductEnergyRefill:
                Debug.Log("✅ Energy Refill purchased!");
                menuManager.Energies = menuManager.MaxEnergy;
                SaveEnergyData();
                menuManager.SetEnergy();
                menuManager.UpdateUI();
                if (menuManager.BuyButton.Length > 0)
                    menuManager.BuyButton[0].gameObject.SetActive(false);
                break;

            case ProductEnergySlot:
                Debug.Log("✅ Energy Slot purchased!");
                menuManager.MaxEnergy++;
                menuManager.Energies = menuManager.MaxEnergy;
                SaveEnergyData();
                menuManager.SetEnergy();
                menuManager.UpdateUI();
                if (menuManager.BuyButton.Length > 1)
                    menuManager.BuyButton[1].gameObject.SetActive(false);
                break;

            case ProductVIP:
                Debug.Log("✅ VIP purchased!");
                menuManager.SetVIP(true);
                menuManager.SaveVIPToPlayFab();
                menuManager.Energies = menuManager.MaxEnergy;
                SaveEnergyData();
                menuManager.SetEnergy();
                menuManager.UpdateUI();
                if (playerManager != null)
                    playerManager.RefreshNameWithCrown();
                if (menuManager.BuyButton.Length > 2)
                    menuManager.BuyButton[2].gameObject.SetActive(false);
                break;

            default:
                Debug.LogWarning("⚠️ Unknown product: " + args.purchasedProduct.definition.id);
                break;
        }

        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
    {
        Debug.LogError($"❌ Purchase failed for {product.definition.id}: {reason}");
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription description)
    {
        Debug.LogError($"❌ Purchase failed for {product.definition.id}: {description.reason} — {description.message}");
    }

    private void SaveEnergyData()
    {
        PlayerPrefs.SetInt(EnergyKey, menuManager.Energies);
        PlayerPrefs.SetInt(MaxEnergyKey, menuManager.MaxEnergy);
        PlayerPrefs.Save();
    }
}
