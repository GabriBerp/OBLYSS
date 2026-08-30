using UnityEngine;
using Kino;

[RequireComponent(typeof(DigitalGlitch))]
public class DashGlitchEffect : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Referência ao PlayerScript do jogador.")]
    [SerializeField] private PlayerScript player;

    [Header("Configuração do Glitch")]
    [Tooltip("Intensidade máxima do glitch durante o dash (0 a 1).")]
    [SerializeField, Range(0f, 1f)] private float maxIntensity = 0.6f;

    [Tooltip("Velocidade com que a intensidade sobe/desce (maior = mais rápido).")]
    [SerializeField] private float smoothSpeed = 12f;

    [Tooltip("Se true, a intensidade some suavemente depois do dash em vez de cortar na hora.")]
    [SerializeField] private bool fadeOutAfterDash = true;

    [Tooltip("Duração do fade-out após o dash terminar (em segundos).")]
    [SerializeField] private float fadeOutDuration = 0.15f;

    private DigitalGlitch digitalGlitch;
    private float fadeOutTimer;
    private bool wasDashing;

    void Awake()
    {
        digitalGlitch = GetComponent<DigitalGlitch>();
    }

    void Update()
    {
        if (player == null || digitalGlitch == null)
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

        digitalGlitch.intensity = Mathf.Lerp(
            digitalGlitch.intensity,
            targetIntensity,
            smoothSpeed * Time.deltaTime
        );

        wasDashing = isDashing;
    }
}