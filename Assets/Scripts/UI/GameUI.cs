using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour
{
    [Header("General")]
    public Image fader;

    [Header("Weapon Selection")]
    public GameObject weaponSelection;
    public Sprite redactedWeaponOption;

    [Header("End Game")]
    public Text endGameText;
    public Image[] endGameButtonImages;

    // Custom font needs to scale via transform
    // Ratio established w/ iPhone 5/5S/5C/SE
    // (scale of 7)/(viewport width AKA Camera.pixelWidth of 1136)
    private readonly float textWidthRatio = 7f/1136f;
    private Dictionary<string, Button> weaponNamesToButtons;
    private Krieger krieger;

    //yucky
    private Coroutine ammoDisplay;
    private Coroutine ammoDisplayFade;

    private void Awake()
    {
        Debug.Assert(fader != null);
        weaponNamesToButtons = new Dictionary<string, Button>();
    }

    private void Start()
    {
        Button[] weaponButtons = weaponSelection.transform.Find("Names").GetComponentsInChildren<Button>();
        foreach(Button b in weaponButtons)
        {
            weaponNamesToButtons.Add(b.name, b);
            b.onClick.AddListener(() => SelectWeapon(b.gameObject.name));
        }

        krieger = Krieger.instance;
        krieger.OnDeath += EndScreenSequenceWrapper;
        krieger.OnShoot += DisplayAmmoWrapper;
        endGameText.transform.localScale = Vector3.one * Camera.main.pixelWidth * textWidthRatio;

        //set to UI so that Fader doesn't affect it during weapon selection
        krieger.SetRendererLayer("UI");
        fader.color = new Color(0f, 0f, 0f, 210f/255f);

        LockUnavailableWeaponsFromPlayerData();

        SelectWeapon(
            "Blaster", 
            initialization:true);

        SelectWeapon(
            "Shovel",
            initialization:true);
    }

    /// <summary>
    /// Locks weapons that player has earned.
    /// </summary>
    private void LockUnavailableWeaponsFromPlayerData()
    {
        float playerDist = PlayerPrefs.GetFloat("Distance", 0);

        Transform rangedOptions = weaponSelection.transform.Find("Names/RangedOptions");
        Transform meleeOptions = weaponSelection.transform.Find("Names/MeleeOptions");
        foreach(Transform t in rangedOptions)
        {
            if(playerDist < Krieger.instance.armory.ranged[Utils.antiLawsuit[t.name]].unlockDistance)
                LockWeapon(t);
        }
        foreach(Transform t in meleeOptions)
        {
            if(playerDist < Krieger.instance.armory.melee[Utils.antiLawsuit[t.name]].unlockDistance)
                LockWeapon(t);
        }
    }

    /// <summary>
    /// Helper func. for LockUnavailableWeaponsFromPlayerData.
    /// Disables button interactability andd swaps sprite.
    /// </summary>
    private void LockWeapon(Transform weaponButtonTrans)
    {
        Button weaponButton = weaponButtonTrans.GetComponent<Button>();
        weaponButton.enabled = false;
        Image weaponButtonImg = weaponButtonTrans.GetComponent<Image>();
        weaponButtonImg.sprite = redactedWeaponOption;
    }

    /// <summary>
    /// Krieger.OnShoot Listener to display current ammo capacity.
    /// </summary>
    private void DisplayAmmoWrapper()
    {
        if(ammoDisplay != null)
            StopCoroutine(ammoDisplay);

        ammoDisplay = StartCoroutine(DisplayAmmo());
    }
    private IEnumerator DisplayAmmo()
    {
        int ammoInClip = krieger.ammoInClip;
        SpriteRenderer ammoRend = krieger.ammoRend;
        RangedWeapon rangedWeapon = krieger.rangedWeapon;

        //cancel previous fade
        if(ammoDisplayFade != null)
            StopCoroutine(ammoDisplayFade);

        //update AmmoCount sprite
        float capacity = (float)ammoInClip / rangedWeapon.clipSize;
        int capacitySprite = (int)Math.Ceiling(capacity * (rangedWeapon.ammoCounter.Length-1));
        ammoRend.sprite = rangedWeapon.ammoCounter[capacitySprite];

        //make visible
        float displayTime = 1f;   
        float timer = Time.time;
        ammoRend.color = Color.white;
        while(Time.time - timer < displayTime)
            yield return null;

        //do fade
        ammoDisplayFade = StartCoroutine(Utils.Fade(
            ammoRend,
            Color.white,
            Color.clear,
            0.25f));
        yield return ammoDisplayFade;
        ammoDisplayFade = null;
    }

    /// <summary>
    /// OnClick Listener for weapon selection buttons.
    /// Also used to initialize Krieger weapons and buttons.
    /// </summary>
    private void SelectWeapon(
        string buttonName,
        bool initialization=false)
    {
        string realName = Utils.antiLawsuit[buttonName];
        if(realName.Contains("gun"))
        {
            if(krieger.startingRangedWeapon != realName)
            {
                //unhighlight previous weapon
                if(!String.IsNullOrEmpty(krieger.startingRangedWeapon))
                {
                    weaponNamesToButtons[Utils.antiLawsuit[krieger.startingRangedWeapon]].interactable = true;
                }

                //highlight new weapon
                krieger.startingRangedWeapon = realName;
                weaponNamesToButtons[buttonName].interactable = false;
            }

            if(!initialization && 
                krieger.isMelee)
            {
                krieger.anim.SetTrigger("SwitchWeapon");
                krieger.weaponAnim.SetTrigger("SwitchWeapon");
            }
        }
        else
        {
            if(krieger.startingMeleeWeapon != realName)
            {
                //unhighlight previous weapon
                if(!String.IsNullOrEmpty(krieger.startingMeleeWeapon))
                {
                    weaponNamesToButtons[Utils.antiLawsuit[krieger.startingMeleeWeapon]].interactable = true;
                }

                //highlight new weapon
                krieger.startingMeleeWeapon = realName;
                weaponNamesToButtons[buttonName].interactable = false;
            }

            if(!initialization &&
                !krieger.isMelee)
            {
                krieger.anim.SetTrigger("SwitchWeapon");
                krieger.weaponAnim.SetTrigger("SwitchWeapon");
            }
        }
    }

    /// <summary>
    /// OnClick Listener for Ready button (WeaponSelection Menu).
    /// </summary>
    public void Ready()
    {
        StartCoroutine(Utils.Fade(
            fader,
            fader.color,
            Color.clear,
            0.1f));
        weaponSelection.SetActive(false);
        krieger.SetRendererLayer("Gameground");
        GameState.instance.isReady = true;
    }

    /// <summary>
    /// OnClick Listener for Redeploy button.
    /// </summary>
    public void RedeployWrapper(){StartCoroutine(Redeploy());}
    private IEnumerator Redeploy()
    {
        //fade out text and buttons before loading main menu
        foreach(Image img in endGameButtonImages)
        {
            StartCoroutine(
                Utils.Fade(
                    img,
                    Color.white,
                    Color.clear,
                    1f));
            img.GetComponent<Button>().interactable = false;
        }

        yield return StartCoroutine(
            Utils.Fade(
                endGameText,
                Color.white,
                Color.clear,
                1f));

        SceneManager.LoadScene("Game");
    }

    /// <summary>
    /// OnClick Listener for Back button.
    /// </summary>
    public void BackWrapper(){StartCoroutine(Back());}
    private IEnumerator Back()
    {
        //fade out text and buttons before loading main menu
        foreach(Image img in endGameButtonImages)
        {
            StartCoroutine(
                Utils.Fade(
                    img,
                    Color.white,
                    Color.clear,
                    1f));
            img.GetComponent<Button>().interactable = false;
        }

        yield return StartCoroutine(
            Utils.Fade(
                endGameText,
                Color.white,
                Color.clear,
                1f));

        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Subscriber to Krieger.OnDeath Event. Starts the end screen sequence.
    /// </summary>
    private void EndScreenSequenceWrapper() {StartCoroutine(EndScreenSequence());}
    private IEnumerator EndScreenSequence() 
    {
        GameObject endGameElements = endGameText.transform.parent.parent.gameObject;
        endGameElements.SetActive(true);

        //fader screen comes in
        yield return StartCoroutine(
            Utils.Fade(
                fader,
                Color.clear,
                Color.black,
                1f));

        //text and buttons fade in together
        foreach(Image img in endGameButtonImages)
        {
            StartCoroutine(
                Utils.Fade(
                    img,
                    Color.clear,
                    Color.white,
                    1f));
        }
        
        endGameText.text = 
            $"ENEMIES KILLED: {GameState.instance.enemiesDefeated}\n" +
            $"DISTANCE WALKED: {GameState.instance.feetTraversed} FT";
        yield return StartCoroutine(
            Utils.Fade(
                endGameText,
                Color.clear,
                Color.white,
                1f));

        //buttons made interactable after
        foreach(Image img in endGameButtonImages)
        {
            img.GetComponent<Button>().interactable = true;
        }
    }
}