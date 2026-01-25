using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    public int damage = 1;

    // Срабатывает, когда враг касается кого-то (физическое столкновение)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Проверяем, что столкнулись именно с игроком
        if (collision.gameObject.CompareTag("Player"))
        {
            // Пытаемся найти компонент здоровья на игроке
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }
    }
    
    // Опционально: если вы хотите, чтобы урон наносился ПОСТОЯННО, пока враг прижат к игроку
    private void OnCollisionStay2D(Collision2D collision)
    {
         // Здесь можно добавить таймер, чтобы урон шел не каждый кадр, а раз в 0.5 сек
         // Но для начала хватит и OnCollisionEnter2D (урон только при первом касании)
    }
}
