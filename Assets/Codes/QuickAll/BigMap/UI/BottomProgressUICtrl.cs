using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BottomProgressUICtrl : MonoBehaviour
{
    public TextMeshProUGUI hintTextComp;
    public Image ProgressBar;

    public float TargetProgress = 0.3f;
    private float _valCounter = 0;

    private bool isPlaying = false;
    private long playingShowId;

    private bool isFading = false;
    private float fadingTimer = 0;

    public static long ShowInstIdCounter = 10;

    public void Update()
    {

        if(isFading)
        {
            fadingTimer -= Time.deltaTime;
            if(fadingTimer < 0)
            {
                fadingTimer = -1;
                isFading = false;

                if(!isPlaying)
                {
                    this.gameObject.SetActive(false);
                }
            }
        }


        if(TargetProgress < 0)
        {
            return;
        }
        _valCounter += Time.deltaTime;
        ProgressBar.fillAmount = _valCounter / TargetProgress;
        if (_valCounter >= TargetProgress)
        {
            OnProgressComplete();
        }
    }

    public long InitProgressInfo(string hintText, float targetProgress)
    {
        if(isPlaying)
        {
            Debug.Log("InitProgressInfo duplicate ");
        }
        this.TargetProgress = targetProgress;
        _valCounter = 0;

        hintTextComp.text = hintText;
        ProgressBar.fillAmount = 0;

        isFading = false;
        fadingTimer = -1;
        isPlaying = true;

        this.gameObject.SetActive(true);

        ++ShowInstIdCounter;
        this.playingShowId = ShowInstIdCounter;
        return playingShowId;
    }

    public void OnProgressComplete()
    {
        this.gameObject.SetActive(false);
    }

    public void TryCancelProgressComplete(long showId)
    {
        if(!isPlaying)
        {
            return;
        }

        if(showId != playingShowId)
        {
            return;
        }

        hintTextComp.text = "Cancel";
        this.fadingTimer = 0.3f;
        this.isFading = true;

    }
}
