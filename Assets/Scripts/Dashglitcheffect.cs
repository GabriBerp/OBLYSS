using UnityEngine;
using Kino;

// Anexe este script no mesmo GameObject que tem o componente AnalogGlitch
// (normalmente a câmera do jogador).
[RequireComponent(typeof(AnalogGlitch))]
public class DashGlitchEffect : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Referência ao PlayerScript do jogador.")]
    [SerializeField] private PlayerScript player;

    [Header("Configuração do Glitch")]
    [Tooltip("Intensidade base do glitch durante o dash (0 a 1). Cada campo abaixo usa uma fração dela.")]
    [SerializeField, Range(0f, 1f)] private float maxIntensity = 0.6f;

    [Tooltip("Velocidade com que a intensidade sobe/desce (maior = mais rápido).")]
    [SerializeField] private float smoothSpeed = 12f;

    [Header("Peso de cada efeito (multiplicadores sobre a intensidade base)")]
    [Tooltip("Deslocamento horizontal suave. É o efeito principal do dash.")]
    [SerializeField, Range(0f, 1f)] private float horizontalShakeWeight = 0.3f;

    [Tooltip("Tremor/corte nas linhas horizontais. Use um valor baixo para só dar textura.")]
    [SerializeField, Range(0f, 1f)] private float scanLineJitterWeight = 0.8f;

    [Tooltip("Separação cromática (RGB desalinhado). Use um valor baixo, fica forte rápido.")]
    [SerializeField, Range(0f, 1f)] private float colorDriftWeight = 0.5f;

    [Tooltip("Se true, a intensidade some suavemente depois do dash em vez de cortar na hora.")]
    [SerializeField] private bool fadeOutAfterDash = true;

    [Tooltip("Duração do fade-out após o dash terminar (em segundos).")]
    [SerializeField] private float fadeOutDuration = 0.15f;

    private AnalogGlitch analogGlitch;
    private float fadeOutTimer;
    private bool wasDashing;

    void Awake()
    {
        analogGlitch = GetComponent<AnalogGlitch>();
    }

    void Update()
    {
        if (player == null || analogGlitch == null)
            return;

        bool isDashing = player.IsDashing;

        // Detecta o instante em que o dash termina, para iniciar o fade-out
        if (wasDashing && !isDashing)
        {
            fadeOutTimer = fadeOutDuration;
        }

        float targetIntensity;

        if (isDashing)
        {
            // Intensidade acompanha o progresso do dash: começa forte e some conforme o dash avança.
            // Se preferir intensidade constante durante todo o dash, troque por: targetIntensity = maxIntensity;
            targetIntensity = maxIntensity * (1f - player.DashProgress01 * 0.5f);
        }
        else if (fadeOutAfterDash && fadeOutTimer > 0f)
        {
            fadeOutTimer -= Time.deltaTime;
            targetIntensity = 0f;
        }
        else
        {
            targetIntensity = 0f;
        }

        float t = smoothSpeed * Time.deltaTime;

        analogGlitch.horizontalShake = Mathf.Lerp(
            analogGlitch.horizontalShake,
            targetIntensity * horizontalShakeWeight,
            t
        );

        analogGlitch.scanLineJitter = Mathf.Lerp(
            analogGlitch.scanLineJitter,
            targetIntensity * scanLineJitterWeight,
            t
        );

        analogGlitch.colorDrift = Mathf.Lerp(
            analogGlitch.colorDrift,
            targetIntensity * colorDriftWeight,
            t
        );

        wasDashing = isDashing;
    }
}