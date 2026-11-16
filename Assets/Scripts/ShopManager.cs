using System.Collections.Generic;
using UnityEngine;
using TMPro; 

// Clasa ShopManager coordoneaza interfata magazinului, navigarea prin pagini,
// filtrarea stocurilor pe categorii si validarea tranzactiilor financiare de achizitie.
public class ShopManager : MonoBehaviour
{
    [Header("Shop Inventory")]
    // Liste de date ce stocheaza metadatele ScriptableObjects pentru fiecare categorie de obiecte disponibila la comerciant
    public List<ItemData> weaponsStock;
    public List<ItemData> armorStock;
    public List<ItemData> consumablesStock;

    [Header("UI References")]
    // Tablou de componente ce reprezinta cele 4 randuri vizuale instantiate in Layout Group-ul din Canvas
    public ShopRowUI[] uiRows;
    public TextMeshProUGUI pageText;

    [Header("Player References")]
    public PlayerStats playerStats;
    public Inventory playerInventory;

    // Referinta generica catre lista curenta selectata (Weapons, Armor sau Consumables)
    private List<ItemData> currentList;
    private int currentPage = 0;
    // Constanta arhitecturala care defineste capacitatea maxima de randare a unei pagini
    private int itemsPerPage = 4;

    void Start()
    {
        // La initializare, magazinul deschide automat categoria armelor (indexul 0)
        OpenCategory(0);
    }

    // Filtreaza stocul si schimba lista de referinta in functie de tabul apasat in interfata
    public void OpenCategory(int categoryIndex)
    {
        if (categoryIndex == 0) currentList = weaponsStock;
        else if (categoryIndex == 1) currentList = armorStock;
        else if (categoryIndex == 2) currentList = consumablesStock;

        // Resetam intotdeauna indexul paginii la 0 (pagina 1) pentru a preveni erorile de depasire a limitelor listei noi
        currentPage = 0;
        RefreshShopUI();
    }

    // Avanseaza cu o pagina in interfata grafica
    public void NextPage()
    {
        // Calculam numarul maxim de pagini rotunjind in sus raportul dintre marimea stocului si elementele permise per pagina
        int maxPages = Mathf.CeilToInt((float)currentList.Count / itemsPerPage);

        // Permitem incrementarea paginii doar daca nu am atins deja limita superioara
        if (currentPage < maxPages - 1)
        {
            currentPage++;
            RefreshShopUI();
        }
    }

    // Revine la pagina anterioara in interfata grafica
    public void PreviousPage()
    {
        // Permitem decrementarea doar daca nu ne aflam deja pe prima pagina (index 0)
        if (currentPage > 0)
        {
            currentPage--;
            RefreshShopUI();
        }
    }

    // Functia principala de sincronizare grafica si calculare a indicilor de paginare
    public void RefreshShopUI()
    {
        // Determinam numarul maxim de pagini, asigurandu-ne prin Mathf.Max ca valoarea este cel putin 1 chiar daca lista e goala
        int maxPages = Mathf.Max(1, Mathf.CeilToInt((float)currentList.Count / itemsPerPage));
        if (pageText != null) pageText.text = $"{currentPage + 1} of {maxPages}";

        // Iteram prin cele 4 randuri de interfata stabilite prin arhitectura ferestrei
        for (int i = 0; i < uiRows.Length; i++)
        {
            // Calculam indexul real al obiectului din stoc care corespunde randului i de pe pagina curenta
            int itemIndex = (currentPage * itemsPerPage) + i;

            // Verificam daca indexul calculat se afla in limitele listei de stoc active
            if (itemIndex < currentList.Count)
            {
                // Cazul A: Exista un obiect valid pentru acest rand -> Activam randul si ii injectam datele
                uiRows[i].gameObject.SetActive(true);
                uiRows[i].SetupRow(currentList[itemIndex], this);
            }
            else
            {
                // Cazul B: Stocul s-a epuizat pe aceasta pagina -> Ascundem randurile ramase libere
                // pentru a nu lasa elemente UI cu date reziduale sau goale pe ecran.
                uiRows[i].gameObject.SetActive(false);
            }
        }
    }

    // Gestioneaza logica tranzactionala de cumparare a unui obiect
    public void BuyItem(ItemData itemToBuy)
    {
        // Validarea fondurilor
        if (playerStats.currentGold >= itemToBuy.buyPrice)
        {
            // Stabilirea cantitatii
            // Daca obiectul este munitie (Ammo), 
            // jucatorul cumpara un pachet de 10 unitati; pentru orice alt tip de obiect, cantitatea este 1.
            int amountToGive = (itemToBuy.itemType == ItemType.Ammo) ? 10 : 1;

            // Validarea spatiului din inventar
            // Apelam AddItem din clasa Inventory. Aceasta scaneaza automat sloturile pentru stivuire 
            // sau cauta un slot liber, returnand True daca stocarea a reusit si False daca inventarul e plin.
            if (playerInventory.AddItem(itemToBuy, amountToGive))
            {
                // Retinerea fondurilor
                // Retragem aurul din contul jucatorului abia dupa ce avem garantia ca obiectul a fost depozitat fizic in inventar
                playerStats.SpendGold(itemToBuy.buyPrice);

                Debug.Log($"Bought {amountToGive} {itemToBuy.itemName} for {itemToBuy.buyPrice} gold!");
            }
            else
            {
                // Tratare in consola pentru cazul in care AddItem returneaza false (Inventar plin)
                Debug.Log("Inventory is full!");
            }
        }
        else
        {
            // Tratare in consola pentru cazul in care aurul curent este mai mic decat buyPrice
            Debug.Log("Not enough gold!");
        }
    }
}