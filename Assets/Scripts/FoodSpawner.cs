using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class FoodSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject[] foodPrefabs;
    public float spawnHeight = 5f;
    public float movementBoundary = 6f; // Max X distance from center for Landscape

    [Header("Cooldown Settings")]
    public float dropCooldown = 1f;
    private float lastDropTime;
    
    [Header("UI Links")]
    public UnityEngine.UI.Image nextItemUI; // Assign a UI Image here to show the next item

    private GameObject currentPreview;
    private int currentFoodIndex;
    private int nextFoodIndex;

    // Input state
    private bool isDragging = false;
    private Vector3 previewPosition;
    
    private LineRenderer dropLine;

    void Start()
    {
        // Setup drop guide line
        dropLine = gameObject.AddComponent<LineRenderer>();
        dropLine.positionCount = 2;
        dropLine.startWidth = 0.05f;
        dropLine.endWidth = 0.05f;
        dropLine.material = new Material(Shader.Find("Sprites/Default"));
        dropLine.startColor = new Color(1, 1, 1, 0.5f);
        dropLine.endColor = new Color(1, 1, 1, 0f);
        dropLine.sortingLayerName = "FrontWall"; // Render in front of everything
        dropLine.sortingOrder = 100;

        if (foodPrefabs != null && foodPrefabs.Length > 0)
        {
            currentFoodIndex = Random.Range(0, foodPrefabs.Length);
            nextFoodIndex = Random.Range(0, foodPrefabs.Length);
            UpdateNextItemUI();
            SpawnPreview();
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && (GameManager.Instance.IsEatMode || GameManager.Instance.IsGameOver))
        {
            // Hide preview during eat mode or game over
            if (currentPreview != null && currentPreview.activeSelf)
                currentPreview.SetActive(false);
            if (dropLine != null) dropLine.enabled = false;
            return;
        }

        if (Time.time < lastDropTime + dropCooldown) 
        {
            if (dropLine != null) dropLine.enabled = false;
            return;
        }

        // Ensure preview is active
        if (currentPreview != null && !currentPreview.activeSelf)
            currentPreview.SetActive(true);

        if (currentPreview == null) SpawnPreview();

        HandleInput();
    }

    void HandleInput()
    {
        // 1. Switch Style Input (L/R to move, Jump/Space/Submit to drop)
        float horizontalAxis = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontalAxis = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontalAxis = 1f;
        }
        if (Gamepad.current != null)
        {
            horizontalAxis = Gamepad.current.leftStick.x.ReadValue();
        }

        if (Mathf.Abs(horizontalAxis) > 0.1f)
        {
            float moveSpeed = 8f * Time.deltaTime;
            previewPosition.x = Mathf.Clamp(previewPosition.x + horizontalAxis * moveSpeed, -movementBoundary, movementBoundary);
        }

        bool dropPressed = (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)) ||
                           (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

        if (dropPressed)
        {
            DropFood();
            return;
        }

        // 2. Mobile Style Input (Drag to move, Release to drop)
        bool pointerDown = false;
        bool pointerHeld = false;
        bool pointerUp = false;
        Vector2 pointerPos = Vector2.zero;

        if (Mouse.current != null)
        {
            pointerDown = Mouse.current.leftButton.wasPressedThisFrame;
            pointerHeld = Mouse.current.leftButton.isPressed;
            pointerUp = Mouse.current.leftButton.wasReleasedThisFrame;
            pointerPos = Mouse.current.position.ReadValue();
        }
        else if (Touchscreen.current != null)
        {
            pointerDown = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            pointerHeld = Touchscreen.current.primaryTouch.press.isPressed;
            pointerUp = Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
            pointerPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (pointerDown)
        {
            bool overUI = false;
            if (EventSystem.current != null)
            {
                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                {
                    overUI = EventSystem.current.IsPointerOverGameObject(Touchscreen.current.primaryTouch.touchId.ReadValue());
                }
                else
                {
                    overUI = EventSystem.current.IsPointerOverGameObject();
                }
            }

            if (!overUI)
            {
                isDragging = true;
            }
        }

        if (pointerHeld && isDragging)
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(pointerPos);
            previewPosition.x = Mathf.Clamp(mouseWorldPos.x, -movementBoundary, movementBoundary);
        }

        if (pointerUp && isDragging)
        {
            isDragging = false;
            DropFood();
            return;
        }

        // Update preview visual position and guide line
        if (currentPreview != null)
        {
            currentPreview.transform.position = previewPosition;
            if (dropLine != null)
            {
                dropLine.enabled = true;
                dropLine.SetPosition(0, previewPosition);
                
                // Raycast down to find where the piece will land
                RaycastHit2D hit = Physics2D.Raycast(previewPosition, Vector2.down, 20f);
                if (hit.collider != null)
                {
                    dropLine.SetPosition(1, hit.point);
                }
                else
                {
                    // Fallback to bottom of the screen if nothing is hit
                    dropLine.SetPosition(1, new Vector3(previewPosition.x, -10f, previewPosition.z));
                }
            }
        }
        else if (dropLine != null)
        {
            dropLine.enabled = false;
        }
    }

    void SpawnPreview()
    {
        if (foodPrefabs == null || foodPrefabs.Length == 0) return;

        GameObject prefab = foodPrefabs[currentFoodIndex];
        previewPosition = new Vector3(0, spawnHeight, 0);
        
        currentPreview = Instantiate(prefab, previewPosition, Quaternion.identity);
        
        // Disable physics while it's a preview
        Rigidbody2D rb = currentPreview.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            // Ensure no rotation on spawn
            rb.transform.rotation = Quaternion.identity;
        }
        
        Collider2D col = currentPreview.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        
        // Make it translucent
        SpriteRenderer sr = currentPreview.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1.0f; // 100% opaque as requested
            sr.color = c;
        }
    }

    void DropFood()
    {
        if (currentPreview == null) return;

        // Enable physics
        Rigidbody2D rb = currentPreview.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f; // Ensure gravity is active
            
            // Removed the artificial random rotation to allow perfectly straight drops
        }

        Collider2D col = currentPreview.GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        // Restore opacity
        SpriteRenderer sr = currentPreview.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }

        // Add FoodItem script if missing
        if (currentPreview.GetComponent<FoodItem>() == null)
        {
            currentPreview.AddComponent<FoodItem>();
        }

        FoodItem foodItem = currentPreview.GetComponent<FoodItem>();
        if (foodItem != null)
        {
            foodItem.OnDropped();
        }

        currentPreview = null;
        lastDropTime = Time.time;
        if (dropLine != null) dropLine.enabled = false;
        
        currentFoodIndex = nextFoodIndex; // Shift the queue
        nextFoodIndex = Random.Range(0, foodPrefabs.Length); // Pick a new next item
        UpdateNextItemUI();
    }

    void UpdateNextItemUI()
    {
        if (foodPrefabs == null || foodPrefabs.Length == 0 || nextItemUI == null) return;
        
        GameObject prefab = foodPrefabs[nextFoodIndex];
        SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            nextItemUI.sprite = sr.sprite;
            nextItemUI.color = sr.color;
            nextItemUI.preserveAspect = true;

            // Match the visual scale proportions so a flat Tamagoyaki doesn't look like a square
            Vector3 prefabScale = prefab.transform.localScale;
            float maxAxis = Mathf.Max(prefabScale.x, prefabScale.y);
            if (maxAxis > 0.01f)
            {
                nextItemUI.rectTransform.localScale = new Vector3(prefabScale.x / maxAxis, prefabScale.y / maxAxis, 1f);
            }
        }
    }
}
