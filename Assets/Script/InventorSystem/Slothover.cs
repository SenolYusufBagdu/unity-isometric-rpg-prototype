using UnityEngine;
using UnityEngine.EventSystems;


public class SlotHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public System.Action onEnter;
    public System.Action onExit;

    public void OnPointerEnter(PointerEventData eventData) => onEnter?.Invoke();
    public void OnPointerExit(PointerEventData eventData) => onExit?.Invoke();
}