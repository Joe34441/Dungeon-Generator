using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartRoom : MonoBehaviour
{
    private int layer = -1;

    private void OnTriggerEnter(Collider other)
    {
        FindObjectOfType<MazeGenerator>().UpdateLayer(layer);
    }

    public int GetLayer() { return layer; }
    public void SetLayer(int _layer) { layer = _layer; }
}
