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
    private Vector3 previewPosition;
    private float lastEatModeExitTime;
    
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
            
            lastEatModeExitTime = Time.time; // Track when we are in Eat Mode
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
        if (Mouse.current != null)
        {
            // 1. Constantly track mouse position
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
            previewPosition.x = Mathf.Clamp(mouseWorldPos.x, -movementBoundary, movementBoundary);

            // 2. Drop on left click
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Prevent dropping if we JUST exited Eat Mode this exact frame
                if (Time.time - lastEatModeExitTime < 0.1f) return;

                bool overUI = false;
                if (EventSystem.current != null)
                {
                    overUI = EventSystem.current.IsPointerOverGameObject();
                }

                if (!overUI)
                {
                    DropFood();
                    return; // Skip visual update this frame since we just destroyed the preview
                }
            }
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
        if (sr != null && sr.sprite != null)
        {
            nextItemUI.sprite = sr.sprite;
            nextItemUI.color = sr.color;
            nextItemUI.preserveAspect = false; // We will manually set the exact pixel size

            // Calculate the exact physical size of the item in the Unity world
            float worldWidth = (sr.sprite.rect.width / sr.sprite.pixelsPerUnit) * prefab.transform.localScale.x;
            float worldHeight = (sr.sprite.rect.height / sr.sprite.pixelsPerUnit) * prefab.transform.localScale.y;

            // Translate that physical size into UI pixels (e.g. 50 UI pixels per 1 meter in game)
            // If the UI icons look too small or too big overall, you can change this 50f multiplier!
            float uiMultiplier = 50f;
            nextItemUI.rectTransform.sizeDelta = new Vector2(worldWidth * uiMultiplier, worldHeight * uiMultiplier);
            
            // Reset local scale just in case the old code changed it
            nextItemUI.rectTransform.localScale = Vector3.one;
        }
    }
}
