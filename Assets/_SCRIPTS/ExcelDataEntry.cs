using System.Collections.Generic;

[System.Serializable]
public class ExcelDataEntry
{
    public string tag;
    public int year;
    public string name;
    public string description1;
    public string description2;
    public List<string> imageFileNames = new List<string>();
}
