using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Projectile.cs → Q ve W büyü prefablarına ekle
///
/// YENİ: Enemy'e çarptığında 3-4 birim geri knockback (NavMeshAgent uyumlu)
///
/// COLLIDER: Sphere Collider → Is Trigger: AÇIK
/// RIGIDBODY: Use Gravity: KAPALI, Is Kinematic: AÇIK
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("Hareket")]
    public float speed = 20f;
    public float distance = 30f;

    [Header("Hasar")]
    public int damage = 50;

    [Header("Knockback")]
    public float knockbackDistance = 4.5f;
    public float knockbackDuration = 0.25f;

    private float traveledDistance = 0f;

    void Update()
    {
        float step = speed * Time.deltaTime;
        transform.Translate(Vector3.forward * step);
        traveledDistance += step;
        if (traveledDistance >= distance) Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Untagged")) return;

        if (other.CompareTag("Enemy"))
        {
            EnemyHealth eh = other.GetComponent<EnemyHealth>();
            eh?.TakeDamage(damage);

            NavMeshAgent agent = other.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                Vector3 dir = (other.transform.position - transform.position).normalized;
                dir.y = 0f;
                other.GetComponent<MonoBehaviour>()?.StartCoroutine(
                    KnockbackAgent(agent, dir, eh));
            }
        }

        Destroy(gameObject);
    }

    IEnumerator KnockbackAgent(NavMeshAgent agent, Vector3 direction, EnemyHealth eh)
    {
        if (agent == null) yield break;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        float elapsed = 0f;
        float spd = knockbackDistance / knockbackDuration;

        while (elapsed < knockbackDuration)
        {
            if (agent == null) yield break;
            float t = 1f - (elapsed / knockbackDuration); // yavaşlayan knockback
            agent.Move(direction * spd * t * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (agent != null && eh != null && !eh.isDead)
            agent.isStopped = false;
    }
}