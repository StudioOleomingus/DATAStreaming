using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ExcelDataLoader : MonoBehaviour
{
    public static ExcelDataLoader Instance { get; private set; }

    [Tooltip("Name of the CSV file inside StreamingAssets")]
    [SerializeField] private string csvFileName = "DATA.csv";

    [Tooltip("Subfolder inside StreamingAssets where images are stored (leave empty if images are in root)")]
    [SerializeField] private string imageSubFolder = "";

    private Dictionary<string, List<ExcelDataEntry>> dataByTag = new Dictionary<string, List<ExcelDataEntry>>();
    private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    public bool IsReady { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadCSV();
        StartCoroutine(PreloadAllImages());
    }

    //  CSV
    private void LoadCSV()
    {
        string path = Path.Combine(Application.streamingAssetsPath, csvFileName);

        if (!File.Exists(path))
        {
            Debug.LogError($"[ExcelDataLoader] CSV not found: {path}");
            return;
        }

        string[] lines = File.ReadAllLines(path);

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] fields = ParseCSVLine(line);
            if (fields.Length < 5) continue;

            string tag = fields[0].Trim();
            if (string.IsNullOrEmpty(tag)) continue;

            int year = 0;
            int.TryParse(fields[1].Trim(), out year);

            ExcelDataEntry entry = new ExcelDataEntry
            {
                tag = tag,
                year = year,
                name = fields[2].Trim(),
                description1 = fields[3].Trim(),
                description2 = fields[4].Trim()
            };

            //  IMAGES column (6th column, index 5)
            if (fields.Length >= 6 && !string.IsNullOrEmpty(fields[5]))
            {
                string[] imageNames = fields[5].Split(',');
                foreach (string img in imageNames)
                {
                    string trimmed = img.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        entry.imageFileNames.Add(trimmed);
                }
            }

            if (!dataByTag.ContainsKey(tag))
                dataByTag[tag] = new List<ExcelDataEntry>();

            dataByTag[tag].Add(entry);
        }

        foreach (var kvp in dataByTag)
            kvp.Value.Sort((a, b) => a.year.CompareTo(b.year));

        Debug.Log($"[ExcelDataLoader] Loaded {dataByTag.Values.Sum(l => l.Count)} entries across {dataByTag.Count} tags.");
    }

    //Img.
    private IEnumerator PreloadAllImages()
    {
        HashSet<string> uniqueImages = new HashSet<string>();
        foreach (var list in dataByTag.Values)
            foreach (var entry in list)
                foreach (string img in entry.imageFileNames)
                    uniqueImages.Add(img);

        foreach (string fileName in uniqueImages)
        {
            if (spriteCache.ContainsKey(fileName)) continue;

            yield return StartCoroutine(LoadImageCoroutine(fileName));
        }

        IsReady = true;
        Debug.Log($"[ExcelDataLoader] All images loaded. Cache size: {spriteCache.Count}");
    }

    private IEnumerator LoadImageCoroutine(string fileName)
    {
        string folder = string.IsNullOrEmpty(imageSubFolder)
            ? Application.streamingAssetsPath
            : Path.Combine(Application.streamingAssetsPath, imageSubFolder);

        string filePath = Path.Combine(folder, fileName);

        // Use file:// protocol for local file loading
        string url = "file:///" + filePath.Replace("\\", "/");

        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Texture2D tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                tex.name = fileName;

                Sprite sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                sprite.name = fileName;

                spriteCache[fileName] = sprite;
            }
            else
            {
                Debug.LogWarning($"[ExcelDataLoader] Failed to load image '{fileName}': {www.error}");
            }
        }
    }

    //  Public API
    public List<ExcelDataEntry> GetEntriesForTag(string tag)
    {
        if (dataByTag.TryGetValue(tag, out List<ExcelDataEntry> entries))
            return entries;
        return null;
    }

    public Sprite GetSprite(string fileName)
    {
        if (spriteCache.TryGetValue(fileName, out Sprite sprite))
            return sprite;
        return null;
    }

    public List<string> GetAllTags()
    {
        return new List<string>(dataByTag.Keys);
    }

    // CSV Parser
    private string[] ParseCSVLine(string line)
    {
        List<string> fields = new List<string>();
        bool inQuotes = false;
        string current = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }
        fields.Add(current);
        return fields.ToArray();
    }
}
