using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SimulationInformationDisplay : MonoBehaviour
{
    [SerializeField] TextMeshPro systemField;
    [SerializeField] TextMeshPro interactionField;
    [SerializeField] TextMeshPro referencelineField;
    [SerializeField] TextMeshPro pathfollowerField;
    [SerializeField] TextMeshPro instructionsField;

    private Dictionary<string, string> data = new Dictionary<string, string>();

    private string headerStyleIn = "<size=120%><u><b>";
    private string headerStyleOut = "</b></u></size>";

    void LateUpdate()
    {
        if (Time.frameCount % 2 == 0)
        {
            RefreshDisplay();
        }
    }


    // Update all tmp texts with the current data
    public void RefreshDisplay()
    {
        UpdateSystem();
        UpdateInteraction();
        UpdateReferenceline();
        UpdateFollower();
        UpdateInstructions();
    }

    public void UpdateData(string key, string value)
    {
        data[data.ContainsKey(key) ? key : key] = value;
    }


    void UpdateSystem()
    {
        string simulationTime = data.TryGetValue("simulationTime", out var simTime) ? simTime : "N/A";
        string accumulatedWork = data.TryGetValue("accumulatedWork", out var accWork) ? accWork : "N/A";

        systemField.text = headerStyleIn + "SYSTEM" + headerStyleOut
            + "\n- " + "simulation time: " + simulationTime
            + "\n- " + "accumulated work: " + accumulatedWork
            ;
    }

    void UpdateInteraction()
    {
        string rightPosition = data.TryGetValue("rightPosition", out var rightPos) ? rightPos : "N/A";
        string rightAtom = data.TryGetValue("rightAtom", out var rightAtomValue) ? rightAtomValue : "N/A";
        string leftPosition = data.TryGetValue("leftPosition", out var leftPos) ? leftPos : "N/A";
        string leftAtom = data.TryGetValue("leftAtom", out var leftAtomValue) ? leftAtomValue : "N/A";

        interactionField.text = headerStyleIn + "INTERACTION"+ headerStyleOut
            + "\n- " + "right position at " + rightPosition
            + "on atom atom #" + rightAtom
            + "\n- " + "left position at " + leftPosition
            + " on atom #" + leftAtom
            ;
    }


    void UpdateReferenceline()
    {
        string length = data.TryGetValue("refLength", out var len) ? len : "N/A";
        string points = data.TryGetValue("refPoints", out var pts) ? pts : "N/A";
        string origin = data.TryGetValue("refOrigin", out var org) ? org : "N/A";
        string end = data.TryGetValue("refEnd", out var ed) ? ed : "N/A";
        string triplet = data.TryGetValue("refTriplet", out var trp) ? trp : "N/A";
        string jagger = data.TryGetValue("refJagger", out var jg) ? jg : "N/A";

        referencelineField.text = headerStyleIn + "REFERENCE LINE" + headerStyleOut
            + "\n- " + "length: " + length
            + "\n- " + "points: " + points
            + "\n- " + "from " + origin + " to " + end
            + "\n- " + "angular triplets: " + triplet
            + "\n- " + "path jagger index: " + jagger
            ;
    }


    void UpdateFollower(Dictionary<string, string>? keyValues = null)
    {
        string advance = data.TryGetValue("advance", out var adv) ? adv : "N/A";
        string speed = data.TryGetValue("speed", out var spd) ? spd : "N/A";
        string forceScale = data.TryGetValue("forceScale", out var fs) ? fs : "N/A";
        string colinearity = data.TryGetValue("colinearity", out var col) ? col : "N/A";

        pathfollowerField.text = headerStyleIn + "DaMD FOLLOWER" + headerStyleOut
            + "\n- " + "advance: " + advance
            + "\n- " + "speed: " + speed
            + "\n- " + "force scale: " + forceScale
            + "\n- " + "colinearity: " + colinearity
            ;
    }


    public void UpdateInstructions(int mode = 1)
    {
        if (mode == 1)
        {
            instructionsField.text = "[A] Draw a reference line"
                + "\n[B] Delete last reference line"
                + "\n[Trigger] Select the follower molecule"
                + "\n\n[Y] Delete last interaction trail line"
                + "\n[X] Move reference cube"
                ;
        }
        // make it dinamically change based on the state of the process
        // mode = 0 : none
        // mode = 1 : all instructions
        // mode = 2 : after drawing a reference line
        // mode = 3 : during path following
    }
}
