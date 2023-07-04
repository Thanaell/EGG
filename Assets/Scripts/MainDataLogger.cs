using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static OVRPlugin;

public class MainDataLogger : MonoBehaviour
{
    private StreamWriter writer;

    private void OnApplicationQuit()
    {
        writer.Close();
    }

    public void CreateStreamWriter(string path)
    {
        writer = new StreamWriter(path);
    }

    public void WriteDataToCSV(
        int participantNumber,
        int modalityNumber,
        SHOWING_TECHNIQUE showTechnique,
        bool isTraining,
        int showGestureRepeats,
        string gestureName,
        float timeBeforeTriggerSecondPhase,
        float timeBeforeTriggerThirdPhase,
        int numberOfSuccessInFirstPhase,
        int numberOfSuccessInSecondPhase,
        int numberOfSuccessInThirdPhase
    )
    {
        string line = "";

        line += string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};",
                   participantNumber, modalityNumber, showTechnique, gestureName, isTraining,
                   showGestureRepeats, timeBeforeTriggerSecondPhase, timeBeforeTriggerThirdPhase,
                   numberOfSuccessInFirstPhase, numberOfSuccessInSecondPhase, numberOfSuccessInThirdPhase);

        writer.WriteLine(line);
        writer.Flush();
    }
}
