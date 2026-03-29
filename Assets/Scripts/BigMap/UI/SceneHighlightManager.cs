

using System.Collections.Generic;
using System.Linq;

public interface IHighlightableObj
{
    void SetHighlightStatus(bool isHighlight);
}

public class SceneHighlightManager
{
    private Dictionary<IHighlightableObj, List<int>> HighlightStatsDict = new();

    private static SceneHighlightManager _instance;
    public static SceneHighlightManager Instance
    {
        get
        {
            if (_instance == null)
            {
                if (_instance == null)
                    _instance = new SceneHighlightManager();
            }
            return _instance;
        }
    }

    public void SetHighlight(IHighlightableObj highlightableObj, int highlightReason)
    {
        HighlightStatsDict.TryGetValue(highlightableObj, out var statList);
        if (statList == null)
        {
            statList = new();
            HighlightStatsDict[highlightableObj] = statList;
        }

        if (statList.Contains(highlightReason))
        {
            statList.Add(highlightReason);
        }

        highlightableObj.SetHighlightStatus(true);
    }

    public void CancelHighlight(IHighlightableObj highlightableObj, int highlightReason)
    {
        HighlightStatsDict.TryGetValue(highlightableObj, out var statList);
        if (statList == null)
        {
            return;
        }

        statList.Remove(highlightReason);
        if (statList.Count == 0)
        {
            HighlightStatsDict.Remove(highlightableObj);
            if (highlightableObj != null)
            {
                highlightableObj.SetHighlightStatus(false);
            }
        }
    }

    public void ClearAllHighlightByReason(int highlightReason)
    {
        foreach (var k in HighlightStatsDict.Keys.ToList())
        {
            HighlightStatsDict[k].Remove(highlightReason);
            if (HighlightStatsDict[k].Count == 0)
            {
                HighlightStatsDict.Remove(k);
                k.SetHighlightStatus(false);
            }
        }
    }

    public void ClearAllHighlight()
    {
        foreach (var kv in HighlightStatsDict)
        {
            kv.Key.SetHighlightStatus(false);
        }
    }
}