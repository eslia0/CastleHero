﻿using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviour
{
    public const int waitData = 12;
    public const int battleData = 1;

    public bool[] dataCheck;
    [SerializeField] bool loadEnd;
    [SerializeField] GameManager.Scene currentScene;

    GameManager gameManager;
    NetworkManager networkManager;
    UIManager uiManager;
    BattleManager battleManager;

    public GameManager.Scene CurrentScene { get { return currentScene; } }

    void Awake()
    {
        tag = "LoadingManager";
        DontDestroyOnLoad(transform.gameObject);
    }
    
    //매니저 초기화
    public void ManagerInitialize()
    {
        uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<UIManager>();
        networkManager = GameObject.FindGameObjectWithTag("NetworkManager").GetComponent<NetworkManager>();
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        battleManager = GameObject.FindGameObjectWithTag("BattleManager").GetComponent<BattleManager>();
    }

    public void InitializeDataCheck()
    {
        for(int i =0; i< dataCheck.Length; i++)
        {
            dataCheck[i] = false;
        }
    }

    public IEnumerator LoadingEndCheck(GameManager.Scene scene)
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            for (int i = 0; i < dataCheck.Length; i++)
            {
                if (dataCheck[i])
                {
                    loadEnd = true;
                }
                else
                {
                    loadEnd = false;
                    yield return null;
                }
            }

            if (loadEnd)
            {
                if (scene == GameManager.Scene.Login)
                {
                    gameManager.ManagerDestory();
                }

                currentScene = scene;
                SceneManager.LoadScene((int)scene);
                StopAllCoroutines();
            }
        }
    }

    public void LoadWaitScene()
    {
        StopAllCoroutines();

        dataCheck = new bool[waitData];
        InitializeDataCheck();
        loadEnd = false;

        StartCoroutine(LoadHeroData());
        StartCoroutine(LoadSkillData());
        StartCoroutine(LoadItemData());
        StartCoroutine(LoadUnitData());
        StartCoroutine(LoadBuildingData());
        StartCoroutine(LoadUpgradeData());
        StartCoroutine(LoadResourceData());
        StartCoroutine(LoadStateData());
        StartCoroutine(LoadBuildData());
        StartCoroutine(LoadUnitCreateData());
        StartCoroutine(LoadMyPositionData());
        StartCoroutine(LoadPlaceData());
        StartCoroutine(LoadingEndCheck(GameManager.Scene.Wait));
    }

    public void LoadLoginScene()
    {
        StopAllCoroutines();

        dataCheck = new bool[1];
        InitializeDataCheck();
        loadEnd = false;

        StartCoroutine(InitializeData());
        StartCoroutine(LoadingEndCheck(GameManager.Scene.Login));
    }

    public void LoadBattleScene()
    {
        StopAllCoroutines();

        dataCheck = new bool[battleData];
        InitializeDataCheck();
        loadEnd = false;

        StartCoroutine(LoadEnemyUnitDataRequest());
        StartCoroutine(LoadingEndCheck(GameManager.Scene.Battle));
    }

    public IEnumerator LoadHeroData()
    {
        while (!dataCheck[(int) ServerPacketId.HeroData - 4])
        {
            networkManager.DataRequest(ClientPacketId.HeroDataRequest);
            yield return new WaitForSeconds(1f);
        }
    }

    public IEnumerator LoadSkillData()
    {
        while (!dataCheck[(int)ServerPacketId.SkillData - 4])
        {
            networkManager.DataRequest(ClientPacketId.SkillDataRequest);
            yield return new WaitForSeconds(1f);
        }
    }

    public IEnumerator LoadItemData()
    {
        while (!dataCheck[(int)ServerPacketId.ItemData - 4])
        {
            networkManager.DataRequest(ClientPacketId.ItemDataRequest);
            yield return new WaitForSeconds(1f);
        }
    }

    public IEnumerator LoadUnitData()
    {
        while (!dataCheck[(int)ServerPacketId.UnitData - 4])
        {
            networkManager.DataRequest(ClientPacketId.UnitDataRequest);
            yield return new WaitForSeconds(1f);
        }
    }

    public IEnumerator LoadBuildingData()
    {
        while (!dataCheck[(int)ServerPacketId.BuildingData - 4])
        {
            networkManager.DataRequest(ClientPacketId.BuildingDataRequest);
            yield return new WaitForSeconds(1f);
        }
    }

    public IEnumerator LoadUpgradeData()
    {
        while (!dataCheck[(int)ServerPacketId.UpgradeData - 4])
        {
            networkManager.DataRequest(ClientPacketId.UpgradeDataRequest);
            yield return new WaitForSeconds(1f);
        }
    }

    public IEnumerator LoadResourceData()
    {
        while (!dataCheck[(int)ServerPacketId.ResourceData - 4])
        {
            networkManager.DataRequest(ClientPacketId.ResourceDataRequest);
            yield return new WaitForSeconds(1f);
        }
    }

    public IEnumerator LoadStateData()
    {
        while (!dataCheck[(int)ServerPacketId.StateData - 4])
        {
            networkManager.DataRequest(ClientPacketId.StateDataRequest);
            yield return new WaitForSeconds(1f);
        }
    }

    public IEnumerator LoadBuildData()
    {
        while (!dataCheck[(int)ServerPacketId.BuildData - 4])
        {
            networkManager.DataRequest(ClientPacketId.BuildDataRequest);
            yield return new WaitForSeconds(1f);
        }
    }

    public IEnumerator LoadUnitCreateData()
    {
        while (!dataCheck[(int)ServerPacketId.UnitCreateData - 4])
        {
            networkManager.DataRequest(ClientPacketId.UnitCreateDataRequest);
            yield return new WaitForSeconds(1f);
        }
    }

    public IEnumerator LoadMyPositionData()
    {
        while (!dataCheck[(int)ServerPacketId.MyPositionData - 4])
        {
            networkManager.DataRequest(ClientPacketId.MyPositionDataRequest);
            yield return new WaitForSeconds(1f);
        }
    }

    public IEnumerator LoadPlaceData()
    {
        while (!dataCheck[(int)ServerPacketId.PlaceData - 4])
        {
            networkManager.DataRequest(ClientPacketId.PlaceDataRequest);
            yield return new WaitForSeconds(1f);
        }
    }

    public IEnumerator LoadEnemyUnitDataRequest()
    {
        while (!dataCheck[(int)ServerPacketId.EnemyUnitData - 17])
        {
            networkManager.EnemyUnitDataRequest(battleManager.GetAttackPos());
            yield return new WaitForSeconds(1f);
        }
    }

    public IEnumerator InitializeData()
    {
        while (!dataCheck[0])
        {
            dataCheck[0] = true;
            yield return new WaitForSeconds(1f);
        }
    }

    public IEnumerator LoadScene(GameManager.Scene prevScene, GameManager.Scene NextScene, float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        SceneManager.LoadScene((int) GameManager.Scene.Loading);
        currentScene = GameManager.Scene.Loading;

        if (NextScene == GameManager.Scene.Wait)
        {
            LoadWaitScene();
        }
        else if (NextScene == GameManager.Scene.Login)
        {
            LoadLoginScene();
        }
        else if (NextScene == GameManager.Scene.Battle)
        {
            LoadBattleScene();
        }
    }

    void OnLevelWasLoaded(int level)
    {
        if (level == (int)GameManager.Scene.Wait)
        {
            uiManager.SetState();
            uiManager.SetWaitUIObject();
            uiManager.SetUnitScrollView();
            uiManager.WaitSceneAddListener();
            uiManager.InformationPanel.SetActive(false);
            StartCoroutine(uiManager.BuildTimeCheck());
            StartCoroutine(uiManager.UnitCreateTimeCheck());
        }
        else if (level == (int)GameManager.Scene.Battle)
        {
            battleManager.SetSpawnPoint();
            battleManager.SetBattleStage();
        }
    }
}