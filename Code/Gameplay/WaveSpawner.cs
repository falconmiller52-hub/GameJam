using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Settings")]
    public GameObject enemyPrefab;  // Кого спавним
    public Transform[] spawnPoints; // Откуда спавним (массив точек)
    
    public float timeBetweenSpawns = 2f; // Задержка между врагами
    public bool canSpawn = true;    // Рубильник для остановки спавна

    void Start()
    {
        // Запускаем бесконечный процесс спавна
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        // Ждем немного перед первым врагом, чтобы игрок осмотрелся
        yield return new WaitForSeconds(1f);

        while (canSpawn)
        {
            SpawnEnemy();
            // Ждем указанное время перед следующим
            yield return new WaitForSeconds(timeBetweenSpawns);
        }
    }

    void SpawnEnemy()
    {
        // 1. Проверка на ошибки
        if (enemyPrefab == null)
        {
            Debug.LogError("Не назначен Enemy Prefab в спавнере!");
            return;
        }
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("Нет точек спавна!");
            return;
        }

        // 2. Выбираем случайную точку из массива
        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[randomIndex];

        // 3. Создаем врага (Instantiate)
        // position: позиция точки
        // rotation: Quaternion.identity (стандартный поворот, без вращения)
        Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
    }
}
