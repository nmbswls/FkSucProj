using System.Collections;
using System.Collections.Generic;
using My.Map.Scene;
using My.UI;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class SceneInteractUIHinter : MonoBehaviour
    {
        public ISceneInteractable sceneInteract;

        public bool IsExpanded = false;

        public int SelectIdx = 0;

        //public GameObject SelectItemPrefab;
        public Transform ShowRoot;

        public void Bind(ISceneInteractable sceneInteract)
        {
            this.sceneInteract = sceneInteract;
            //this.BindInteractPoint.EventOnInteractStateChanged += OnExpandStateChanged;
            gameObject.SetActive(true);
        }


        public void Unbind()
        {
            //BindInteractPoint.EventOnInteractStateChanged -= OnExpandStateChanged;
            sceneInteract = null;
            gameObject.SetActive(false);

            //OnExpandStateChanged(false);
        }

    }
}


