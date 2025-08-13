using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using TMPro;

[RequireComponent(typeof(XRSimpleInteractable))]
public class DualPartButton : MonoBehaviour
{
    [Header("Configuración del Botón")]
    public Transform[] movingParts;
    public float pressedZPosition = -0.00012f;
    public float returnSpeed = 25f;

    [Header("Feedback Háptico")]
    public bool hapticFeedback = true;
    [Range(0, 1)] public float hapticIntensity = 0.3f;
    public float hapticDuration = 0.1f;

    [Header("Control del Pistón")]
    public Transform piston;
    public float pistonSpeed = 0.5f;
    public float minPistonHeight = -22.21f;
    public float maxPistonHeight = 0.05f;
    public bool isOnButton = true;

    [Header("Medidor de Volumen")]
    public TextMeshProUGUI volumeText;
    public RectTransform volumeUI;
    public float maxVolume = 5f;
    public float minVolume = 0f;
    public Vector3 uiOffset = new Vector3(0, 0.1f, 0);

    [Header("Medidor de Presión")]
    public TextMeshProUGUI pressureText;
    public float minPressure = 1f; // 1 atm
    public float maxPressure = 15f; // 15 atm
    public Color normalPressureColor = Color.white;
    public Color warningPressureColor = new Color(1f, 0.6f, 0f); // Naranja
    public Color dangerPressureColor = Color.red;
    [Range(0, 1)] public float warningThreshold = 0.7f; // 70% de presión máxima
    [Range(0, 1)] public float dangerThreshold = 0.9f; // 90% de presión máxima
    [Header("Eventos de Cambio de Valores")]
    public System.Action<float, float> OnPVValuesChanged; // Presión, Volumen

    private Vector3[] originalPositions;
    private bool isPressed = false;
    private Vector3 initialUIPosition;
    private float initialPistonY;
    private float currentPressure;

    void Start()
    {
        // 1. Inicializar posiciones de las partes móviles
        originalPositions = new Vector3[movingParts.Length];
        for (int i = 0; i < movingParts.Length; i++)
        {
            originalPositions[i] = movingParts[i].localPosition;
        }

        // 2. Configurar eventos de interacción XR
        XRSimpleInteractable interactable = GetComponent<XRSimpleInteractable>();
        interactable.selectEntered.AddListener(OnButtonPressed);
        interactable.selectExited.AddListener(OnButtonReleased);

        // 3. Forzar posición inicial exacta del pistón
        if (piston != null)
        {
            // Asegurar posición Y inicial exacta
            Vector3 pistonPos = piston.localPosition;
            pistonPos.y = maxPistonHeight; // Posición completamente arriba
            piston.localPosition = pistonPos;

            initialPistonY = piston.localPosition.y;

            if (volumeUI != null)
            {
                initialUIPosition = volumeUI.localPosition;
                SyncVolumeUIWithPiston();
            }
        }

        // 4. Forzar valores iniciales exactos
        ForceInitialValues();
    }

    private void ForceInitialValues()
    {
        // Asegurar volumen exacto (5.000)
        if (volumeText != null)
        {
            volumeText.text = "--- 5.000 m³ ---";
        }

        // Asegurar presión exacta (1.00)
        if (pressureText != null)
        {
            pressureText.text = "   1.00 (atm)";
            pressureText.color = normalPressureColor;
        }

        // Sincronizar valores internos
        if (piston != null)
        {
            currentPressure = minPressure;
        }
    }
    private bool firstFrame = true;
    void Update()
    {
        if (firstFrame)
        {
            firstFrame = false;
            return;
        }
        // Movimiento del botón físico
        AnimateButtonParts();

        // Control del pistón y actualización de UI
        if (isPressed && piston != null)
        {
            ControlPistonMovement();
            UpdateVolumeUI();
            UpdatePressureUI();
        }
    }

    private void AnimateButtonParts()
    {
        for (int i = 0; i < movingParts.Length; i++)
        {
            Vector3 targetPos = isPressed ?
                new Vector3(originalPositions[i].x, originalPositions[i].y, pressedZPosition) :
                originalPositions[i];

            movingParts[i].localPosition = Vector3.Lerp(
                movingParts[i].localPosition,
                targetPos,
                returnSpeed * Time.deltaTime
            );
        }
    }

    private void OnButtonPressed(SelectEnterEventArgs args)
    {
        isPressed = true;

        // Mover partes al instante al presionar
        for (int i = 0; i < movingParts.Length; i++)
        {
            Vector3 newPos = movingParts[i].localPosition;
            newPos.z = pressedZPosition;
            movingParts[i].localPosition = newPos;
        }

        // Feedback háptico
        if (hapticFeedback && args.interactorObject is XRBaseInputInteractor inputInteractor)
        {
            if (inputInteractor.TryGetComponent(out XRController controller))
            {
                controller.SendHapticImpulse(hapticIntensity, hapticDuration);
            }
        }
    }

    private void OnButtonReleased(SelectExitEventArgs args)
    {
        isPressed = false;
    }

    private void ControlPistonMovement()
    {
        float direction = isOnButton ? -1f : 1f;
        Vector3 newPos = piston.localPosition;

        newPos.y = Mathf.Clamp(
            newPos.y + (direction * pistonSpeed * Time.deltaTime),
            minPistonHeight,
            maxPistonHeight
        );

        piston.localPosition = newPos;
        SyncVolumeUIWithPiston();
    }

    private void SyncVolumeUIWithPiston()
    {
        if (volumeUI != null)
        {
            Vector3 volumePos = volumeUI.localPosition;
            volumePos.y = initialUIPosition.y + (piston.localPosition.y * piston.parent.localScale.y);
            volumeUI.localPosition = volumePos;
        }
    }
    private void UpdateVolumeUI()
    {
        if (volumeText == null || piston == null) return;

        // Usar comparación exacta para posición inicial
        if (Mathf.Approximately(piston.localPosition.y, maxPistonHeight))
        {
            volumeText.text = "--- 5.000 m³ ---";
            return;
        }

        float normalizedPosition = Mathf.InverseLerp(minPistonHeight, maxPistonHeight, piston.localPosition.y);
        float currentVolume = Mathf.Lerp(minVolume, maxVolume, normalizedPosition);
        volumeText.text = $"--- {currentVolume.ToString("0.000")} m³ ---";
    }

    // Modifica el método UpdatePressureUI para que notifique cambios
    private void UpdatePressureUI()
    {
        if (pressureText == null || piston == null) return;

        float normalizedPosition = Mathf.InverseLerp(maxPistonHeight, minPistonHeight, piston.localPosition.y);
        currentPressure = Mathf.Lerp(minPressure, maxPressure, normalizedPosition);

        pressureText.text = $"   {currentPressure.ToString("0.00")} (atm)";

        if (normalizedPosition >= dangerThreshold)
            pressureText.color = dangerPressureColor;
        else if (normalizedPosition >= warningThreshold)
            pressureText.color = warningPressureColor;
        else
            pressureText.color = normalPressureColor;

        // Notificar el cambio de valores
        OnPVValuesChanged?.Invoke(currentPressure, GetCurrentVolume());
    }

    // Asegúrate que este método sea público
    public float GetCurrentVolume()
    {
        float normalizedPosition = Mathf.InverseLerp(minPistonHeight, maxPistonHeight, piston.localPosition.y);
        return Mathf.Lerp(minVolume, maxVolume, normalizedPosition);
    }

    public float GetCurrentPressure()
    {
        return currentPressure;
    }
}