using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RailFlasher : MonoBehaviour
{
    private List<Renderer> renderers = new List<Renderer>();
    private List<Color> originalColors = new List<Color>();

    public Color flashColor = Color.red;
    public float flashInterval = 0.5f;

    private Coroutine flashCoroutine;

    void Awake()
    {
        // Находим все Renderer'ы в дочерних объектах (включая сам объект)
        renderers.AddRange(GetComponentsInChildren<Renderer>());

        // Сохраняем их оригинальные цвета
        foreach (var rend in renderers)
        {
            if (rend.material.HasProperty("_Color"))
                originalColors.Add(rend.material.color);
            else
                originalColors.Add(Color.white); // безопасная заглушка
        }
    }

    public void StartFlashing()
    {
        if (flashCoroutine == null)
            flashCoroutine = StartCoroutine(FlashRoutine());
    }

    public void StopFlashing()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
            RestoreOriginalColors();
        }
    }

    private IEnumerator FlashRoutine()
    {
        while (true)
        {
            SetColorToAll(flashColor);
            yield return new WaitForSeconds(flashInterval);
            RestoreOriginalColors();
            yield return new WaitForSeconds(flashInterval);
        }
    }

    private void SetColorToAll(Color color)
    {
        foreach (var rend in renderers)
        {
            if (rend != null && rend.material.HasProperty("_Color"))
                rend.material.color = color;
        }
    }

    private void RestoreOriginalColors()
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = originalColors[i];
        }
    }
}
