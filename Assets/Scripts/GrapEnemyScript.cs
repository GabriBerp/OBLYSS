using UnityEngine;

public class GrapEnemyScript : MonoBehaviour
{
    // Define os padrões de movimento possíveis
    public enum MovementPattern
    {
        LeftRight,       // Esquerda e Direita (Eixo X)
        UpDown,          // Cima e Baixo (Eixo Y)
        ForwardBackward  // Frente e Trás (Eixo Z)
    }

    [Header("Configurações de Movimento")]
    [Tooltip("Selecione o padrão de movimento deste inimigo.")]
    [SerializeField] private MovementPattern selectedPattern = MovementPattern.LeftRight;

    [Tooltip("Distância máxima que o inimigo se moverá a partir do ponto inicial.")]
    [SerializeField] private float distance = 3f;

    [Tooltip("Velocidade do movimento.")]
    [SerializeField] private float speed = 2f;

    private Vector3 startPosition;

    void Start()
    {
        // Guarda a posição inicial onde o inimigo foi colocado na cena
        startPosition = transform.position;
    }

    void Update()
    {
        MoveEnemy();
    }

    private void MoveEnemy()
    {
        // Mathf.Sin cria uma oscilação suave de -1 a 1 ao longo do tempo
        float offset = Mathf.Sin(Time.time * speed) * distance;

        Vector3 newPosition = startPosition;

        // Aplica o movimento apenas no eixo escolhido
        switch (selectedPattern)
        {
            case MovementPattern.LeftRight:
                newPosition.x += offset;
                break;

            case MovementPattern.UpDown:
                newPosition.y += offset;
                break;

            case MovementPattern.ForwardBackward:
                newPosition.z += offset;
                break;
        }

        transform.position = newPosition;
    }

    // BOA PRÁTICA: Desenha uma linha visual no editor da Unity mostrando o caminho do inimigo
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = Application.isPlaying ? startPosition : transform.position;

        switch (selectedPattern)
        {
            case MovementPattern.LeftRight:
                Gizmos.DrawLine(center - Vector3.right * distance, center + Vector3.right * distance);
                break;
            case MovementPattern.UpDown:
                Gizmos.DrawLine(center - Vector3.up * distance, center + Vector3.up * distance);
                break;
            case MovementPattern.ForwardBackward:
                Gizmos.DrawLine(center - Vector3.forward * distance, center + Vector3.forward * distance);
                break;
        }
    }
}
