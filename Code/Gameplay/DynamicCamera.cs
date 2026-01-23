using UnityEngine;

public class DynamicCamera : MonoBehaviour
{
    [Header("Targets")]
    public Transform player;  // Сюда перетащить игрока
    public Transform reticle; // Сюда перетащить объект Reticle

    [Header("Settings")]
    [Range(0f, 1f)]
    public float bias = 0.25f; // Насколько сильно камера тянется к курсору (0.25 = 25% пути)
    public float smoothSpeed = 5f; // Плавность движения камеры

    void FixedUpdate()
    {
        if (player == null || reticle == null) return;

        // 1. Вычисляем желаемую точку камеры
        // Это линия между Игроком и Прицелом. 
        // Если bias = 0.5, камера будет ровно посередине. 
        // Для топ-даун шутеров обычно берут 0.2 - 0.3 (ближе к игроку).
        Vector3 targetPos = Vector3.Lerp(player.position, reticle.position, bias);

        // 2. Сохраняем Z камеры (обычно -10), иначе ничего не увидим
        targetPos.z = transform.position.z;

        // 3. Плавно двигаем камеру к этой точке (Lerp для плавности)
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.fixedDeltaTime);
    }
}
