using MoreMountains.Feedbacks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;

public class GameManager : MonoBehaviour
{
    [SerializeField] GameObject checkPointParent;
    [SerializeField] List<BoosterCard> boosterCards;
    [SerializeField] GameObject aimingPrefab;

    List<PlayerCheckpoint> checkpointList = new List<PlayerCheckpoint>();
    WeaponManager weaponManager;
    BoosterCardManager boosterCardManager;
    Weapon playerStartPistol;
    Ability playerAbility;
    List<BoosterCard> boosterCardsCopy = new List<BoosterCard>();
    List<Weapon> copyWeapon = new List<Weapon>();
    private Camera cameraMain;
    int activeSpawners = 0;

    [SerializeField] List<Character> characters;
    [Header("LootBox")]
    [SerializeField] GameObject buttonObjLeft;
    [SerializeField] GameObject buttonObjRight;
    [SerializeField] GameObject parent;

    LootBoxManager lootBoxManager;

    float gameDuration;

    Character character;
    Character characterCopy;
    /// end testing block 
    private void OnEnable()
    {
        EventBus.StartBattle += StartBattle;
        EventBus.EndBattle += EndBattle;
        EventBus.OnPlayerCheckpoint += PlayerCheckpoint;
        EventBus.EndGameScene += ChangeScene;
        EventBus.ShakeCamera += ShakeCamera;
        EventBus.LootBoxEnd += LootBoxEnd;
    }

    private void OnDisable()
    {
        EventBus.StartBattle -= StartBattle;
        EventBus.EndBattle -= EndBattle;
        EventBus.OnPlayerCheckpoint -= PlayerCheckpoint;
        EventBus.EndGameScene -= ChangeScene;
        EventBus.ShakeCamera -= ShakeCamera;
        EventBus.LootBoxEnd -= LootBoxEnd;
    }

    void Awake()
    {
        character = characters[PlayerPrefs.GetInt(Prefs.pickedCharacter, 0)];
        characterCopy = Instantiate(character);
        LoadWeaponSave();

        GetComponent<PrefabsGlobalAccessPoint>().Init();

                                    //Init checkpoint list
        for(int i = 0; i < checkPointParent.transform.childCount; i++)
        {
            checkpointList.Add(checkPointParent.transform.GetChild(i).GetComponent<PlayerCheckpoint>());
        }
        GameObject playerCharacter;
        //Init player
        if (PlayerPrefs.GetInt(Prefs.alteredSkin + characterCopy.characterName, 0) != 0)
        {
            characterCopy.alteredskin = true;
            playerCharacter = Instantiate(characterCopy.alteredSkinPrefab, checkpointList[0].transform.position, characterCopy.prefab.transform.rotation);
        }
        else
        {
            characterCopy.alteredskin = false;
            playerCharacter = Instantiate(characterCopy.prefab, checkpointList[0].transform.position, characterCopy.prefab.transform.rotation);
        }
        CharacterManager characterManager = playerCharacter.AddComponent<CharacterManager>();
        
        characterCopy.characterManager = characterManager;
         

                                    // Init weapon manager
        weaponManager = gameObject.AddComponent<WeaponManager>();
        weaponManager.Init(playerCharacter.GetComponent<RootBone>().weaponRootBone.transform, aimingPrefab, playerCharacter);
        //copy weapons objects
        for (int i = 0; i < characterCopy.mainWeaponPool.Count; i++)
        {
            copyWeapon.Add(Instantiate(characterCopy.mainWeaponPool[i]));
        }
        playerStartPistol = Instantiate(characterCopy.startPistol);
        playerAbility = Instantiate(characterCopy.ability);
        /////////////////////
        // init player iteration 2
        
        characterManager.Init(checkpointList, characterCopy, weaponManager);


        //Init BoosterCardManager
        boosterCardManager = gameObject.AddComponent<BoosterCardManager>();
        InitBoosterCard();
        for (int i = 0; i < boosterCards.Count; i++)
        {
            boosterCardsCopy.Add(Instantiate(boosterCards[i]));
        }
        boosterCardManager.GetListCards(boosterCardsCopy);




        lootBoxManager = gameObject.AddComponent<LootBoxManager>();
    }

    void LoadWeaponSave()
    {
        foreach(Weapon weapon in characterCopy.mainWeaponPool)
        {
            
            if(PlayerPrefs.GetInt(Prefs.weapon + weapon.name, 0) == 1)
            {
                weapon.unlocked = true;
            }
        }

        foreach(Weapon weapon in characterCopy.pistolPool)
        {
            
            if (PlayerPrefs.GetInt(Prefs.weapon + weapon.name, 0) == 1)
            {
                weapon.unlocked = true;
            }
        }
    }

    private void Start()
    {
        cameraMain = Camera.main;
        weaponManager.GetPistol(playerStartPistol);
        Instantiate(PrefabsGlobalAccessPoint.instance.MusicBackGround, cameraMain.transform);

        IDictionary<string, object> eventData = new Dictionary<string, object>();
        eventData.Add("level", SceneManager.GetActiveScene().buildIndex - 1);
        AppMetrica.Instance.ReportEvent("level_start", eventData);
        AppMetrica.Instance.SendEventsBuffer();
    }

    private void Update()
    {
        gameDuration += Time.deltaTime;
    }

    void InitBoosterCard()
    {
        foreach (Weapon weapon in characterCopy.mainWeaponPool)
        {
            BoosterCard buffer = ScriptableObject.CreateInstance<BoosterCard>();
            buffer.Init(weapon.name, CardType.BasicWeapon, weapon.Card_UI_prefab, weapon);
            boosterCards.Add(buffer);
        }

        foreach (Weapon weapon in characterCopy.pistolPool)
        {
            BoosterCard buffer = ScriptableObject.CreateInstance<BoosterCard>();
            buffer.Init(weapon.name, CardType.Pistol, weapon.Card_UI_prefab, weapon);
            boosterCards.Add(buffer);
        }
    }

    void StartBattle()
    {
        activeSpawners++;
    }

    void EndBattle()
    {
        activeSpawners--;
        if(activeSpawners == 0)
        {
            characterCopy.characterManager.EndBattle();
        }
    }

    bool firstCheck = false;
    void PlayerCheckpoint(PlayerCheckpoint checkpoint)
    {
        if(firstCheck == false)
        {
            firstCheck = true;

            MMF_Fade fade = (MMF_Fade)gameObject.GetComponent<MMF_Player>().FeedbacksList[1];

            fade.TargetPosition = cameraMain.transform.position + cameraMain.transform.forward;
            fade.Active = true;
            fade.Play(gameObject.transform.position);

            return;
            //fade
        }
        if(checkpoint.endGame == true)
        {
            MMF_Fade fade = (MMF_Fade)gameObject.GetComponent<MMF_Player>().FeedbacksList[0];

            fade.TargetPosition = cameraMain.transform.position + cameraMain.transform.forward;
            fade.Active = true;
            fade.Play(gameObject.transform.position);

            GetBox();

            //ChangeScene(EndGameType.win);

            return;
        }
        if(checkpoint.teleportToNextCheck == true)
        {
            StartCoroutine(TeleportFade());
            return;
        }
    }

    void GetBox()
    {
        lootBoxManager.Init(characters, buttonObjLeft, buttonObjRight, parent);
        parent.SetActive(true);
    }

    IEnumerator TeleportFade()
    {
        //yield return new WaitForSeconds(1.5f);

        MMF_Fade fade = (MMF_Fade)gameObject.GetComponent<MMF_Player>().FeedbacksList[0];

        fade.TargetPosition = cameraMain.transform.position + cameraMain.transform.forward;
        fade.Active = true;
        fade.Play(gameObject.transform.position);

        yield return new WaitForSeconds(1f);

        fade = (MMF_Fade)gameObject.GetComponent<MMF_Player>().FeedbacksList[1];

        fade.TargetPosition = cameraMain.transform.position + cameraMain.transform.forward;
        fade.Active = true;
        fade.Play(gameObject.transform.position);

    }

    void LootBoxEnd()
    {
        ChangeScene(EndGameType.win);
    }
    private void ShakeCamera()
    {
        MMF_CameraShake cameraShake = (MMF_CameraShake)gameObject.GetComponent<MMF_Player>().FeedbacksList[2];

        cameraShake.Play(gameObject.transform.position);
    }
    void ChangeScene(EndGameType type)
    {
        switch(type)
        {
            case EndGameType.restart:

                IDictionary<string, object> eventData2 = new Dictionary<string, object>();
                eventData2.Add("level", SceneManager.GetActiveScene().buildIndex);
                eventData2.Add("time_spent", (int)gameDuration);
                eventData2.Add("reason", "restart button");
                AppMetrica.Instance.ReportEvent("level_fail", eventData2);
                AppMetrica.Instance.SendEventsBuffer();

                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                break;
            case EndGameType.loose:

                IDictionary<string, object> eventData3 = new Dictionary<string, object>();
                eventData3.Add("level", SceneManager.GetActiveScene().buildIndex);
                eventData3.Add("time_spent", (int)gameDuration);
                eventData3.Add("reason", "loose");
                AppMetrica.Instance.ReportEvent("level_fail", eventData3);
                AppMetrica.Instance.SendEventsBuffer();

                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                break;
            case EndGameType.win:

                IDictionary<string, object> eventData = new Dictionary<string, object>();
                eventData.Add("level", SceneManager.GetActiveScene().buildIndex - 1);
                eventData.Add("time_spent", (int)gameDuration);
                AppMetrica.Instance.ReportEvent("level_complete", eventData);
                AppMetrica.Instance.SendEventsBuffer();


                EventBus.SaveMoney?.Invoke();
                int level = PlayerPrefs.GetInt(Prefs.level, 2);
                level++;
                PlayerPrefs.SetInt(Prefs.level, level);
                if (level >= SceneManager.sceneCountInBuildSettings)
                {
                    level = 2;
                    PlayerPrefs.SetInt(Prefs.level, level);
                }
                if(level == 3)
                {
                    PlayerPrefs.SetInt(Prefs.InitiateCharacter2Unlock, 1);
                    SceneManager.LoadScene(1);
                    return;
                }
                if(level == 4)
                {
                    PlayerPrefs.SetInt(Prefs.InitiateDailyUnlock, 1);
                    SceneManager.LoadScene(1);
                    return;
                }

                SceneManager.LoadScene(level);
                break;
            case EndGameType.toMenu:
                IDictionary<string, object> eventData4 = new Dictionary<string, object>();
                eventData4.Add("level", SceneManager.GetActiveScene().buildIndex);
                eventData4.Add("time_spent", (int)gameDuration);
                eventData4.Add("reason", "Exit to menu");
                AppMetrica.Instance.ReportEvent("level_fail", eventData4);
                AppMetrica.Instance.SendEventsBuffer();

                SceneManager.LoadScene(1);
                break;
        }
    }
}

public enum EndGameType
{
    restart,
    loose,
    win,
    toMenu
}