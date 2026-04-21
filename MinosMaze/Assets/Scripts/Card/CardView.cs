using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CardView : MonoBehaviour
{
    [SerializeField] private TMP_Text Name;
    [SerializeField] private TMP_Text Cost;
    [SerializeField] private TMP_Text Description;

    [SerializeField] private GameObject wrapper;

    [SerializeField] private SpriteRenderer image;
    [SerializeField] private SpriteRenderer background;
}
