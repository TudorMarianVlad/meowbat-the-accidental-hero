using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Gestioneaza elementele grafice si logica de input pentru un rand din magazin.
// Actioneaza ca punte intre datele obiectului (ItemData) si ShopManager.
public class ShopRowUI : MonoBehaviour
{
    [Header("UI Component References")]
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemPriceText;
    public Button buyButton;

    private ItemData currentItem;
    private ShopManager shopManager;

    // Configureaza randul cu datele obiectului si referinta catre manager.
    public void SetupRow(ItemData item, ShopManager manager)
    {
        currentItem = item;
        shopManager = manager;

        // Actualizare vizuala elemente.
        itemIcon.sprite = item.icon;

        // Logica afisare nume (formatare speciala pentru munitie).
        if (item.itemType == ItemType.Ammo)
        {
            itemNameText.text = item.itemName + " <color=#FFD24D>x10</color>";
        }
        else
        {
            itemNameText.text = item.itemName;
        }

        itemPriceText.text = item.buyPrice.ToString();

        // Resetare ascultatori pentru a preveni declansarea evenimentelor multiple (recycling pattern).
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClicked);
    }

    // Callback pentru executarea achizitiei prin intermediul ShopManager.
    void OnBuyClicked()
    {
        shopManager.BuyItem(currentItem);
    }
}