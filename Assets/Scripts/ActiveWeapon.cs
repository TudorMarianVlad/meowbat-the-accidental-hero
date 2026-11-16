using UnityEngine;

// Clasa ActiveWeapon gestioneaza orientarea vizuala a obiectului echipat.
// Actioneaza ca un pivot, orientand sprite-ul armei in functie de pozitia cursorului.
public class ActiveWeapon : MonoBehaviour
{
    [Header("References")]
    // Referinta catre PlayerController pentru a obtine punctul de origine al jucatorului.
    [SerializeField] private PlayerController playerController;

    private void Awake()
    {
        // Initializare defensiva pentru gasirea automata a referintei in ierarhie.
        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>();
    }

    private void Update()
    {
        // Verificam daca arma curenta este un arc (BowWeapon), care necesita rotatie libera 360 grade.
        if (GetComponentInChildren<BowWeapon>() != null)
        {
            // Resetam scala pentru a preveni distorsiuni vizuale la schimbarea armelor.
            transform.localScale = new Vector3(1, 1, 1);

            // Intrerupem executia (Early Exit); BowWeapon isi gestioneaza singur rotatia.
            return;
        }

        // Pentru arme melee sau consumabile, aplicam oglindirea standard.
        FlipWeaponSide();
    }

    // Calculeaza pozitia cursorului si oglindeste (flip) transform-ul armei pe axa X.
    private void FlipWeaponSide()
    {
        // 1. Preluarea si conversia pozitiei cursorului din Screen Space in World Space.
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(mousePos);

        // 2. Obtinerea pozitiei jucatorului ca punct de referinta.
        Vector3 playerPos = playerController.transform.position;

        // 3. Oglindirea sprite-ului in functie de pozitia cursorului fata de jucator.
        if (worldMousePos.x < playerPos.x)
        {
            // Cursor in stanga: inversam scala pe axa X.
            transform.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            // Cursor in dreapta: pastram scala nativa.
            transform.localScale = new Vector3(1, 1, 1);
        }

        // Blocarea rotatiei la 0 pentru a preveni acumularea devierilor nedorite.
        transform.localRotation = Quaternion.identity;
    }
}