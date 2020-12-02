﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardButton : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Card myCard;

    Transform parentObject;
    Transform handObject;

    bool waitForClick = false;

    private void Update()
    {
        if(waitForClick && Input.GetMouseButtonDown(0))
        {
            ShrinkCard();
        }
    }

    public void MyCard(string cardName)
    {
        myCard = CardParser.parseCard(cardName);
        handObject = parentObject = transform.parent;

        //set texts
        char cardText = valueToChar(myCard.number);
        if (cardText == '0') transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = "10";
        else transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = cardText.ToString();
        transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = myCard.suit.ToString();

        if(myCard.suit == Suit.Diamond || myCard.suit == Suit.Heart)
        {
            // make texts red
            transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().color = Color.red;
            transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().color = Color.red;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // zoom in on card
        if (!waitForClick)
        {
            transform.localScale *= 2;
            Debug.Log("card expanded");

            waitForClick = true;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        GetComponent<Image>().raycastTarget = false;
        transform.SetParent(parentObject.parent);

        // if in drop handler, remove it from its list
    }

    public void OnDrag(PointerEventData eventData)
    {
        // move card image with mouse/finger
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        GameObject dropObject = eventData.pointerCurrentRaycast.gameObject;

        if (dropObject?.GetComponent<DropHandler>())
        {
            if (dropObject.GetComponent<DropHandler>().checkValidDrop(this))
            {
                parentObject = dropObject.transform;
                transform.SetParent(parentObject);
            }
            else
            {
                ReturnToHand();
            }
        }
        else
        {
            ReturnToHand();
        }

        GetComponent<Image>().raycastTarget = true;
    }

    void ReturnToHand()
    {
        parentObject = handObject;
        transform.SetParent(parentObject);
    }

    void ShrinkCard()
    {
        transform.localScale /= 2;
        Debug.Log("card shrunk");
        waitForClick = false;
    }

    char valueToChar(int num)
    {
        switch (num)
        {
            case 0: return 'J';
            case 1: return 'A';
            case 2: return '2';
            case 3: return '3';
            case 4: return '4';
            case 5: return '5';
            case 6: return '6';
            case 7: return '7';
            case 8: return '8';
            case 9: return '9';
            case 10: return '0';
            case 11: return 'J';
            case 12: return 'Q';
            case 13: return 'K';
            default: return '0';
        }
    }
}
