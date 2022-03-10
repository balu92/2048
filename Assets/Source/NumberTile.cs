using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NumberTile : MonoBehaviour
{
    private Vector3 destination;

    public Vector3 Destination
    {
        get => destination;
        set
        {
            IsMoving = true;
            destination = value;
        }
    }

    NumberTile MergeWith = null;

    public CreateGameUI gameUI;

    public int Column;
    public int Row;

    public int value;

    public int Value
    {
        get => value;
        set
        {
            this.value = value;
            GetComponentInChildren<UnityEngine.UI.Text>().text = value.ToString();
            GetComponent<UnityEngine.UI.Image>().color = value > 2048 ? TileColours[0] : TileColours[value];
        }
    }

    public static readonly Dictionary<int, Color> TileColours = new Dictionary<int, Color>()
    {
        { 0, new Color(0.24f, 0.23f, 0.2f, 1f) },
        { 2, new Color(0.93f, 0.89f, 0.85f, 1f) },
        { 4, new Color(0.93f, 0.88f, 0.78f, 1f) },
        { 8, new Color(0.95f, 0.69f, 0.47f, 1f) },
        { 16, new Color(0.96f, 0.58f, 0.39f, 1f) },
        { 32, new Color(0.96f, 0.49f, 0.37f, 1f) },
        { 64, new Color(0.96f, 0.37f, 0.23f, 1f) },
        { 128, new Color(0.93f, 0.81f, 0.45f, 1f) },
        { 256, new Color(0.93f, 0.8f, 0.38f, 1f) },
        { 512, new Color(0.93f, 0.78f, 0.31f, 1f) },
        { 1024, new Color(0.93f, 0.79f, 0.33f, 1f) },
        { 2048, new Color(0.95f, 0.84f, 0.47f, 1f) }
    };

    public bool IsMoving = false;

    bool NeedsScaling = true;

    bool IsMerging = false;

    void Start()
    {
        gameObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        GetComponent<UnityEngine.UI.Image>().color = TileColours[2];
    }

    public override string ToString()
    {
        return $"COLUMN: {Column} ROW: {Row} VALUE: {Value} TEXT: {GetComponentInChildren<UnityEngine.UI.Text>().text}";
    }

    void Update()
    {
        if (NeedsScaling) {
            if (Vector3.Distance(gameObject.transform.localScale, Vector3.one) > 0.01f)
                gameObject.transform.localScale = Vector3.Lerp(gameObject.transform.localScale, Vector3.one, Time.deltaTime * 13);
            else
            {
                gameObject.transform.localScale = Vector3.one;
                NeedsScaling = false;
            }
        }

        float dist;
        if (gameObject.transform.localPosition == Destination && IsMoving)
        {
            IsMoving = false;
        }
        else if ((dist = Vector3.Distance(gameObject.transform.localPosition, Destination)) > 0.05f)
        {
            gameObject.transform.localPosition = Vector3.Lerp(gameObject.transform.localPosition, Destination, Time.deltaTime * 32f);

            if (IsMerging && dist < GetComponent<RectTransform>().sizeDelta.x / 2)
            {
                if (MergeWith != null)
                {
                    MergeWith.AddScore();
                    MergeWith.IsMerging = false;
                    Destroy(gameObject);
                }
            }
        }
        else if (IsMoving)
        {
            gameObject.transform.localPosition = Destination;
        }
    }

    public int GetValueOf(int column, int row)
    {
        return gameUI.Tiles[column, row] == null ? 0 : gameUI.Tiles[column, row].Value;
    }

    public void AddScore()
    {
        Value += Value;
        gameUI.CurrentScore += Value;
    }

    public void Move(int column, int row, bool merge)
    {
        MergeWith = merge ? gameUI.Tiles[column, row] : null;

        if (merge)
        {
            MergeWith.IsMerging = true;
            IsMerging = true;
        }

        gameUI.Tiles[Column, Row] = null;

        Destination = gameUI.GetTilePosition(column, row);

        Column = column;
        Row = row;

        if (!merge) gameUI.Tiles[Column, Row] = this;
    }

    public bool CanBeMerged(int column, int row) => !gameUI.Tiles[column, row].IsMerging;

    public bool MoveLeft()
    {
        if (Column > 0)
        {
            var moves = GetDestinationLeft();

            if (Column != moves.Item2)
            {
                Move(moves.Item2, Row, moves.Item1);
                return true;
            }
        }
        return false;
    }

    public (bool, int) GetDestinationLeft()
    {
        int result = Column;

        for (int column = Column - 1; column >= 0; column--)
        {
            if (gameUI.Tiles[column, Row] == null)
            {
                result = column;
            }
            else if (gameUI.Tiles[column, Row].Value != Value)
            {
                return (false, column + 1);
            }
            else if (gameUI.Tiles[column, Row].Value == Value)
            {
                if (CanBeMerged(column, Row))
                    return (true, column);
                return (false, column + 1);
            }
        }
        return (false, result);
    }

    public bool MoveRight()
    {
        if (Column < 3)
        {
            var moves  = GetDestinationRight();

            if (Column != moves.Item2)
            {
                Move(moves.Item2, Row, moves.Item1);
                return true;
            }
        }
        return false;
    }

    public (bool, int) GetDestinationRight()
    {
        int result = Column;
        for (int column = Column + 1; column < 4; column++)
        {
            if (gameUI.Tiles[column, Row] == null)
            {
                result = column;
            }
            else if (gameUI.Tiles[column, Row].Value != Value)
            {
                return (false, column - 1);
            }
            else if (gameUI.Tiles[column, Row].Value == Value)
            {
                if (CanBeMerged(column, Row))
                    return (true, column);
                return (false, column - 1);
            }
        }
        return (false, result);
    }

    public bool MoveDown()
    {
        if (Row > 0)
        {
            var moves = GetDestinationDown();

            if (Row != moves.Item2)
            {
                Move(Column, moves.Item2, moves.Item1);
                return true;
            }
        }
        return false;
    }

    public (bool, int) GetDestinationDown()
    {
        int result = Row;
        for (int row = Row - 1; row >= 0; row--)
        {
            if (gameUI.Tiles[Column, row] == null)
            {
                result = row;
            }
            else if (gameUI.Tiles[Column, row].Value != Value)
            {
                return (false, row + 1);
            }
            else if (gameUI.Tiles[Column, row].Value == Value)
            {
                if (CanBeMerged(Column, row))
                    return (true, row);
                return (false, row + 1);
            }
        }
        return (false, result);
    }

    public bool MoveUp()
    {
        if (Row < 3)
        {
            var moves = GetDestinationUp();

            if (Row != moves.Item2)
            {
                Move(Column, moves.Item2, moves.Item1);
                return true;
            }
        }
        return false;
    }

    public (bool, int) GetDestinationUp()
    {
        int result = Row;
        for (int row = Row + 1; row < 4; row++)
        {
            if (gameUI.Tiles[Column, row] == null)
            {
                result = row;
            }
            else if (gameUI.Tiles[Column, row].Value != Value)
            {
                return (false, row - 1);
            }
            else if (gameUI.Tiles[Column, row].Value == Value)
            {
                if (CanBeMerged(Column, row))
                    return (true, row);
                return (false, row - 1);
            }
        }
        return (false, result);
    }
}
