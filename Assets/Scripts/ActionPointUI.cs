using UnityEngine;
using StarterAssets;
using NUnit.Framework;
using UnityEngine.UI;
using System.Collections.Generic;

public class ActionPointUI : MonoBehaviour
{
    [UnityEngine.Range(0f, 1f)]
    public float alpha;
    public List<Image> icons = new();
    public GameObject iconPrefab;
    public Transform parent;
    private void Start()
    {
        ActionPointManager.instance.OnAPUpdated += UpdateUI;
        for(int i = 0; i < ActionPointManager.instance.maxAP; i++)
        {
            GameObject tmp = Instantiate(iconPrefab, parent);
            icons.Add(tmp.GetComponent<Image>());
        }
    }

    private void UpdateUI(int apRemaining)
    {
        for(int i =0; i < icons.Count; i++)
        {
            if (i  < apRemaining)
            {
                Debug.Log(apRemaining);

                SetAlpha(icons[i], 1f);
            }
            else
            {
                SetAlpha(icons[i], alpha);
            }
        }
    }

    private void SetAlpha(Image image, float targetAlpha)
    {
        Color c = image.color;
        c.a = targetAlpha;
        image.color = c;
    }
}
