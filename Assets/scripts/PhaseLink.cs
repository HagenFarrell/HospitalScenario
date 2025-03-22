using UnityEngine;
using System.Collections.Generic;

namespace PhaseLink
{
    public class PhaseNode
    {
        public GamePhase Phase { get; private set; }
        public PhaseNode Previous { get; set; }
        public PhaseNode Next { get; set; }

        public PhaseNode(GamePhase phase)
        {
            Phase = phase;
            Previous = null;
            Next = null;
        }
    }

    public class PhaseLinkedList
    {
        public PhaseNode Head { get; private set; }
        public PhaseNode Tail { get; private set; }
        public PhaseNode Current { get; private set; }

        public PhaseLinkedList()
        {
            Head = null;
            Tail = null;
            Current = null;
        }

        public void AddPhase(GamePhase phase)
        {
            PhaseNode newNode = new PhaseNode(phase);

            if (Head == null)
            {
                Head = newNode;
                Tail = newNode;
            }
            else
            {
                Tail.Next = newNode;
                newNode.Previous = Tail;
                Tail = newNode;
            }
        }

        public void SetCurrentToHead()
        {
            Current = Head;
        }

        public bool MoveNext()
        {
            if (Current?.Next != null)
            {
                Current = Current.Next;
                return true;
            }
            return false;
        }

        public bool MovePrevious()
        {
            if (Current?.Previous != null)
            {
                Current = Current.Previous;
                return true;
            }
            return false;
        }
    }
}