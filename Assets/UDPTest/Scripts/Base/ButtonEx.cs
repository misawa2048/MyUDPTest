using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;

namespace TmUIEx
{
    public class ButtonEx : Button
    {
        public UnityEvent buttonExDownEvent;
        public UnityEvent buttonExUpEvent;
        //bool isInButton;

        public override void OnPointerDown(PointerEventData eventData)
        {
            //isInButton = true;
            buttonExDownEvent.Invoke();
        }
        public override void OnPointerExit(PointerEventData eventData)
        {
            //isInButton = false;
        }
        public override void OnPointerUp(PointerEventData eventData)
        {
            //if (!isInButton)
            {
                buttonExUpEvent.Invoke();
            }
        }
    }
}