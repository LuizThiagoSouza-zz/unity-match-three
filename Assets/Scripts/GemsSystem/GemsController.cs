using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GemsController : SingletonMonobehaviour<GemsController>
{
    private enum SwipeDirection { Nothing, Up = 1, Down = -1, Left = -1, Right = 1 }

    [SerializeField] private AudioSource gemSwipeAudioSource;
    [SerializeField] private AudioSource gemFallAudioSource;

    private List<GemPair> gemsPairs;
    private GemsManager gemsManager;
    private bool needFallGems,
    hasNewGems;

    public bool AllowSwipe { get; private set; }

    protected override void Awake ()
    {
        base.Awake ();

        gemsPairs = new List<GemPair> ();
    }

    private void Start ()
    {
        gemsManager = GemsManager.Instance;
    }

    private void Update ()
    {
        if (hasNewGems) return;

        if (needFallGems && gemsPairs.Count <= 0)
        {
            needFallGems = false;
            GemsManager.Instance.CalculateGemsToFall (() => { AllowSwipe = true; });
        }

        if (!AllowSwipe) return;

        for (int i = gemsPairs.Count - 1; i >= 0; i--)
        {
            if (gemsPairs[i].UpdateMovements ())
            {
                if (!needFallGems && gemsPairs[i].HasMatch)
                    needFallGems = true;

                gemsPairs.RemoveAt (i);
            }
        }

        if (needFallGems)
            AllowSwipe = false;
    }

    public void ResetController ()
    {
        gemsPairs.Clear ();
        AllowSwipe = true;
        needFallGems = false;
        hasNewGems = false;
    }

    public void UpdatedNewSpawnedGems (List<Gem> newGems, Action onFinished)
    {
        if (newGems == null || newGems.Count <= 0)
        {
            hasNewGems = false;
            return;
        }

        StartCoroutine (ProcessNewSpawnedGemsMovements (newGems, onFinished));
    }

    private IEnumerator ProcessNewSpawnedGemsMovements (List<Gem> newGems, Action onFinished)
    {
        hasNewGems = true;

        while (newGems.Count > 0)
        {
            for (int i = newGems.Count - 1; i >= 0; i--)
            {
                if (!newGems[i].ProcessMovement ())
                {
                    gemFallAudioSource.Play();
                    newGems.RemoveAt (i);
                }
            }

            yield return null;
        }

        hasNewGems = false;

        onFinished.Invoke ();
    }

    public void UpdatedAllGems (Action onFinished)
    {
        StartCoroutine (ProcessAllNewGemsMovements (onFinished));
    }

    private IEnumerator ProcessAllNewGemsMovements (Action onFinished)
    {
        var gemsBoard = GemsManager.GetGemsBoard ();
        var width = gemsBoard.GetLength (0);
        var height = gemsBoard.GetLength (1);
        var gemsToMove = new List<Gem> (width * height);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
                gemsToMove.Add (gemsBoard[x, y]);
        }

        while (gemsToMove.Count > 0)
        {
            for (int i = gemsToMove.Count - 1; i >= 0; i--)
            {
                if (!gemsToMove[i].ProcessMovement ())
                    gemsToMove.RemoveAt (i);
            }

            yield return null;
        }

        onFinished.Invoke ();
    }

    public void SwipeGem (Gem gemToSwipe, float swipeAngle)
    {
        if (Mathf.Abs (swipeAngle) <= 0) return;

        var neighboarGem = GetNeighboarGem (gemToSwipe, swipeAngle);
        if (neighboarGem != null)
        {
            gemSwipeAudioSource.Play ();
            gemsPairs.Add (new GemPair (gemToSwipe, neighboarGem));
        }
    }

    private Gem GetNeighboarGem (Gem selectedGem, float swipeAngle)
    {
        //RIGHT SWIPE
        if (swipeAngle > -45 && swipeAngle <= 45)
            return GetHorizontalNeighboar (selectedGem, (int) SwipeDirection.Right);

        //UP SWIPE
        if (swipeAngle > 45 && swipeAngle <= 135)
            return GetVerticalNeighboar (selectedGem, (int) SwipeDirection.Up);

        //LEFT SWIPE
        if (swipeAngle > 135 || swipeAngle <= -135)
            return GetHorizontalNeighboar (selectedGem, (int) SwipeDirection.Left);

        //DOWN SWIPE
        if (swipeAngle < -45 && swipeAngle >= -135)
            return GetVerticalNeighboar (selectedGem, (int) SwipeDirection.Down);

        return null;
    }

    private Gem GetVerticalNeighboar (Gem currentGem, int dir)
    {
        if (currentGem.Row + dir < 0 || currentGem.Row + dir >= gemsManager.BoardHeight)
            return null;

        var NeighboarGem = GemsManager.GetGemsBoard () [currentGem.Column, currentGem.Row + dir];
        if (NeighboarGem == null)
            return null;

        NeighboarGem.LastRow = NeighboarGem.Row;
        NeighboarGem.Row -= dir;

        currentGem.LastRow = currentGem.Row;
        currentGem.Row += dir;

        return NeighboarGem;
    }

    private Gem GetHorizontalNeighboar (Gem currentGem, int dir)
    {
        if (currentGem.Column + dir < 0 || currentGem.Column + dir >= gemsManager.BoardWidth)
            return null;

        var NeighboarGem = GemsManager.GetGemsBoard () [currentGem.Column + dir, currentGem.Row];
        if (NeighboarGem == null)
            return null;

        NeighboarGem.LastColumn = NeighboarGem.Column;
        NeighboarGem.Column -= dir;

        currentGem.LastColumn = currentGem.Column;
        currentGem.Column += dir;

        return NeighboarGem;
    }

    private class GemPair
    {
        public Gem SelectedGem { get; private set; }
        public Gem NeighboarGem { get; private set; }
        public bool HasMatch { get; private set; }

        public GemPair (Gem gemToSelect, Gem neighboarGem)
        {
            SelectedGem = gemToSelect;
            NeighboarGem = neighboarGem;

            NeighboarGem.LastPosition = NeighboarGem.transform.position;
            NeighboarGem.TargetPosition = SelectedGem.transform.position;
            SelectedGem.LastPosition = SelectedGem.transform.position;
            SelectedGem.TargetPosition = NeighboarGem.transform.position;
        }

        public bool UpdateMovements ()
        {
            if (SelectedGem == null || NeighboarGem == null) return false;

            if (!SelectedGem.ProcessMovement () && !NeighboarGem.ProcessMovement ())
            {
                GemsManager.Instance.HasMatches (SelectedGem, NeighboarGem);

                if (!GemsManager.Instance.HasMatches (SelectedGem, NeighboarGem))
                {
                    SelectedGem.ReturnToLastPosition ();
                    NeighboarGem.ReturnToLastPosition ();
                }
                else
                {
                    GemsManager.Instance.DestroyAllMatchedGems ();

                    if (SelectedGem.isActiveAndEnabled)
                        SelectedGem.UpdateGemBoard ();

                    if (NeighboarGem.isActiveAndEnabled)
                        NeighboarGem.UpdateGemBoard ();

                    HasMatch = true;
                }

                return true;
            }

            return false;
        }
    }
}