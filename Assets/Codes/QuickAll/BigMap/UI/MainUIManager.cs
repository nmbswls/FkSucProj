using Bag;
using Map.Logic.Events;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEditorInternal.VersionControl;
using UnityEngine;

public class MainUIManager : MonoBehaviour
{

    public static MainUIManager Instance;
    public Camera UICamera;
    public Canvas RootCanvas;

    public Transform HintFloatingPanel;

    public UISceneInteractMenu SceneInteractMenu;

    private void Awake()
    {
        Instance = this;

        InteractHintPrefab.SetActive(false);
        //InventoryUICtrl.gameObject.SetActive(false);
        //LootUICtrl.gameObject.SetActive(false);
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.B))
        {
            if(!InventoryUICtrl.gameObject.activeSelf)
            {
                InventoryUICtrl.InitilaizeView();
                InventoryUICtrl.gameObject.SetActive(true);
            }
        }
    }

    private void LateUpdate()
    {
        //OnRefreshInteractInfo();
    }


    public void InitGameLogicEventListener()
    {
        MainGameManager.Instance.gameLogicManager.LogicEventBus.Subscribe(new CommonGameEventAdapter((ev) =>
        {
            switch(ev.Name)
            {
                case "Death":
                    {
                        var guy = ev.Param3;
                        var presenter = SceneAOIManager.Instance.GetActivePresentation(guy);
                        if (presenter != null)
                        {
                            FakeHintTextManager.ShowWorld("imdead", presenter.GetWorldPosition(), Camera.main);
                        }
                    }
                    break;

            }

        }));
    }

    
    public GameObject InteractHintPrefab;
    private Dictionary<long, SceneInteractUIHinter> sceneInteractHintDicts = new(0);
    private Queue<SceneInteractUIHinter> _hintPool = new();

    public void OnScenePresentationBinded(IScenePresentation scenePresentation)
    {
        if(scenePresentation is ISceneInteractable interactPoint)
        {
            SceneInteractUIHinter hint = null;
            if(_hintPool.Count > 0)
            {
                hint = _hintPool.Dequeue();
            }
            else
            {
                var newHintGo = GameObject.Instantiate(InteractHintPrefab, HintFloatingPanel);
                hint = newHintGo.GetComponent<SceneInteractUIHinter>();
            }
            hint.InitBind(interactPoint);

            hint.BindInteractPoint = interactPoint;
            hint.gameObject.SetActive(true);
            sceneInteractHintDicts[interactPoint.Id] = hint;

            hint.transform.position = scenePresentation.GetWorldPosition();
            hint.transform.localPosition = new Vector3(hint.transform.localPosition.x, hint.transform.localPosition.y, 0);
        }
    }

    public void OnScenePresentationUbbind(IScenePresentation scenePresentation)
    {
        if (scenePresentation is ISceneInteractable interactPoint)
        {
            sceneInteractHintDicts.TryGetValue(scenePresentation.Id, out var hintItem);
            if(hintItem != null)
            {
                hintItem.Clear();
                hintItem.gameObject.SetActive(false);
                sceneInteractHintDicts.Remove(scenePresentation.Id);

                if(_hintPool.Count < 10)
                {
                    _hintPool.Enqueue(hintItem);
                }
                else
                {
                    GameObject.Destroy(hintItem.gameObject);
                }
            }
        }
    }

    public void OnInteractPointStatusChanged(InteractPointPresenter presenter)
    {

    }

    public void OnInteractExpandItemChanged(ISceneInteractable newInteract)
    {

    }


    public BottomProgressUICtrl ProgressUICtrl;

    public long ShowBottomProgress(string hintText, float progressTime)
    {
        return ProgressUICtrl.InitProgressInfo(hintText, progressTime);
    }

    public void TryCancelProgressComplete(long showId)
    {
        ProgressUICtrl.TryCancelProgressComplete(showId);
    }

    public InventoryUIController InventoryUICtrl;
    public LootPointUIController LootUICtrl;

    public bool IsLootingMode;

    public void TryEnterLootDetailMode(ILootableObj lootObj)
    {
        if(!IsLootingMode)
        {
            IsLootingMode = true;
            LootUICtrl.InitializeContent(lootObj);
            LootUICtrl.gameObject.SetActive(true);

            // 如未打开背包 打开
            if (!InventoryUICtrl.gameObject.activeSelf)
            {
                InventoryUICtrl.InitilaizeView();
                InventoryUICtrl.gameObject.SetActive(true);
            }

            ItemPopupMenu.Instance.Close();
        }
    }

    public void TryQuitLootDetailMode()
    {
        if (IsLootingMode)
        {
            IsLootingMode = false;

            LootUICtrl.gameObject.SetActive(false);
            InventoryUICtrl.gameObject.SetActive(false);

            ItemPopupMenu.Instance.Close();
        }
    }
}
