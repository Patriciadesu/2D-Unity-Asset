using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UIElements;
using static NodeHelper.NodeUIHelpers;

[System.Serializable]
public class ShootState : BossState
{
    private GameObject projectilePrefab;   // Prefab of projectile
    private Transform shootPoint;          // Spawn position
    private float projectileSpeed = 15f;
    private float shootCooldown = 2;
    private TimelineAsset timelinePlayable; // Optional animation timeline
    private PlayableDirector director;
    private bool hasShot;
    private bool endedOnce;

    public ShootState(Boss bossInstance) : base("Shoot", bossInstance) { }

    public override void Enter()
    {
        base.Enter();

        // Initialize sentinel the first time
        if (boss.shootInterval == -999) boss.shootInterval = shootCooldown;

        // If interval <= 0, we can shoot now; otherwise we already shot and are cooling down
        hasShot = boss.shootInterval > 0f;
        endedOnce = false;

        if (animator != null)
            animator.SetTrigger("Shoot");

        // Optional timeline (anim)
        director = boss.GetComponent<PlayableDirector>();
        if (director == null) director = boss.gameObject.AddComponent<PlayableDirector>();
        if (timelinePlayable != null)
        {
            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.None;
            director.playableAsset = timelinePlayable;
            director.RebuildGraph();
            director.Evaluate();
            director.Play();
        }
    }
    public override void Update()
    {
        base.Update();

        if (!hasShot && boss.shootInterval <= 0)
        {
            hasShot = true;
            boss.shootInterval = shootCooldown;

            ShootProjectile();

            // Start ticking cooldown; at the end allow next shot on next entry
            boss.StartCoroutine(boss.ShootCooldown(() => hasShot = false));

            OnShootEnd(); // finish this state immediately after one shot
        }
        else
        {
            // We are still cooling down on this entry -> finish state without spamming events
            OnShootEnd();
        }
    }

    private void ShootProjectile()
    {
        Transform player = Player.Instance != null ? Player.Instance.transform : null;
        if (player == null) return;

        Vector3 shootPos = shootPoint ? shootPoint.position : boss.transform.position + (boss.transform.forward * 2);
        Vector3 dir = (player.position - shootPos).normalized;

        GameObject proj;
        if (projectilePrefab)
        {
            proj = GameObject.Instantiate(projectilePrefab, shootPos, Quaternion.LookRotation(dir));
        }
        else
        {
            proj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            proj.transform.position = shootPos;
            proj.transform.rotation = Quaternion.LookRotation(dir);
            proj.AddComponent<Rigidbody>();
        }

        if (proj.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.AddForce(dir * projectileSpeed, ForceMode.Impulse);
        }
    }

    private void OnShootEnd()
    {
        if (endedOnce) return;
        endedOnce = true;

        isFinished = true;
        boss.onAttackEnd?.Invoke();
        stage = StateStage.Exit; // <-- important: stop updating this state
    }

    public override void Exit()
    {
        if (director != null && director.state == PlayState.Playing)
            director.Stop();

        base.Exit();
    }

    public override void BuildInspectorUI(VisualElement container)
    {
        base.BuildInspectorUI(container);
        container.Add(GameObjectField("Bullet Prefab", () => this.projectilePrefab, v => this.projectilePrefab = v));
        container.Add(TransformField("Shooting Point", () => this.shootPoint, v => this.shootPoint = v));
        container.Add(FloatField("Bullet Speed", () => this.projectileSpeed, v => this.projectileSpeed = v));
        container.Add(FloatField("Cooldown Per Shot", () => this.shootCooldown, v => this.shootCooldown = v));
        container.Add(TimelineField("Shooting Timeline", () => this.timelinePlayable, v => this.timelinePlayable = v));
    }

}



public partial class Boss : MonoBehaviour
{
    public float shootInterval = -999;

    public IEnumerator ShootCooldown(UnityAction callback)
    {
        while (shootInterval > 0)
        {
            shootInterval -= 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
        callback.Invoke();
    }
}