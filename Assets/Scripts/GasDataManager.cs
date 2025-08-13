using UnityEngine;
using TMPro;

public class GasDataManager : MonoBehaviour
{
    [Header("Componentes de Control")]
    public DualPartButton botonSubir;
    public DualPartButton botonBajar;
    public ParticleControllerFire temperatureController;

    [Header("UI de Reporte")]
    public TextMeshProUGUI textoReporte;

    [Header("Configuración de Colores")]
    public Color colorNormal = Color.black;
    public Color colorAlerta = new Color(1f, 0.5f, 0f); // Naranja intenso
    public Color colorPeligro = Color.red;
    [Range(0.5f, 0.9f)] public float umbralAlerta = 0.7f;
    [Range(0.8f, 1f)] public float umbralPeligro = 0.9f;

    private void Update()
    {
        ActualizarReporte();
    }

    private void ActualizarReporte()
    {
        if (textoReporte == null) return;

        float presion = botonSubir.GetCurrentPressure();
        float volumen = botonSubir.GetCurrentVolume();
        float temperatura = temperatureController.GetCurrentTemperature();

        // Configurar autoajuste de texto
        textoReporte.enableAutoSizing = true;
        textoReporte.fontSizeMin = 1;
        textoReporte.fontSizeMax = 20;
        textoReporte.fontStyle = FontStyles.Bold;

        // Determinar colores para cada valor
        Color colorPresion = ObtenerColorSegunValor(presion, botonSubir.minPressure, botonSubir.maxPressure);
        Color colorVolumen = ObtenerColorSegunValor(volumen, botonSubir.minVolume, botonSubir.maxVolume, true); // true para volumen
        Color colorTemp = ObtenerColorSegunValor(temperatura, temperatureController.minTemperature, temperatureController.maxTemperature);

        // Texto formateado con rich text
        textoReporte.text = "<color=#000000><b>Datos:</b></color>\n" +
                           $"<color=#{ColorUtility.ToHtmlStringRGB(colorPresion)}><b>Presión = {presion:0.00} (atm)</b></color>\n" +
                           $"<color=#{ColorUtility.ToHtmlStringRGB(colorVolumen)}><b>Volumen = {volumen:0.000} (m³)</b></color>\n" +
                           $"<color=#{ColorUtility.ToHtmlStringRGB(colorTemp)}><b>Temperatura = {temperatura:0} (°K)</b></color>";
    }

    private Color ObtenerColorSegunValor(float valor, float min, float max, bool esVolumen = false)
    {
        float normalizado = Mathf.InverseLerp(min, max, valor);

        // Invertir la escala solo para volumen
        if (esVolumen)
        {
            normalizado = 1 - normalizado;
        }

        if (normalizado >= umbralPeligro)
            return colorPeligro;
        else if (normalizado >= umbralAlerta)
            return colorAlerta;

        return colorNormal;
    }
}