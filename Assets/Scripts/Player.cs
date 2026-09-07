using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour {
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float cameraHeight = 1.0f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField, Min(1f)] private float crosshairSize = 6f;

    private CharacterController characterController;
    private float pitch;
    private Texture2D crosshairTexture;

    void Start() {
        characterController = GetComponent<CharacterController>();

        if (cameraTransform == null) {
            Camera mainCamera = Camera.main;
            if (mainCamera != null) {
                cameraTransform = mainCamera.transform;
            }
        }

        if (cameraTransform != null) {
            cameraTransform.SetParent(transform);
            cameraTransform.localPosition = new Vector3(0f, cameraHeight, 0f);
            cameraTransform.localRotation = Quaternion.identity;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        CreateCrosshair();
    }

    private void CreateCrosshair() {
        GameObject canvasObject = new GameObject("Crosshair Canvas", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        const int textureSize = 32;
        crosshairTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        crosshairTexture.name = "Crosshair Dot";
        crosshairTexture.wrapMode = TextureWrapMode.Clamp;
        crosshairTexture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[textureSize * textureSize];
        Vector2 center = Vector2.one * (textureSize * 0.5f);
        float radius = textureSize * 0.5f - 1f;
        for (int y = 0; y < textureSize; y++) {
            for (int x = 0; x < textureSize; x++) {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                pixels[y * textureSize + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(radius - distance));
            }
        }
        crosshairTexture.SetPixels(pixels);
        crosshairTexture.Apply(false, true);

        GameObject dotObject = new GameObject("Crosshair Dot", typeof(RectTransform), typeof(RawImage));
        dotObject.transform.SetParent(canvasObject.transform, false);
        RawImage dot = dotObject.GetComponent<RawImage>();
        dot.texture = crosshairTexture;
        dot.color = Color.white;
        dot.raycastTarget = false;
        RectTransform rect = dot.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.one * crosshairSize;
    }

    private void OnDestroy() {
        if (crosshairTexture != null) {
            Destroy(crosshairTexture);
        }
    }

    void Update() {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        characterController.Move(move * moveSpeed * Time.deltaTime);

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(0f, mouseX, 0f);

        if (cameraTransform != null) {
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
