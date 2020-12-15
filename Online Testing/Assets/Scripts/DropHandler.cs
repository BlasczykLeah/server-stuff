﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropHandler : MonoBehaviour, IDropHandler, IComparer<CardButton>
{
    public List<CardButton> cards; 
    
    public Out outState;

    public float reorderTime = .1f;
    
    public OutHandler outHandler;

    public List<CardButton> wildCards;

    public GameObject DropGuideLeft, DropGuideRight;

    public bool canDrop = true;

    public CardButton lastCard;

    private GameManager gameManager;

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("dropped", gameObject);
    }

    public bool checkValidDrop(CardButton newCard)
    {
        if (!canDrop)
        {
            print("This DropZone is droppable");
            return false;
        }

        print("new card...");
        // first card
        if (cards.Count == 0)
        {
            if (newCard.myCard.suit == Suit.Joker || newCard.myCard.number == GameManager.instance.round)
            {
                // return false -- no wild as first card
                // may have ui display something
                print("Wilds can not be the first card played on a drop zone.");
                return false;
            }
            
            cards.Add(newCard);
            outHandler.RemoveFromHand(newCard);
            activateNewDropHandler();
            print("Added a new card ${newCard.myCard.suit} - ${newCard.myCard.number} to drop zone");
            return true;
        }
        
        // second card
        int incomingNum = newCard.myCard.number;
        Suit incomingSuit = newCard.myCard.suit;
        List<int> cardNums = cards.Select(acard => acard.myCard.number).ToList();
        // check wilds first~!
        
        //  compare second card to first card...
        if (cards.Count == 1)
        {
            if (incomingNum == cards[0].myCard.number)
            {
                print("Setting Drop Zone to Out to 'Set'");
                outState = Out.Set;
            }
            else if (incomingSuit == cards[0].myCard.suit)
            {
                print("Setting Drop Zone to Out to 'Run'");
                outState = Out.Run;
            } 
            // ~ wild
            else if (newCard.myCard.suit == Suit.Joker | newCard.myCard.number == GameManager.instance.round)
            {
                print("Dropped a Wild Card, enabling Contextual Menu");
                if(!wildCards.Contains(newCard))
                    wildCards.Add(newCard);   
                newCard.myCard.usedAsWild = true;
                outHandler.RemoveFromHand(newCard);
                ContextEnableOutOptions();
                return true;
            }
            else
            {
                // if the second card is not set-making nor contiguous, reject 
                print($"Second card {newCard.myCard.suit}: {newCard.myCard.number} is neither set-making nor contiguous");
                return false;
            }
        }

        // 2 or more cards...
        if (outState == Out.Set)
        {
            // ~ wild
            if (newCard.myCard.suit == Suit.Joker | newCard.myCard.number == GameManager.instance.round)
            {
                print($"Dropped a Wild Card to set of {cards[0].myCard.suit}");
                if(!wildCards.Contains(newCard))
                    wildCards.Add(newCard);   
                cards.Add(newCard);
                newCard.myCard.usedAsWild = true;
                outHandler.RemoveFromHand(newCard);
                return true;
            } else if(incomingNum == cards[0].myCard.number)
            {
                cards.Add(newCard);
                outHandler.RemoveFromHand(newCard);
                Invoke(nameof(ReorderCardObjects), reorderTime);
                print($"Adding a new card {newCard.myCard.suit}: {newCard.myCard.number} to our set.");
                return true;
            }
            else
            {
                print($"Failed attempt to add a new card {newCard.myCard.suit} to our set.");
                return false;
            }
        }

        if (outState == Out.Run)
        {
            // ~ wild 
            if (newCard.myCard.suit == Suit.Joker || newCard.myCard.number == GameManager.instance.round)
            {
                print("Added a wild card, contextual options should appear.");
                ContextEnableRunOptions();
                newCard.myCard.usedAsWild = true;
                // cards.Add(newCard);
                wildCards.Add(newCard);
                // outHandler.RemoveFromHand(newCard);
                return true;
            }
            
            if (cardNums.Contains(incomingNum)) // this might be necessary if i adjust the contiguous
            {
                print($"{newCard.myCard.suit}: {newCard.myCard.number} - Card already exists in run.");
                return false;
            }

            if (!checkContinguous(incomingNum))
            {
                print($"{newCard.myCard.suit}: {newCard.myCard.number} - Card not contiguous.");
                return false;
            }
            
            cards.Add(newCard);
            outHandler.RemoveFromHand(newCard);
            cards.Sort(Compare);
            Invoke(nameof(ReorderCardObjects), reorderTime);
            return true;
            
        }

        print($"Oops something wrong with the validDropCheck on {gameObject.name}");
        return false;
    }
    
    public void ContextEnableRunOptions()
    {
        GetComponent<LayoutController>().squeezeIn();
        // display context options for run or sets
        DropGuideLeft.SetActive(true);
        DropGuideRight.SetActive(true);
        // set context values
        DropGuideLeft.GetComponent<DropContextController>().setHeader("<");
        DropGuideLeft.GetComponent<Button>().onClick.AddListener(ContextSetRunFirstCard);
        DropGuideRight.GetComponent<DropContextController>().setHeader(">");
        DropGuideRight.GetComponent<Button>().onClick.AddListener(ContextSetRunLastCard);
        canDrop = false;
    }
    
    
    public void ContextEnableOutOptions()
    {
        // squeeze cards to center
        GetComponent<LayoutController>().squeezeIn();
        // display context options for run or sets
        DropGuideLeft.SetActive(true);
        DropGuideRight.SetActive(true);
        // set context values
        DropGuideLeft.GetComponent<DropContextController>().setHeader("Run");
        DropGuideLeft.GetComponent<Button>().onClick.AddListener(ContextSetRun);
        DropGuideRight.GetComponent<DropContextController>().setHeader("Set");
        DropGuideRight.GetComponent<Button>().onClick.AddListener(ContextSetSet);
        canDrop = false;
    }

    public void ContextDisable()
    {
        // remove event listeners
        DropGuideLeft.GetComponent<Button>().onClick.RemoveAllListeners();
        DropGuideRight.GetComponent<Button>().onClick.RemoveAllListeners();
        // disable context options
        DropGuideLeft.SetActive(false);
        DropGuideRight.SetActive(false);
    }

    public void ContextSetRun()
    {
        outState = Out.Run;
        canDrop = true;
        ContextDisable();
        // may add a flash?
        ContextEnableRunOptions();
    }
    
    public void ContextSetSet()
    {
        outState = Out.Set;
        canDrop = true;
        cards.Add(wildCards.Last());
        ContextDisable();
    }

    public void ContextSetRunFirstCard()
    {
        // set position
        cards.Insert(0, wildCards.Last());
        // set wild value
        cards[0].myCard.wildNumber = cards[1].myCard.number - 1;
        print($"Setting wild card to number to {cards[0].myCard.wildNumber}");
        ContextDisable();
        Invoke(nameof(ReorderCardObjects), reorderTime);
        canDrop = true;
        // check for out
        outHandler.RemoveFromHand(cards[0]);
    }
    
    public void ContextSetRunLastCard()
    {
        // set position
        cards.Add(wildCards.Last());
        // set wild value
        cards.Last().myCard.wildNumber = cards[cards.Count-2].myCard.number + 1;
        print($"Setting wild card to number to {cards.Last().myCard.wildNumber}");
        ContextDisable();
        Invoke(nameof(ReorderCardObjects), reorderTime);
        canDrop = true;
        // check for out
        outHandler.RemoveFromHand(cards.Last());
    }


    public bool removeCard(CardButton card)
    {
        print("removing card");
        if (cards.Contains(card))
        {
            // ~ wild
            if (card.myCard.usedAsWild)
            {
                wildCards.Remove(card);
                card.myCard.usedAsWild = false;
            }
            
            if (outState == Out.Set)
            {
                // if set
                cards.Remove(card);
                outHandler.ReturnToHand(card);
                print($"Removed {card} from CheckPile on {gameObject.name}");
                cards.Sort(Compare);
            }

            if (outState == Out.Run)
            {
                // if run, remove that card and the shorter side of the existing run
                var cardIndex = cards.IndexOf(card);
                print($"Removing from a run at index {cardIndex}. Total number of cards is {cards.Count}.");
                // determine shorter side
                if ((cardIndex+1) * 2 <= cards.Count)
                {
                    print("Removing from left side");
                    for (int i = 0; i < cardIndex; i++)
                    {
                        // removing from the left side end everytime
                        outHandler.ReturnToHand(cards[0]);
                        cards[0].ReturnToHand();
                        cards.RemoveAt(0);
                    }
                }
                else
                {
                    print("Removing from right side");
                    for (int i = 0; i < (cards.Count-cardIndex); i++)
                    {
                        outHandler.ReturnToHand(cards[cards.Count-1]);
                        cards.Last().ReturnToHand();
                        cards.RemoveAt(cards.Count-1);
                    }
                }
                if(cards.Contains(card))
                    cards.Remove(card);
            }

            if (cards.Count == 1)
            {
                outState = Out.None;
                cards.Remove(card);
                outHandler.ReturnToHand(card);
            }

            if (cards.Count == 0)
            {
                outHandler.RemoveEmptyDrop();
            }

            return true;
        }
        else
        {
            print($"Trying to remove card that doesn't exist in DropHandler on {gameObject.name}");
            return false;
        }
    }

    public bool checkEmpty()
    {
        return cards.Count == 0;
    }
    
    public bool checkValid()
    {
        return cards.Count > 2 || cards.Count == 0;
    }
    
    void activateNewDropHandler()
    {
        outHandler.OpenNewDrop();
    }

    bool checkContinguous(int cardNum)
    {
        // print("checking contiguousness:");
        int firstcardVal = (cards[0].myCard.usedAsWild) ? cards[0].myCard.wildNumber : cards[0].myCard.number;
        int lastcardVal = (cards.Last().myCard.usedAsWild) ? cards.Last().myCard.wildNumber : cards.Last().myCard.number;

        return cardNum - firstcardVal == -1 |
               cardNum - lastcardVal == 1;
    }
    
    public int Compare(CardButton x, CardButton y)
    {
        print("Sorting Cards...");
        int xVal = (x.myCard.usedAsWild) ? x.myCard.wildNumber : x.myCard.number;
        int yVal = (y.myCard.usedAsWild) ? y.myCard.wildNumber : y.myCard.number;
        if (xVal < yVal) return -1;
        else if (xVal > yVal) return 1;
        else return 0;
    }

    void ReorderCardObjects()
    {
        foreach (var t in cards)
        {
            t.transform.SetAsLastSibling();
        }
    }

    public void clearDropZone()
    {
        cards.Clear();
        wildCards.Clear();
        outState = Out.None;
        canDrop = true;
    }
}
