using UnityEngine;
using UnityEngine.InputSystem;

public class EatController : MonoBehaviour
{
    void Update()
    {
        // Only active if Eat Mode is turned on
        if (GameManager.Instance == null || !GameManager.Instance.IsEatMode || GameManager.Instance.IsGameOver)
            return;

        bool isClicking = false;
        Vector2 screenPos = Vector2.zero;

        // Mouse click
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            isClicking = true;
            screenPos = Mouse.current.position.ReadValue();
        }
        // Touch input
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            isClicking = true;
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (isClicking)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(screenPos);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                FoodItem food = hit.collider.GetComponent<FoodItem>();
                if (food != null)
                {
                    if (GameManager.Instance.TryConsumeItem(food))
                    {
                        food.OnEaten();
                    }
                    else
                    {
                        Debug.Log("Not enough score to eat this item!");
                    }
                }
            }
        }
    }
}
