using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Gem : PoolItemMonobehaviour
{
    [Header("Gem References:")]
    [SerializeField] private Image image;
    [SerializeField] private List<Sprite> sprites;
    [SerializeField] private EventTrigger eventTrigger;

    private Vector2 initialPointerPos, finalPointerPos;
    private bool isHorizontalMoving, isVerticalMoving;
    private float swipeDeadZone = 1f;
    private Vector2 fallPosition;
    
    public int Column { get; set; }
    public int LastColumn { get; set; }
    public int Row { get; set; }
    public int LastRow { get; set; }
    public Image Image { get { return image; } }
    public bool IsMatched { get; set; }
    public Vector2 TargetPosition { get; set; }
    public Vector2 LastPosition { get; set; }
    public string Type { get { return image.sprite.name; } }

    private void Start()
    {
        SetupPointerEvents();
    }

    #region <--- PUBLIC METHODS --->

    public bool ProcessMovement()
    {
        if (Mathf.Abs(TargetPosition.x - transform.position.x) > 0.1f)
        {
            transform.position = Vector2.Lerp(transform.position, new Vector2(TargetPosition.x, transform.position.y), 0.4f);
            isHorizontalMoving = true;
        }
        else if (isHorizontalMoving)
        {
            transform.position = new Vector2(TargetPosition.x, transform.position.y);
            GemsManager.GetGemsBoard()[Column, Row] = this;
            isHorizontalMoving = false;
        }

        if (Mathf.Abs(TargetPosition.y - transform.position.y) > 0.1f)
        {
            transform.position = Vector2.Lerp(transform.position, new Vector2(transform.position.x, TargetPosition.y), 0.4f);
            isVerticalMoving = true;
        }
        else if (isVerticalMoving)
        {
            transform.position = new Vector2(transform.position.x, TargetPosition.y);
            GemsManager.GetGemsBoard()[Column, Row] = this;
            isVerticalMoving = false;
        }

        return isHorizontalMoving || isVerticalMoving;
    }

    public void UpdateGemBoard()
    {
        GemsManager.GetGemsBoard()[Column, Row] = this;
    }

    public void ReturnToLastPosition()
    {
        Column = LastColumn;
        Row = LastRow;
        UpdateGemBoard();
        StartCoroutine(MoveToLastPosition());
    }

    public void PrepareToFall(int fallSteps)
    {
        Row -= fallSteps;
        LastRow = Row;
        LastColumn = Column;
        UpdateGemBoard();

        fallPosition = transform.position;
        fallPosition.y -= fallSteps * GemsManager.Instance.GemSize.y;
    }

    public void Fall()
    {
        if (!isActiveAndEnabled) return;

        StartCoroutine(MoveToFallPosition(fallPosition));
    }

    public void RandomizeType()
    {
        image.sprite = sprites[UnityEngine.Random.Range(0, sprites.Count)];
    }

    #endregion <--- PUBLIC METHODS --->

    #region <--- POOL ITEM METHODS --->

    public override void OnDespawn()
    {
        
    }

    public override void OnSpawn()
    {
        RandomizeType();
        IsMatched = false;
        isHorizontalMoving = false;
        isVerticalMoving = false;
    }

    #endregion <--- POOL ITEM METHODS --->

    #region <--- EVENT SYSTEM METHODS --->

    private void OnPointerDown(PointerEventData data)
    {
        if (!GameManager.GameIsRunning) return;
        initialPointerPos = data.position;
    }

    private void OnPointerUp(PointerEventData data)
    {
        finalPointerPos = data.position;
        GemsController.Instance.SwipeGem(this, GetSwipeAngle());
    }

    #endregion <--- EVENT SYSTEM METHODS --->

    #region <--- PRIVATE METHODS --->

    private void SetupPointerEvents()
    {
        EventTrigger.Entry pointerDownEvent =
            new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pointerDownEvent.callback.AddListener((data) => { OnPointerDown((PointerEventData)data); });
        eventTrigger.triggers.Add(pointerDownEvent);

        EventTrigger.Entry pointerUpEvent =
            new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        pointerUpEvent.callback.AddListener((data) => { OnPointerUp((PointerEventData)data); });
        eventTrigger.triggers.Add(pointerUpEvent);
    }

    private float GetSwipeAngle()
    {
        return (Mathf.Abs(finalPointerPos.y - initialPointerPos.y) > swipeDeadZone || Mathf.Abs(finalPointerPos.x - initialPointerPos.x) > swipeDeadZone)
            ? Mathf.Atan2(finalPointerPos.y - initialPointerPos.y, finalPointerPos.x - initialPointerPos.x) * 180 / Mathf.PI
            : 0f;
    }

    private IEnumerator MoveToLastPosition()
    {
        while (Vector2.Distance(transform.position, LastPosition) > 0.1f)
        {
            transform.position = Vector2.Lerp(transform.position, LastPosition, 0.4f);
            yield return null;
        }

        transform.position = LastPosition;
    }

    private IEnumerator MoveToFallPosition(Vector2 fallPosition)
    {
        while (Vector2.Distance(transform.position, fallPosition) > 0.1f)
        {
            transform.position = Vector2.Lerp(transform.position, fallPosition, 0.4f);
            yield return null;
        }

        transform.position = fallPosition;
    }

    #endregion <--- PRIVATE METHODS --->
}
