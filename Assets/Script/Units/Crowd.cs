using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crowd : MonoBehaviour
{
    [SerializeField] private RectTransform selectionBox;                     // All this logic will have to move into the player
    [SerializeField] private LayerMask UnitLayer;                            //
    [SerializeField] private List<Unit> allUnits = new List<Unit>();         //
    private Vector2 startMousePos;                                           //

    private HashSet<Unit> selectedUnits = new HashSet<Unit>();
    private float slowestTroopSpeed = float.MaxValue;



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        HandleSelectionInputs();
        HandleMoveInput();
    }

    private void HandleSelectionInputs()
    {
        if (Input.GetMouseButtonDown(0))
        {
            selectionBox.sizeDelta = Vector2.zero;
            selectionBox.gameObject.SetActive(true);
            startMousePos = Input.mousePosition;
        }

        else if (Input.GetMouseButton(0))
        {
            ResizeSelectionBox();
        }

        else if (Input.GetMouseButtonUp(0))
        {
            selectionBox.sizeDelta = Vector2.zero;
            selectionBox.gameObject.SetActive(false);
            slowestTroopSpeed = FindSlowestUnitSpeed();
            SetCrowdSpeed(slowestTroopSpeed);
        }
    }

    private void HandleMoveInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out var hit))
            {
                foreach (var unit in selectedUnits)
                {
                    unit.SetDestination(hit.point);
                }
            }
        }
    }

    private void ResizeSelectionBox()
    {
        float width = Input.mousePosition.x - startMousePos.x;
        float height = Input.mousePosition.y - startMousePos.y;

        selectionBox.anchoredPosition = startMousePos + new Vector2(width / 2, height / 2);
        selectionBox.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));

        Bounds bounds = new Bounds(selectionBox.anchoredPosition, selectionBox.sizeDelta);

        for (int i = 0; i < allUnits.Count; i++)
        {
            if (UnitIsInSelectionBox(Camera.main.WorldToScreenPoint(allUnits[i].transform.position), bounds))
            {
                selectedUnits.Add(allUnits[i]);
                allUnits[i].Select();
            }

            else
            {
                selectedUnits.Remove(allUnits[i]);
                allUnits[i].DeSelect();
            }
        }
    }

    private bool UnitIsInSelectionBox(Vector3 position, Bounds bounds)
    {
        return position.x > bounds.min.x && position.x < bounds.max.x &&
               position.y > bounds.min.y && position.y < bounds.max.y;
    }

    private float FindSlowestUnitSpeed()
    {
        float slowestSoFar = float.MaxValue;

        foreach (var unit in selectedUnits)
        {
            if (unit.GetMaxSpeed() < slowestSoFar)
                slowestSoFar = unit.GetMaxSpeed();
        }

        return slowestSoFar;
    }

    private void SetCrowdSpeed(float speed)
    {
        foreach (var unit in selectedUnits)
        {
            unit.SetUnitSpeed(speed);
        }
    }
}
