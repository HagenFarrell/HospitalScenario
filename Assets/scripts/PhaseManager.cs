using PhaseLink;
using UnityEngine;

public class PhaseManager : MonoBehaviour
{
    private PhaseLinkedList phaseList;

    private void Start()
    {
        phaseList = new PhaseLinkedList();

        // Define the phases in order
        phaseList.AddPhase(GamePhase.Phase1);
        phaseList.AddPhase(GamePhase.Phase2);
        phaseList.AddPhase(GamePhase.Phase3);
        phaseList.AddPhase(GamePhase.Phase4);
        phaseList.AddPhase(GamePhase.Phase5);
        phaseList.AddPhase(GamePhase.Phase6);
        phaseList.AddPhase(GamePhase.Phase7);

        // Set the current phase to the head of the list
        phaseList.SetCurrentToHead();
        StartPhase();
    }

    private void StartPhase()
    {
        Debug.Log($"Starting Phase: {phaseList.Current.Phase}");
        // Add logic for what happens at the start of a phase
    }

    public void NextPhase()
    {
        if (phaseList.MoveNext())
        {
            StartPhase();
        }
        else
        {
            Debug.Log("Already at the last phase!");
        }
    }

    public void PreviousPhase()
    {
        if (phaseList.MovePrevious())
        {
            StartPhase();
        }
        else
        {
            Debug.Log("Already at the first phase!");
        }
    }
}
