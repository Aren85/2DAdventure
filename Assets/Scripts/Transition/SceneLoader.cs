using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour, ISaveable
{
    public Transform playerTrans;
    public Vector3 firstPosition;
    public Vector3 menuPosition;

    [Header("事件監聽")]
    public SceneLoadEventSO loadEventSO;
    public VoidEventSO newGameEvent;

    [Header("廣播")]
    public VoidEventSO afterSceneLoadedEvent;
    public FadeEventSO fadeEvent;
    public SceneLoadEventSO unloadedSceneEvent;

    [Header("場景")]
    public GameSceneSO firstLoadScene;
    public GameSceneSO menuScene;
    private GameSceneSO currentLoadScene;
    private GameSceneSO sceneToLoad;
    private Vector3 positionToGo;
    private bool fadeScreen;
    private bool isLoading;
    public float fadeDuration;

    private void Awake()
    {
        //Addressables.LoadSceneAsync(LV1LoadScene.sceneReference, LoadSceneMode.Additive);
        //currentLoadScene = firstLoadScene;
        //currentLoadScene.sceneReference.LoadSceneAsync(LoadSceneMode.Additive);


    }
    //TODO:做完MainMenu之後更改
    private void Start()
    {
        loadEventSO.RaiseLoadRequestEvent(menuScene, menuPosition, true);
        //NewGame();
    }
    private void OnEnable()
    {
        loadEventSO.LoadRequestEvent += OnLoadRequestEvent;
        newGameEvent.OnEventRaised += NewGame;

        ISaveable saveable = this;
        saveable.RegisterSaveData();
    }
    private void OnDisable()
    {
        loadEventSO.LoadRequestEvent -= OnLoadRequestEvent;
        newGameEvent.OnEventRaised -= NewGame;

        ISaveable saveable = this;
        saveable.UnRegisterSaveData();
    }

    private void NewGame()
    {
        sceneToLoad = firstLoadScene;
        //OnLoadRequestEvent(sceneToLoad, firstPosition, true);
        loadEventSO.RaiseLoadRequestEvent(sceneToLoad, firstPosition, true);
    }

    /// <summary>
    /// 場景加載事件請求
    /// </summary>
    /// <param name="locationToLoad"></param>
    /// <param name="posToGo"></param>
    /// <param name="fadeScreen"></param>

    private void OnLoadRequestEvent(GameSceneSO locationToLoad, Vector3 posToGo, bool fadeScreen)
    {
        if (isLoading) { return; }
        isLoading = true;
        sceneToLoad = locationToLoad;
        positionToGo = posToGo;
        this.fadeScreen = fadeScreen;
        if (currentLoadScene != null)
        {
            StartCoroutine(UnLoadPreviousScene());
        }
        else
        {
            LoadNewScene();
        }
    }

    private IEnumerator UnLoadPreviousScene()
    {
        if (fadeScreen)
        {
            //TODO:實現漸入漸出(變黑)
            fadeEvent.FadeIn(fadeDuration);
        }
        yield return new WaitForSeconds(fadeDuration);

        //廣播事件調整血量調顯示
        unloadedSceneEvent.RaiseLoadRequestEvent(sceneToLoad, positionToGo, true);

        yield return currentLoadScene.sceneReference.UnLoadScene();
        //關閉人物
        playerTrans.gameObject.SetActive(false);
        //加載新場景
        LoadNewScene();
    }

    private void LoadNewScene()
    {
        var loadingOption = sceneToLoad.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true);
        loadingOption.Completed += OnLoadCompleted;
    }

    /// <summary>
    /// 場景加載完成後
    /// </summary>
    /// <param name="handle"></param>

    private void OnLoadCompleted(AsyncOperationHandle<SceneInstance> handle)
    {
        currentLoadScene = sceneToLoad;

        playerTrans.position = positionToGo;

        playerTrans.gameObject.SetActive(true);
        if (fadeScreen)
        {
            //TODO:
            fadeEvent.FadeOut(fadeDuration);
        }

        isLoading = false;

        if (currentLoadScene.sceneType == SceneType.Loaction)
        {
            //場景加載完成後事件
            afterSceneLoadedEvent.RaiseEvent();
        }
    }

    public DataDefinition GetDataID()
    {
        return GetComponent<DataDefinition>();
    }

    public void GetSaveData(Data data)
    {
        data.SaveGameScene(currentLoadScene);
    }

    public void LoadData(Data data)
    {
        var playerID = playerTrans.GetComponent<DataDefinition>().ID;
        if (data.characterPosDict.ContainsKey(playerID))
        {
            positionToGo = data.characterPosDict[playerID];
            sceneToLoad = data.GetSavedScene();

            OnLoadRequestEvent(sceneToLoad, positionToGo, true);
        }
    }
}
