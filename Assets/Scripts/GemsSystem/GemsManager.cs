using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GemsManager : SingletonMonobehaviour<GemsManager>
{
    [Header ("Board Properties:")]
    [SerializeField] private int boardWidth = 5;
    [SerializeField] private int boardHeight = 8;
    [Header ("Board References:")]
    [SerializeField] private RectTransform gemsContainer;
    [Header ("Pools References:")]
    [SerializeField] private Pool gemsPool;
    [SerializeField] private Pool particlesPool;
    [SerializeField] private Vector2 gemSize;
    [Header ("Audio References:")]
    [SerializeField] private AudioSource gemMatchAudioSource;

    private Gem[, ] gemsBoard;
    private List<Gem> gemsToFall;

    public int BoardWidth { get { return boardWidth; } }
    public int BoardHeight { get { return boardHeight; } }
    public Vector2 GemSize { get { return gemSize; } }

    override protected void Awake() {
        base.Awake();
        gemsBoard = new Gem[boardWidth, boardHeight];
        gemsToFall = new List<Gem> ();
    }

    #region----- STATIC METHODS

    public static Gem[, ] GetGemsBoard ()
    {
        return Instance.gemsBoard;
    }

    #endregion<--- STATIC METHODS --->

    #region----- PUBLIC METHODS

    public void SpawnInitialGems ()
    {
        var center = GetBoardCenterPosition ();
        var posX = center.x - gemSize.x * (boardWidth / 2);

        for (int x = 0; x < boardWidth; x++)
        {
            var posY = center.y - gemSize.y * (boardHeight / 2);
            for (int y = 0; y < boardHeight; y++)
            {
                var gem = SpawnGem (new Vector2 (posX, posY), x, y);

                while (GemWillMatch (gem))
                    gem.RandomizeType ();

                posY += gemSize.y;
            }

            posX += gemSize.x;
        }
    }

    public void ResetGemsBoard()
    {
        StopAllCoroutines();

        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                gemsBoard[x, y].Despawn();
                gemsBoard[x, y] = null;
            }
        }
    }

    public void DestroyAllMatchedGems ()
    {
        var matchCount = 0;
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                if (gemsBoard[x, y] != null && gemsBoard[x, y].IsMatched)
                {
                    matchCount++;
                    particlesPool.GetPoolItem(gemsBoard[x, y].transform.position, Quaternion.identity);
                    DestroyGem (gemsBoard[x, y]);
                }
            }
        }

        for (int i = 0; i < matchCount/3; i++)
        {
            GameManager.AddScore();
            gemMatchAudioSource.Play();
        }
    }

    public void DestroyGem (Gem gemToDestroy)
    {
        if (gemsBoard[gemToDestroy.Column, gemToDestroy.Row] != gemToDestroy) return;

        gemsBoard[gemToDestroy.Column, gemToDestroy.Row] = null;
        gemToDestroy.Despawn ();
    }

    public void CalculateGemsToFall (Action onDone)
    {
        StartCoroutine (ProcessGemsToFall (onDone));
    }

    #region<--- Match Check Methods --->

    public bool HasMatches (Gem selectedGem, Gem neighboarGem)
    {
        return FindMatches (selectedGem) || FindMatches (neighboarGem);
    }

    #endregion<--- Match Check Methods --->

    #endregion<--- PUBLIC METHODS --->

    #region----- PRIVATE METHODS

    #region--- Setup Methods

    #region- match check methods

    private bool HasMatchesOnBoard ()
    {
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                if (FindMatches (gemsBoard[x, y]))
                    return true;
            }
        }

        return false;
    }

    private bool FindMatches (Gem gem)
    {
        var matches = new List<Gem> ();

        var horizontalMatches = new List<Gem> ();

        for (int i = 0; i < boardWidth; i++)
        {
            horizontalMatches.Clear ();
            if (gem == null || gemsBoard[i, gem.Row] == null) break;

            horizontalMatches.Add (gemsBoard[i, gem.Row]);

            if (CheckColumnMatch (gem.Row, i, ref horizontalMatches))
            {
                matches.AddRange (horizontalMatches);
                break;
            }
        }

        var verticalMatches = new List<Gem> ();

        for (int i = 0; i < boardHeight; i++)
        {
            verticalMatches.Clear ();
            if (gem == null || gemsBoard[gem.Column, i] == null) break;

            verticalMatches.Add (gemsBoard[gem.Column, i]);

            if (CheckRowMatch (gem.Column, i, ref verticalMatches))
            {
                matches.AddRange (verticalMatches);
                break;
            }
        }

        foreach (var matchedGem in matches)
            matchedGem.IsMatched = true;

        return matches.Count > 0;
    }

    private bool CheckColumnMatch (int row, int index, ref List<Gem> matches)
    {
        if (index >= 0 && index + 1 < boardWidth && gemsBoard[index, row] != null && gemsBoard[index + 1, row] != null && gemsBoard[index, row].Type == gemsBoard[index + 1, row].Type)
        {
            matches.Add (gemsBoard[index + 1, row]);
            return CheckColumnMatch (row, index + 1, ref matches);
        }

        return matches.Count >= 3;
    }

    private bool CheckRowMatch (int column, int index, ref List<Gem> matches)
    {
        if (index >= 0 && index + 1 < boardHeight && gemsBoard[column, index] != null && gemsBoard[column, index + 1] != null && gemsBoard[column, index].Type == gemsBoard[column, index + 1].Type)
        {
            matches.Add (gemsBoard[column, index + 1]);
            return CheckRowMatch (column, index + 1, ref matches);
        }

        return matches.Count >= 3;
    }

    #endregion- match check methods

    #region- spawn methods

    private Gem SpawnGem (Vector2 pos, int x, int y)
    {
        var gem = gemsPool.GetPoolItem (pos, Quaternion.identity, gemsContainer) as Gem;
        if (gem == null) return null;

        gem.name = "Gem(" + x + "," + y + ")";
        gem.Column = x;
        gem.LastColumn = x;
        gem.Row = y;
        gem.LastRow = y;
        gemsBoard[x, y] = gem;

        return gem;
    }

    private bool GemWillMatch (Gem currentGem)
    {
        if (currentGem.Column > 1 && currentGem.Row > 1)
        {
            return gemsBoard[currentGem.Column - 1, currentGem.Row].Type == currentGem.Type &&
                gemsBoard[currentGem.Column - 2, currentGem.Row].Type == currentGem.Type ||
                gemsBoard[currentGem.Column, currentGem.Row - 1].Type == currentGem.Type &&
                gemsBoard[currentGem.Column, currentGem.Row - 2].Type == currentGem.Type;
        }
        else if (currentGem.Column <= 1 || currentGem.Row <= 1)
        {
            if (currentGem.Row > 1 &&
                gemsBoard[currentGem.Column, currentGem.Row - 1].Type == currentGem.Type &&
                gemsBoard[currentGem.Column, currentGem.Row - 2].Type == currentGem.Type) return true;

            if (currentGem.Column > 1 &&
                gemsBoard[currentGem.Column - 1, currentGem.Row].Type == currentGem.Type &&
                gemsBoard[currentGem.Column - 2, currentGem.Row].Type == currentGem.Type) return true;
        }

        return false;
    }

    #endregion- spawn methods

    #endregion--- Setup Methods

    #region--- Helpers Methods

    private Vector2 GetBoardCenterPosition ()
    {
        Vector3[] v = new Vector3[4];
        gemsContainer.GetWorldCorners (v);

        var minX = v[0].x;
        var maxX = v[2].x;
        var minY = v[0].y;
        var maxY = v[1].y;

        var centerPos = new Vector2
        {
            x = minX + ((maxX - minX) / 2),
            y = minY + ((maxY - minY) / 2)
        };

        return centerPos;
    }

    private bool HasNoMovementsLeft ()
    {
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                if (gemsBoard[x, y] != null && x < boardWidth - 1 && CheckForFutureMatches (x, y, Vector2.right) || y < boardHeight - 1 && CheckForFutureMatches (x, y, Vector2.up))
                    return false;
            }
        }

        return true;
    }

    private bool CheckForFutureMatches (int x, int y, Vector2 direction)
    {
        SwitchGem (x, y, direction);
        if (WillHaveFutureMatches ())
        {
            SwitchGem (x, y, direction);
            return true;
        }

        SwitchGem (x, y, direction);
        return false;
    }

    private void SwitchGem (int x, int y, Vector2 direction)
    {
        var tempGem = gemsBoard[x + (int) direction.x, y + (int) direction.y];
        gemsBoard[x + (int) direction.x, y + (int) direction.y] = gemsBoard[x, y];
        gemsBoard[x, y] = tempGem;
    }

    private bool WillHaveFutureMatches ()
    {
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                if (gemsBoard[x, y] != null)
                {
                    if (x + 1 < boardWidth && x + 2 < boardWidth && gemsBoard[x + 1, y] != null && gemsBoard[x + 2, y] != null && gemsBoard[x, y].Type == gemsBoard[x + 1, y].Type && gemsBoard[x, y].Type == gemsBoard[x + 2, y].Type)
                        return true;

                    if (y + 1 < boardHeight && y + 2 < boardHeight && gemsBoard[x, y + 1] != null && gemsBoard[x, y + 2] != null && gemsBoard[x, y].Type == gemsBoard[x, y + 1].Type && gemsBoard[x, y].Type == gemsBoard[x, y + 2].Type)
                        return true;
                }
            }
        }

        return false;
    }

    private void ShuffleBoard ()
    {
        int rows = gemsBoard.GetLength (0);
        int columns = gemsBoard.GetLength (1);
        var randomizedBoard = new Gem[rows, columns];

        int[] shuffledRowIndexes = Enumerable.Range (0, rows).ToArray ();
        int[] shuffledColumnIndexes = Enumerable.Range (0, columns).ToArray ();

        System.Random rnd = new System.Random ();
        shuffledRowIndexes = shuffledRowIndexes.OrderBy (x => rnd.Next ()).ToArray ();

        var center = GetBoardCenterPosition ();
        var posX = center.x - gemSize.x * (boardWidth / 2);

        for (int i = 0; i < rows; i++)
        {
            shuffledColumnIndexes = shuffledColumnIndexes.OrderBy (x => rnd.Next ()).ToArray ();
            var posY = center.y - gemSize.y * (boardHeight / 2);

            for (int j = 0; j < columns; j++)
            {
                randomizedBoard[i, j] = gemsBoard[shuffledRowIndexes.ElementAt (i), shuffledColumnIndexes.ElementAt (j)];

                randomizedBoard[i, j].TargetPosition = new Vector2 (posX, posY);
                randomizedBoard[i, j].Column = i;
                randomizedBoard[i, j].Row = j;
                posY += gemSize.y;
            }

            posX += gemSize.x;
        }

        gemsBoard = randomizedBoard;
    }

    #endregion--- Helpers Methods

    #region--- Refil Methods

    private IEnumerator RefilGemsBoard (Action onDone)
    {
        yield return SpawnNewGemsToRefil ();

        if (HasMatchesOnBoard ())
        {
            yield return new WaitForSeconds (0.5f);
            DestroyAllMatchedGems ();

            yield return new WaitForSeconds (0.5f);
            yield return ProcessGemsToFall (onDone);
        }
        else
        {
            if (HasNoMovementsLeft ())
                yield return ShuffleEntireBoard ();

            onDone?.Invoke ();
        }
    }

    private IEnumerator SpawnNewGemsToRefil ()
    {
        var center = GetBoardCenterPosition ();
        var posX = center.x - gemSize.x * (boardWidth / 2);

        var newGems = new List<Gem> ();

        for (int x = 0; x < boardWidth; x++)
        {
            var posY = center.y - gemSize.y * (boardHeight / 2);

            for (int y = 0; y < boardHeight; y++)
            {
                if (gemsBoard[x, y] == null)
                {
                    var yOffset = (y + 1) * gemSize.y * (boardHeight / 2);
                    var newGem = SpawnGem (new Vector2 (posX, posY + yOffset), x, y);
                    newGem.TargetPosition = new Vector2 (posX, posY);
                    newGems.Add (newGem);
                }

                posY += gemSize.y;
            }

            posX += gemSize.x;
        }

        if (newGems.Count > 0)
        {
            var finished = false;
            GemsController.Instance.UpdatedNewSpawnedGems (newGems, () => { finished = true; });

            while (!finished)
                yield return null;
        }
    }

    private IEnumerator ShuffleEntireBoard ()
    {
        while (HasNoMovementsLeft ())
        {
            ShuffleBoard ();

            var finished = false;
            GemsController.Instance.UpdatedAllGems (() => { finished = true; });
            while (!finished) yield return null;

            if (HasMatchesOnBoard ())
            {
                yield return new WaitForSeconds (0.5f);
                DestroyAllMatchedGems ();

                yield return new WaitForSeconds (0.5f);
                yield return ProcessGemsToFall (null);
            }
        }

    }

    #endregion--- Refil Methods

    #region--- Others

    private IEnumerator ProcessGemsToFall (Action onDone)
    {
        int fallSteps = 0;

        gemsToFall.Clear ();

        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                if (gemsBoard[x, y] == null)
                {
                    fallSteps++;
                }
                else if (fallSteps > 0)
                {
                    gemsBoard[x, y].PrepareToFall (fallSteps);
                    gemsToFall.Add (gemsBoard[x, y]);
                    gemsBoard[x, y] = null;
                }
            }
            fallSteps = 0;
        }

        foreach (var gemToFall in gemsToFall)
        {
            gemToFall.Fall ();
            yield return new WaitForSeconds (0.01f);
        }

        yield return RefilGemsBoard (onDone);
    }

    #endregion--- Others

    #endregion----- PRIVATE METHODS
}