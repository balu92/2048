using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CreateGameUI : MonoBehaviour
{
    long BestScore = 0;
    long currentScore = 0;
    public long CurrentScore
    {
        get => currentScore;
        set
        {
            currentScore = value;
            if (currentScore > BestScore) BestScore = currentScore;
            Score.text = $"Best score: {BestScore}\r\nCurrent score: {currentScore}";
        }
    }

    public UnityEngine.UI.Text Score;

    public OrientationManager OrientationManager;

    ScreenOrientation Orientation = ScreenOrientation.Portrait;

    public NumberTile[,] Tiles = new NumberTile[4,4];

    public Canvas Canvas;

    public GameObject NumberTilePrefab;

    public GameObject TileBackgroundPrefab;

    public GameObject PlayAreaBackgroundPrefab;

    public int tolerance = 200;

    private Vector2 fingerStart;
    private Vector2 fingerEnd;

    int StartingTile = 2;

    bool CreateNewTile = false;

    bool InputHandled = true;

    void Start()
    {
        Application.targetFrameRate = Screen.currentResolution.refreshRate;

        List<Vector2Int> FreeTiles = new List<Vector2Int>();

        var playAreaBkg = Canvas.Instantiate(PlayAreaBackgroundPrefab);
        int bkgsize = (IsPortrait() ? Screen.width : Screen.height);

        bool isPortrait = IsPortrait();
        playAreaBkg.GetComponent<RectTransform>().sizeDelta = new Vector2(bkgsize, bkgsize);
        playAreaBkg.transform.position = new Vector2(0, 0 + bkgsize / (isPortrait ? 6 : 4));
        playAreaBkg.transform.SetParent(Canvas.transform, false);

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                FreeTiles.Add(new Vector2Int(i, j));
                var tilebkg = Canvas.Instantiate(TileBackgroundPrefab);

                tilebkg.transform.position = GetTilePosition(i, j);
                tilebkg.transform.SetParent(Canvas.transform, false);

                int tileSize = GetTileSize();

                tilebkg.GetComponent<RectTransform>().sizeDelta = new Vector2(tileSize, tileSize);
                //CreateTile(i, j, 2);
            }
        }

        for (int i = 0; i < StartingTile; i++)
        {
            var rand = Mathf.RoundToInt(Random.Range(-0.5f, FreeTiles.Count - 0.5001f));
            CreateTile(FreeTiles[rand].x, FreeTiles[rand].y, 2);
            FreeTiles.RemoveAt(rand);
        }
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
        var tileObject = Canvas.Instantiate(NumberTilePrefab);

        tileObject.transform.position = GetTilePosition(column, row);
        tileObject.transform.SetParent(Canvas.transform, false);

        int tileSize = GetTileSize();

        tileObject.GetComponent<RectTransform>().sizeDelta = new Vector2(tileSize, tileSize);
        var txt = tileObject.GetComponentInChildren<UnityEngine.UI.Text>();
        txt.text = value.ToString();
        //txt.fontSize = tileSize / 4;

        var tile = tileObject.GetComponent<NumberTile>();
        
        tile.Value = value;
        tile.Column = column;
        tile.Row = row;

        tile.Destination = tileObject.transform.localPosition;

        tile.gameUI = this;

        Tiles[column, row] = tile;

        return tile;
    }

    public int GetTileSize() => Mathf.RoundToInt((IsPortrait() ? Screen.width : Screen.height) * 0.2125f);

    public bool IsPortrait() => (Orientation & (ScreenOrientation.Portrait | ScreenOrientation.PortraitUpsideDown)) > 0;// Orientation == ScreenOrientation.Portrait || Orientation == ScreenOrientation.PortraitUpsideDown;

    public Vector3 GetTilePosition(int column, int row)
    {
        bool isPortrait = IsPortrait();

        int TileSize = GetTileSize();
        int PlayAreaSize = isPortrait ? Screen.width : Screen.height;

        var Margin = Mathf.RoundToInt(PlayAreaSize * 0.03f);

        int x = Margin + column * (TileSize + Margin) + TileSize / 2;
        int y = Margin + row * (TileSize + Margin) + TileSize / 2;
        x -= PlayAreaSize / (isPortrait ? 2 : 3);
        y -= PlayAreaSize / (isPortrait ? 3 : 2);

        return new Vector3(x, y, 0);
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

    void LateUpdate()
    {
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
