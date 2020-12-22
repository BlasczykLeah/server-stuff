﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OutDropHandler : DropHandler, IDropHandler, IComparer<CardButton>
{
    [Header("Refs")]

    public OutDeckHandler outDeckHandler;

    private GameManager gameManager;

    public override bool  checkValidDrop(CardButton newCard)
    {
        if (!GameManager.instance.myTurn)
        {
            print("You can not drop because it's not your turn.");
            return false;
        }
        
        if (GameManager.instance.myDraw)
        {
            print("You can not drop because it is your turn but you must draw first");
            return false;
        }
        
        if (!canDrop)
        {
            print("This DropZone is droppable");
            return false;
        }

        print("new card...");
        
        int incomingNum = newCard.myCard.number;
        Suit incomingSuit = newCard.myCard.suit;
        List<int> cardNums = cards.Select(acard => acard.myCard.number).ToList();
        // check wilds first~!
        

        // 2 or more cards...
        if (outState == Out.Set)
        {
            // ~ wild
            if (newCard.myCard.suit == Suit.Joker | newCard.myCard.number == GameManager.instance.round)
            {
                print($"Dropped a Wild Card to set of {outSuit}");
                if(!wildCards.Contains(newCard))
                    wildCards.Add(newCard);   
                cards.Add(newCard);
                newCard.myCard.usedAsWild = true;
                outDeckHandler.RemoveFromHand(newCard);
                
                // check for first normal card
                CardButton refCard = cards.FirstOrDefault(t => !t.myCard.usedAsWild);

                if (refCard == null)
                {
                    Debug.Log($"Tried to update incoming wild card's number to a ref card's number, but coud not find a value reference card...");
                    return true;
                }
                newCard.myCard.number = refCard.myCard.number;
                newCard.myCard.wildNumber = refCard.myCard.number;
                Debug.Log($"Updating incoming wild card's number to {newCard.myCard.number}");

                //
                return true;
            } else if(incomingNum == cards[0].myCard.number)
            {
                cards.Add(newCard);
                outDeckHandler.RemoveFromHand(newCard);
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
            outDeckHandler.RemoveFromHand(newCard);
            cards.Sort(Compare);
            print($"Added new card: {newCard.myCard.suit}: {newCard.myCard.number} - to run.");
            Invoke(nameof(ReorderCardObjects), reorderTime);
            return true;
        }

        print($"Oops something wrong with the validDropCheck on {gameObject.name}");
        return false;
    }
    
    /// <summary>
    /// Set OutState of the OutDrop Handler
    /// </summary>
    /// <param name="state"></param>
    public void setOutState(Out state)
    {
        outState = state;
        print($"Setting outstate of OutDropHandler: + {state}");
    }
    
    /// <summary>
    ///  Add individual cards to the cards list from the OutDeck
    /// </summary>
    /// <param name="newCard">CardButton type</param>
    public void addOutDeckCard(CardButton newCard)
    {
        print("Receiving the Out Deck infos...");
        cards.Add(newCard);
        if (newCard.myCard.suit != Suit.Joker && newCard.myCard.number != GameManager.instance.round) return;
        switch (outState)
        {
            case Out.Run:
                wildCards.Add(newCard);
                newCard.myCard.usedAsWild = true;
                newCard.myCard.wildNumber = -1;
                return;
            case Out.Set:
                wildCards.Add(newCard);
                newCard.myCard.usedAsWild = true;
                return;
            case Out.None:
            default:
                Debug.LogWarning($"ERROR:: outState of OutDropHandler on {gameObject.name} not set");
                break;
        }
    }

    public void completeOutDeck()
    {
        if (cards.Select(card => !card.myCard.usedAsWild).ToList().Count < 1)
        {
            Debug.Log("No Wilds in Cards List?: " + cards);
            return;
        }

        // check for first normal card
        CardButton refCard = cards.FirstOrDefault(t => !t.myCard.usedAsWild);
        
        if (refCard == null)
        {
            Debug.Log("NO non-wild cards in Cards List");
            return;
        }

        if (outState == Out.Run)
        {
            // sweep right
            for (int i = cards.IndexOf(refCard); i  < cards.Count; i++)
            {
                if (cards[i].myCard.usedAsWild)
                {
                    cards[i].myCard.wildNumber = refCard.myCard.number + (i - cards.IndexOf(refCard)) ;
                }
            }
            
            // sweep left 
            for (int i = cards.IndexOf(refCard); i  > 0; i--)
            {
                if (cards[i].myCard.usedAsWild)
                {
                    cards[i].myCard.wildNumber = refCard.myCard.number + (cards.IndexOf(refCard) - i) ;
                }
            }
        }

        if (outState == Out.Set)
        {
            foreach (var card in cards.Where(c => c.myCard.usedAsWild))
            {
                card.myCard.wildNumber = refCard.myCard.number;
                card.myCard.number = refCard.myCard.number;
            }
        }

        else
        {
            Debug.LogWarning($"ERROR:: outState of OutDropHandler on {gameObject.name} not set");
        }
    }
    
    public override void ContextEnableRunOptions()
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
    
    // public void ContextEnableOutOptions()
    // {
    //     // squeeze cards to center
    //     GetComponent<LayoutController>().squeezeIn();
    //     // display context options for run or sets
    //     DropGuideLeft.SetActive(true);
    //     DropGuideRight.SetActive(true);
    //     // set context values
    //     DropGuideLeft.GetComponent<DropContextController>().setHeader("Run");
    //     DropGuideLeft.GetComponent<Button>().onClick.AddListener(ContextSetRun);
    //     DropGuideRight.GetComponent<DropContextController>().setHeader("Set");
    //     DropGuideRight.GetComponent<Button>().onClick.AddListener(ContextSetSet);
    //     canDrop = false;
    // }

    // public void ContextDisable()
    // {
    //     // remove event listeners
    //     DropGuideLeft.GetComponent<Button>().onClick.RemoveAllListeners();
    //     DropGuideRight.GetComponent<Button>().onClick.RemoveAllListeners();
    //     // disable context options
    //     DropGuideLeft.SetActive(false);
    //     DropGuideRight.SetActive(false);
    // }

    // public void ContextSetRun()
    // {
    //     outState = Out.Run;
    //     canDrop = true;
    //     ContextDisable();
    //     // may add a flash?
    //     ContextEnableRunOptions();
    // }
    //
    // public void ContextSetSet()
    // {
    //     outState = Out.Set;
    //     canDrop = true;
    //     cards.Add(wildCards.Last());
    //     outHandler.RemoveFromHand(wildCards.Last());
    //     ContextDisable();
    // }

    public override void ContextSetRunFirstCard()
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
        outDeckHandler.RemoveFromHand(cards[0]);
    }
    //
    public override void ContextSetRunLastCard()
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
        outDeckHandler.RemoveFromHand(cards.Last());
    }


    // public bool removeCard(CardButton card)
    // {
    //     print("removing card");
    //     if (cards.Contains(card))
    //     {
    //         // ~ wild
    //         if (card.myCard.usedAsWild)
    //         {
    //             wildCards.Remove(card);
    //             card.myCard.usedAsWild = false;
    //         }
    //         
    //         if (outState == Out.Set)
    //         {
    //             // if set
    //             cards.Remove(card);
    //             outHandler.ReturnToHand(card);
    //             print($"Removed {card} from CheckPile on {gameObject.name}");
    //             cards.Sort(Compare);
    //             return true;
    //         }
    //
    //         else if (outState == Out.Run)
    //         {
    //             // if run, remove that card and the shorter side of the existing run
    //             var cardIndex = cards.IndexOf(card);
    //             print($"Removing from a run at index {cardIndex}. Total number of cards is {cards.Count}.");
    //             // determine shorter side
    //             if ((cardIndex+1) * 2 <= cards.Count)
    //             {
    //                 print("Removing from left side");
    //                 for (int i = 0; i < cardIndex; i++)
    //                 {
    //                     // removing from the left side end everytime
    //                     outHandler.ReturnToHand(cards[0]);
    //                     cards[0].ReturnToHand();
    //                     cards.RemoveAt(0);
    //                 }
    //             }
    //             else
    //             {
    //                 print("Removing from right side");
    //                 for (int i = 0; i < (cards.Count-cardIndex); i++)
    //                 {
    //                     outHandler.ReturnToHand(cards[cards.Count-1]);
    //                     cards.Last().ReturnToHand();
    //                     cards.RemoveAt(cards.Count-1);
    //                 }
    //             }
    //             if(cards.Contains(card))
    //                 cards.Remove(card);
    //         }
    //
    //         else if (cards.Count == 1)
    //         {
    //             outState = Out.None;
    //             cards.Remove(card);
    //             outHandler.ReturnToHand(card);
    //         }
    //
    //         if (cards.Count == 0)
    //         {
    //             outHandler.RemoveEmptyDrop();
    //         }
    //
    //         return true;
    //     }
    //     else
    //     {
    //         print($"Trying to remove card that doesn't exist in DropHandler on {gameObject.name}");
    //         return false;
    //     }
    // }

    public bool checkEmpty()
    {
        return cards.Count == 0;
    }
    
    public override bool checkValid()
    {
        // return cards.Count > 0;
        return cards.Count > 2 || cards.Count == 0;
    }
    
    // void activateNewDropHandler()
    // {
    //     outHandler.OpenNewDrop();
    // }

    bool checkContinguous(int cardNum)
    {
        // print("checking contiguousness:");
        int firstcardVal = (cards[0].myCard.usedAsWild) ? cards[0].myCard.wildNumber : cards[0].myCard.number;
        int lastcardVal = (cards.Last().myCard.usedAsWild) ? cards.Last().myCard.wildNumber : cards.Last().myCard.number;

        return cardNum - firstcardVal == -1 |
               cardNum - lastcardVal == 1;
    }
    
    // public int Compare(CardButton x, CardButton y)
    // {
    //     print("Sorting Cards...");
    //     int xVal = (x.myCard.usedAsWild) ? x.myCard.wildNumber : x.myCard.number;
    //     int yVal = (y.myCard.usedAsWild) ? y.myCard.wildNumber : y.myCard.number;
    //     if (xVal < yVal) return -1;
    //     else if (xVal > yVal) return 1;
    //     else return 0;
    // }

    void ReorderCardObjects()
    {
        foreach (var t in cards)
        {
            t.transform.SetAsLastSibling();
        }
    }

    public override void clearDropZone()
    {
        foreach (var card in cards)
        {
            card.gameObject.transform.parent = null;
            card.ReturnToHand();
        }
        cards.Clear();
        wildCards.Clear();
        outState = Out.None;
        canDrop = true;
    }

    public void OutValues()
    {
        // Values to go to OutHandler
        // outsuit
        // run - ends
    }
}
