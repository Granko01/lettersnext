using UnityEngine;
using UnityEngine.Purchasing;

public class IAPManager : MonoBehaviour
{
    public MenuManager menuManager;

    private const string EnergyKey = "PlayerEnergy";
    private const string MaxEnergyKey = "PlayerMaxEnergy";

    private void Start()
    {
        if (menuManager == null)
        {
            Debug.LogError("⚠️ MenuManager reference missing in IAPManager!");
            return;
        }

        // Load saved values
        menuManager.Energies = PlayerPrefs.GetInt(EnergyKey, menuManager.Energies);
        menuManager.MaxEnergy = PlayerPrefs.GetInt(MaxEnergyKey, menuManager.MaxEnergy);

        menuManager.SetEnergy();
        menuManager.UpdateUI();
    }

    public void OnPurchaseComplete(Product purchasedProduct)
    {
        if (menuManager == null)
        {
            Debug.LogError("⚠️ MenuManager reference missing in IAPManager!");
            return;
        }

        switch (purchasedProduct.definition.id)
        {
            case "com.bidatalab.lettersnext.energyrefill":
                Debug.Log("✅ Energy Refill Purchase successful!");
                if (menuManager.Energies < menuManager.MaxEnergy)
                {
                    menuManager.Energies++;
                    SaveEnergyData();
                    menuManager.SetEnergy();
                    menuManager.UpdateUI();
                    menuManager.BuyButton[0].gameObject.SetActive(false);
                }
                else
                {
                    Debug.Log("FULL");
                }
                break;

            case "com.bidatalab.lettersnext.energyslot":
                Debug.Log("✅ Energy Slot Purchase successful!");
                menuManager.MaxEnergy++;
                menuManager.Energies = menuManager.MaxEnergy;
                SaveEnergyData();
                menuManager.SetEnergy();
                menuManager.UpdateUI();
                menuManager.BuyButton[1].gameObject.SetActive(false);
                break;

            default:
                Debug.LogWarning("⚠️ Unknown product purchased: " + purchasedProduct.definition.id);
                break;
        }
    }

    public void OnPurchaseFailed(Product purchasedProduct, PurchaseFailureReason reason)
    {
        Debug.LogError($"❌ Purchase failed for {purchasedProduct.definition.id}: {reason}");
    }

    private void SaveEnergyData()
    {
        PlayerPrefs.SetInt(EnergyKey, menuManager.Energies);
        PlayerPrefs.SetInt(MaxEnergyKey, menuManager.MaxEnergy);
        PlayerPrefs.Save();
    }
}
