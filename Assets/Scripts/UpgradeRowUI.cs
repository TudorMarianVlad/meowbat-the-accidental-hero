using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Gestioneaza elementele grafice ale unui rand din magazinul de upgrade-uri.
// Actioneaza ca interfata pasiva (Passive View) pentru UpgradeShopkeeper.
public class UpgradeRowUI : MonoBehaviour
{
    // Referinte catre componentele UI.
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Button buyButton;

    // Actualizeaza pretul afisat in UI dupa fiecare achizitie (Data Injection).
    public void UpdatePrice(int newPrice)
    {
        priceText.text = newPrice.ToString();
    }
}