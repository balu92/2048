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

    public UnityEngine.UI.Image TileImage;
    public UnityEngine.UI.Text ValueText;

    public int Column;
    public int Row;

    private int value;

    public int Value
    {
        get => value;
        set
        {
            this.value = value;
            UpdateValue();
        }
    }

    void UpdateValue()
    {
        ValueText.text = Value.ToString();
        TileImage.CrossFadeColor(Value > 2048 ? TileColours[0] : TileColours[Value], 0.016f, true, false);
    }

    public static readonly Dictionary<int, Color> TileColours = new Dictionary<int, Color>()
    {
        { 0, new Color(0.24f, 0.23f, 0.2f) },
        { 2, new Color(0.93f, 0.89f, 0.85f) },
        { 4, new Color(0.93f, 0.88f, 0.78f) },
        { 8, new Color(0.95f, 0.69f, 0.47f) },
        { 16, new Color(0.96f, 0.58f, 0.39f) },
        { 32, new Color(0.96f, 0.49f, 0.37f) },
        { 64, new Color(0.96f, 0.37f, 0.23f) },
        { 128, new Color(0.93f, 0.81f, 0.45f) },
        { 256, new Color(0.93f, 0.8f, 0.38f) },
        { 512, new Color(0.93f, 0.78f, 0.31f) },
        { 1024, new Color(0.93f, 0.79f, 0.33f) },
        { 2048, new Color(0.95f, 0.84f, 0.47f) }
    };

    public bool IsMoving = false;

    bool NeedsScaling = true;

    bool IsMerging = false;

    void Awake()
    {
        gameObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        ValueText = GetComponentInChildren<UnityEngine.UI.Text>();
        TileImage = GetComponent<UnityEngine.UI.Image>();

        TileImage.CrossFadeColor(TileColours[2], 0.016f, true, false);
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
        UpdateValue();
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
            var goal = GetDestinationLeft();

            if (Column != goal.Column)
            {
                Move(goal.Column, Row, goal.IsMerge);
                return true;
            }
        }
        return false;
    }

    public Goal GetDestinationLeft()
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
                return new Goal(column + 1, Row);
            }
            else if (gameUI.Tiles[column, Row].Value == Value)
            {
                if (CanBeMerged(column, Row))
                    return new Goal(column, Row, true);
                return new Goal(column + 1, Row);
            }
        }
        return new Goal(result, Row);
    }

    public bool MoveRight()
    {
        if (Column < 3)
        {
            var goal  = GetDestinationRight();

            if (Column != goal.Column)
            {
                Move(goal.Column, Row, goal.IsMerge);
                return true;
            }
        }
        return false;
    }

    public Goal GetDestinationRight()
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
                return new Goal(column - 1, Row);
            }
            else if (gameUI.Tiles[column, Row].Value == Value)
            {
                if (CanBeMerged(column, Row))
                    return new Goal(column, Row, true);
                return new Goal(column - 1, Row);
            }
        }
        return new Goal(result, Row);
    }

    public bool MoveDown()
    {
        if (Row > 0)
        {
            var goal = GetDestinationDown();

            if (Row != goal.Row)
            {
                Move(Column, goal.Row, goal.IsMerge);
                return true;
            }
        }
        return false;
    }

    public Goal GetDestinationDown()
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
                return new Goal(Column, row + 1);
            }
            else if (gameUI.Tiles[Column, row].Value == Value)
            {
                if (CanBeMerged(Column, row))
                    return new Goal(Column, row, true);
                return new Goal(Column, row + 1);
            }
        }
        return new Goal(Column, result);
    }

    public bool MoveUp()
    {
        if (Row < 3)
        {
            var goal = GetDestinationUp();

            if (Row != goal.Row)
            {
                Move(Column, goal.Row, goal.IsMerge);
                return true;
            }
        }
        return false;
    }

    public Goal GetDestinationUp()
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
                return new Goal(Column, row - 1);
            }
            else if (gameUI.Tiles[Column, row].Value == Value)
            {
                if (CanBeMerged(Column, row))
                    return new Goal(Column, row, true);
                return new Goal(Column, row - 1);
            }
        }
        return new Goal(Column, result);
    }

    public struct Goal
    {
        public int Column;
        public int Row;
        public bool IsMerge;

        public Goal(int column, int row, bool ismerge = false)
        {
            Column = column;
            Row = row;
            IsMerge = ismerge;
        }
    }
}
