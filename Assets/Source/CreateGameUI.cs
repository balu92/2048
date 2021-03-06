using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

public class CreateGameUI : MonoBehaviour
{
    long BestScore;
    long currentScore;
    public long CurrentScore
    {
        get => currentScore;
        set
        {
            currentScore = value;
            if (currentScore > BestScore)
            {
                BestScore = currentScore;
                Save();
            }
            Score.text = $"Best: {FormatNumber(BestScore)}\r\nCurrent: {FormatNumber(currentScore)}";
        }
    }

    int ScoreTextSize = 32;

    public UnityEngine.UI.Text Score;

    public RectTransform PlayAreaRectTransform;

    public OrientationManager OrientationManager;

    ScreenOrientation Orientation = ScreenOrientation.Portrait;

    public NumberTile[,] Tiles = new NumberTile[4, 4];

    public RectTransform[,] TileBackgrounds = new RectTransform[4, 4];

    public Canvas Canvas;

    public GameObject NumberTilePrefab;

    public GameObject TileBackgroundPrefab;

    public GameObject PlayAreaBackground;

    public int tolerance = 200;

    private Vector2 fingerStart;
    private Vector2 fingerEnd;

    int StartingTile = 2;

    bool CreateNewTile = false;

    bool InputHandled = true;

    int ScreenWidth = 0;
    int ScreenHeight = 0;

    bool Initialized = false;

    string GetSaveFileLocation() => Path.Combine(Application.persistentDataPath, "2048.save");
    object SaveLoadFileLock = false;

    void Start()
    {
        Application.targetFrameRate = Screen.currentResolution.refreshRate;

        Load();

        List<Vector2Int> FreeTiles = new List<Vector2Int>();

        int bkgsize = (IsPortrait() ? Screen.width : Screen.height);

        PlayAreaRectTransform.sizeDelta = new Vector2(bkgsize, bkgsize);
        PlayAreaRectTransform.anchoredPosition = Vector2.zero;
        PlayAreaRectTransform.anchorMax = Vector2.zero;
        PlayAreaRectTransform.anchorMin = Vector2.zero;
        PlayAreaBackground.transform.SetParent(Canvas.transform, false);

        Vector2 topleft = new Vector2(0, 1);
        Score.rectTransform.anchoredPosition = Vector2.zero;
        Score.rectTransform.anchorMax = topleft;
        Score.rectTransform.anchorMin = topleft;
        Score.rectTransform.position = Vector3.zero;

        ScoreTextSize = Mathf.RoundToInt(Screen.dpi / 4);

        OnResolutionChanged();

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                FreeTiles.Add(new Vector2Int(i, j));
                var tilebkg = Instantiate(TileBackgroundPrefab);

                tilebkg.transform.SetParent(PlayAreaBackground.transform, false);

                int tileSize = GetTileSize();

                var tileRect = tilebkg.GetComponent<RectTransform>();
                tileRect.sizeDelta = new Vector2(tileSize, tileSize);
                tileRect.anchoredPosition = GetTilePosition(i, j);

                TileBackgrounds[i,j] = tileRect;
            }
        }

        for (int i = 0; i < StartingTile; i++)
        {
            var rand = Mathf.RoundToInt(Random.Range(-0.5f, FreeTiles.Count - 0.5001f));
            CreateTile(FreeTiles[rand].x, FreeTiles[rand].y, 2);
            FreeTiles.RemoveAt(rand);
        }

        Initialized = true;
    }

    const byte SaveVersion = 1;

    void Save()
    {
        lock (SaveLoadFileLock)
        {
            using (var file = File.Open(GetSaveFileLocation(), FileMode.Create, FileAccess.Write))
            {
                file.WriteByte(SaveVersion);

                var bestBytes = System.BitConverter.GetBytes(BestScore);
                file.Write(System.BitConverter.IsLittleEndian ? bestBytes.Reverse().ToArray() : bestBytes, 0, 8);

                file.Flush();
                file.Close();
            }
        }
    }

    void Load()
    {
        lock (SaveLoadFileLock)
        {
            if (File.Exists(GetSaveFileLocation()))
            {
                using (var file = File.Open(GetSaveFileLocation(), FileMode.Open, FileAccess.Read))
                {
                    byte[] version = new byte[1];
                    file.Read(version, 0, 1);
                    if (version[0] == 1)
                    {
                        byte[] bestBytes = new byte[8];
                        file.Read(bestBytes, 0, 8);

                        BestScore = System.BitConverter.ToInt64(System.BitConverter.IsLittleEndian ? bestBytes.Reverse().ToArray() : bestBytes, 0);
                        CurrentScore = 0;
                    }
                }
            }
        }
    }

    public static string FormatNumber(int number)
    {
        string result;
        if (number >= 1000000000) { result = (number / 1000000000f).ToString("###.#") + "b"; }
        else if (number >= 1000000) { result = (number / 1000000f).ToString("###.#") + "m"; }
        else if (number >= 1000) { result = (number / 1000f).ToString("###.#") + "k"; }
        else result = number.ToString();

        return result;
    }

    public static string FormatNumber(long number)
    {
        string result;
        if (number >= 1000000000000000L) { result = (number / 1000000000000000f).ToString("###.#") + "q"; }
        else if (number >= 1000000000000L) { result = (number / 1000000000000f).ToString("###.#") + "t"; }
        else if (number >= 1000000000L) { result = (number / 1000000000f).ToString("###.#") + "b"; }
        else if (number >= 1000000L) { result = (number / 1000000f).ToString("###.#") + "m"; }
        else if (number >= 1000L) { result = (number / 1000f).ToString("###.#") + "k"; }
        else result = number.ToString();

        return result;
    }

    public void CreateRandomTile(int value = 2)
    {
        List<Vector2Int> FreeTiles = new List<Vector2Int>();

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (Tiles[i, j] == null) FreeTiles.Add(new Vector2Int(i, j));
            }
        }

        var rand = Mathf.RoundToInt(Random.Range(-0.5f, FreeTiles.Count - 0.5001f));
        CreateTile(FreeTiles[rand].x, FreeTiles[rand].y, value);
    }

    NumberTile CreateTile(int column, int row, int value)
    {
        var tileObject = Instantiate(NumberTilePrefab);

        var tileRectTransform = tileObject.GetComponent<RectTransform>();
        tileRectTransform.anchorMin = Vector2.zero;
        tileRectTransform.anchorMax = Vector2.zero;
        tileRectTransform.anchoredPosition = GetTilePosition(column, row);
        tileObject.transform.SetParent(PlayAreaBackground.transform, false);

        int tileSize = GetTileSize();

        tileObject.GetComponent<RectTransform>().sizeDelta = new Vector2(tileSize, tileSize);
        var txt = tileObject.GetComponentInChildren<UnityEngine.UI.Text>();
        txt.text = FormatNumber(value);

        var tile = tileObject.GetComponent<NumberTile>();
        
        tile.Value = value;
        tile.Column = column;
        tile.Row = row;

        tile.Destination = tileObject.transform.localPosition;

        tile.gameUI = this;

        Tiles[column, row] = tile;

        return tile;
    }

    public int GetTileSize() => Mathf.RoundToInt(PlayAreaRectTransform.sizeDelta.x * 0.2125f);

    public bool IsPortrait() => Screen.width < Screen.height;

    void OnResolutionChanged()
    {
        if (!Initialized) return;

        bool isPortrait = IsPortrait();

        Score.fontSize = ScoreTextSize;
        Score.rectTransform.sizeDelta = new Vector2(ScoreTextSize * 6.5f, ScoreTextSize * 2);
        Score.rectTransform.anchoredPosition = new Vector2(0, -(ScoreTextSize * 2));

        int PlayAreaSize = Mathf.RoundToInt(Mathf.Min(IsPortrait() ? Screen.width : Screen.height, IsPortrait() ? Screen.height - Score.rectTransform.sizeDelta.y : Screen.width - Score.rectTransform.sizeDelta.x));

        int emptySpace = ((isPortrait ? Screen.height : Screen.width) - ((isPortrait ? ScoreTextSize * 2 : Mathf.RoundToInt(ScoreTextSize * 6.5f)) + PlayAreaSize)) / 2;

        if (isPortrait)
        {
            PlayAreaRectTransform.anchorMax = new Vector2(0, 1);
            PlayAreaRectTransform.anchorMin = new Vector2(0, 1);

            PlayAreaRectTransform.anchoredPosition = new Vector2(0, -(emptySpace + PlayAreaSize + ScoreTextSize * 2));
        }
        else
        {
            PlayAreaRectTransform.anchorMax = new Vector2(0, 0);
            PlayAreaRectTransform.anchorMin = new Vector2(0, 0);

            PlayAreaRectTransform.anchoredPosition = new Vector2(emptySpace + ScoreTextSize * 6.5f, 0);
        }

        PlayAreaRectTransform.sizeDelta = new Vector2(PlayAreaSize, PlayAreaSize);

        int TileSize = GetTileSize();
        for (int column = 0; column < 4; column++)
        {
            for (int row = 0; row < 4; row++)
            {
                TileBackgrounds[column, row].anchoredPosition = GetTilePosition(column, row);
                TileBackgrounds[column, row].sizeDelta = new Vector2(TileSize, TileSize);

                if (Tiles[column, row] != null)
                {
                    Tiles[column, row].Destination = GetTilePosition(column, row);
                    Tiles[column, row].rectTransform.sizeDelta = new Vector2(TileSize, TileSize);
                }
            }
        }
    }

    public Vector2 GetTilePosition(int column, int row)
    {
        int TileSize = GetTileSize();
        float PlayAreaSize = PlayAreaRectTransform.sizeDelta.x;

        float Margin = PlayAreaSize * 0.03f;

        float x = Margin + TileSize/2 + (TileSize + Margin) * column;
        float y = Margin + TileSize/2 + (TileSize + Margin) * row;

        return new Vector2(x, y);
    }

    void MoveTilesLeft()
    {
        bool anyMoved = false;

        for (int column = 1; column < 4; column++)
        {
            for (int row = 0; row < 4; row++)
            {
                if (Tiles[column, row] != null)
                    anyMoved = Tiles[column, row].MoveLeft() || anyMoved;
            }
        }

        if (anyMoved)
            CreateNewTile = true;
    }

    void MoveTilesRight()
    {
        bool anyMoved = false;

        for (int column = 2; column >= 0; column--)
        {
            for (int row = 0; row < 4; row++)
            {
                if (Tiles[column, row] != null)
                    anyMoved = Tiles[column, row].MoveRight() || anyMoved;
            }
        }

        if (anyMoved)
            CreateRandomTile();
    }

    void MoveTilesDown()
    {
        bool anyMoved = false;

        for (int column = 0; column < 4; column++)
        {
            for (int row = 1; row < 4; row++)
            {
                if (Tiles[column, row] != null)
                    anyMoved = Tiles[column, row].MoveDown() || anyMoved;
            }
        }

        if (anyMoved)
            CreateRandomTile();
    }

    void MoveTilesUp()
    {
        bool anyMoved = false;

        for (int column = 0; column < 4; column++)
        {
            for (int row = 2; row >= 0; row--)
            {
                if (Tiles[column, row] != null)
                    anyMoved = Tiles[column, row].MoveUp() || anyMoved;
            }
        }

        if (anyMoved)
            CreateRandomTile();
    }

    bool CheckResolutionChange() => ScreenWidth != Screen.width || ScreenHeight != Screen.height;

    void Update()
    {
        if (CheckResolutionChange())
        {
            ScreenWidth = Screen.width;
            ScreenHeight = Screen.height;
            OnResolutionChanged();
        }

        if (Screen.orientation != Orientation)
        {
            Orientation = Screen.orientation;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (Tiles[i, j] != null)
                    {
                        Tiles[i, j].Destination = GetTilePosition(i, j);
                        Tiles[i, j].IsMoving = true;
                    }
                }
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            InputHandled = false;
            fingerStart = Input.mousePosition;
            fingerEnd = Input.mousePosition;
        }
        if (Input.GetMouseButton(0) && !InputHandled)
        {
            fingerEnd = Input.mousePosition;

            if (Mathf.Abs(fingerEnd.x - fingerStart.x) > tolerance ||
               Mathf.Abs(fingerEnd.y - fingerStart.y) > tolerance)
            {
                if (!IsAnyMoving())
                {
                    InputHandled = true;
                    if (Mathf.Abs(fingerStart.x - fingerEnd.x) > Mathf.Abs(fingerStart.y - fingerEnd.y))
                    {
                        if ((fingerEnd.x - fingerStart.x) > 0)
                        {
                            MoveTilesRight();
                        }
                        else
                        {
                            MoveTilesLeft();
                        }
                    }

                    else
                    {
                        if ((fingerEnd.y - fingerStart.y) > 0)
                        {
                            MoveTilesUp();
                        }
                        else
                        {
                            MoveTilesDown();
                        }
                    }
                }

                fingerStart = fingerEnd;
            }

        }

        if (CreateNewTile && !IsAnyMoving())
        {
            CreateRandomTile();

            CreateNewTile = false;
        }
    }

    bool IsAnyMoving()
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (Tiles[i, j] != null && Tiles[i, j].IsMoving)
                    return true;
            }
        }

        return false;
    }
}
