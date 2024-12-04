using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.XR.ARSubsystems;
using System;
using System.Reflection;

public struct LogEntry
{
    // timestamp
    public DateTime timestamp;
    public float timeToDetect;
    public bool pointedAtSomething;
    public SceneManager.GoalTypes changeType;
    public string objectName;
    public Vector3 objectPosition;
    public Vector3 userPosition;
    public string associatedPlaneClassification;
    public Vector3 userDirection;
    public string pointedObjectName;
    public Vector3 pointedObjectPosition;

    public static string GetCSVHeader()
    {
        var fieldNames = new List<string>();
        foreach (FieldInfo field in typeof(LogEntry).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            fieldNames.Add(field.Name);
        }
        return string.Join(",", fieldNames);
    }

    public readonly string ToCsvString()
    {
        return $"{timestamp:dd_MM_yyyy_HH_mm_ss},{timeToDetect},{pointedAtSomething},{changeType},{objectName},{Vector3ToString(objectPosition)},{Vector3ToString(userPosition)},{associatedPlaneClassification},{Vector3ToString(userDirection)},{pointedObjectName},{Vector3ToString(pointedObjectPosition)}";
    }

    private readonly string Vector3ToString(Vector3 vector)
    {
        return $"{vector.x};{vector.y};{vector.z}";
    }

}

public class LogManager : MonoBehaviour
{
    private string fileName;
    private string filePath;

    private List<LogEntry> logEntries = new();

    public void SaveToFile()
    {
        string csvContent = ConvertToCSV(logEntries);
        SaveCSV(filePath, csvContent);
    }

    public void Init()
    {
        DateTime timestamp = DateTime.Now;
        fileName = $"{timestamp:ddMMyy_HHmmss}.csv";

        string directoryPath = Path.Combine(Application.persistentDataPath, "xr_change_blindness_trials");

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        filePath = Path.Combine(directoryPath, fileName);
    }

    public void Log(LogEntry logEntry)
    {
        logEntries.Add(logEntry);
    }

    private string ConvertToCSV(List<LogEntry> data)
    {
        string csv = LogEntry.GetCSVHeader() + "\n";
        foreach (LogEntry row in data)
        {
            csv += row.ToCsvString() + "\n";
        }
        return csv;
    }

    private void SaveCSV(string filePath, string content)
    {
        Debug.Log("Saving log" + fileName + " to " + filePath);
        File.WriteAllText(filePath, content);
    }
}
