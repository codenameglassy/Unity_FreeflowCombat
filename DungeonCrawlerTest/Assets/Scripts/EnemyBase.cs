using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using FirstGearGames.SmoothCameraShaker;
using UnityEngine.AI;

public class EnemyBase : MonoBehaviour
{
    [Header("Components")]
    private NavMeshAgent agent;
    public Rigidbody rb;
    private Transform player;
    [SerializeField] private GameObject hitVfx;
    [SerializeField] private GameObject activeTargetObject;
    private Animator animator;
    private float waitingTimer;

    [Header("State")]
    public EnemyState enemyState;
    bool isKnockBack = false;
    bool isAttacking = false;
    public enum EnemyState
    {
        idle,
        move,
        readyToTakeDamage,
        knockback,
        attack,
        death
    }
    [Header("Damage")]
    private float timeSinceLastDamage;
    private bool isTakingDamage;
    private Coroutine damageCoroutine;
    public ShakeData damageShakeData;

    [Header("Attack")]
    public LayerMask whatIsPlayer;
    public float range;
    public float attackRange;
    public Transform attackPos;
    public GameObject slashVfx;

    [Header("Health")]
    public int maxHealth;
    private int currentHealth;

    // Start is called before the first frame update
    void Start()
    {
        float speed = Random.Range(2, 6);
        
        //rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        ActiveTarget(false);
        agent.speed = speed;
        timeSinceLastDamage = 0f;
        isTakingDamage = false;

        player = GameControl.instance.playerControl.transform;
        currentHealth = maxHealth;
        StartCoroutine(Enum_Initilaize_Enemy());

        TargetDetectionControl.instance.allTargetsInScene.Add(transform);
    }

    IEnumerator Enum_Initilaize_Enemy()
    {
        enemyState = EnemyState.idle;
        yield return new WaitForSeconds(2f);
        enemyState = EnemyState.move;
    }

    private void Update()
    {
        if (isTakingDamage)
        {
            isTakingDamage = false;
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
            }
            damageCoroutine = StartCoroutine(CheckDamageTime());
        }
    }

    private IEnumerator CheckDamageTime()
    {
        yield return new WaitForSeconds(2f);

        if (!isTakingDamage)
        {
            MoveState();
        }
    }

    private void FixedUpdate()
    {
        waitingTimer -= Time.deltaTime;
        if (waitingTimer <= 0)
        {
            float waitingTimerMax = .2f; // 200ms
            waitingTimer = waitingTimerMax;
            HandleEnemyState();
            CheckPlayerInRange();
        }

    }

    public void Gameover()
    {
        enemyState = EnemyState.idle;
    }

    public void PerformAttack() //animation event
    {
        Collider[] hit = Physics.OverlapSphere(attackPos.position, attackRange, whatIsPlayer);
        if(hit.Length > 0)
        {
            hit[0].GetComponent<PlayerControl>().TakeDamage(10);
        }
    }
    public void TakeDamage(int damageTaken)
    {
       
        if (agent.enabled)
        {
            agent.ResetPath();
        }
       
        enemyState = EnemyState.knockback;
        isTakingDamage = true;

        currentHealth -= damageTaken;
        //CameraShakerHandler.Shake(damageShakeData);
        transform.DOScale(new Vector3(1f, 1f, 1f), .2f).SetEase(Ease.OutBounce).OnComplete(() => transform.DOScale(new Vector3(.8f, .8f, .8f), .1f).SetEase(Ease.InBounce));

        ScoreManager.instance.AddScore(2);
        if (currentHealth <= 0)
        {
            GameControl.instance.playerControl.DestroyedTarget();
            TargetDetectionControl.instance.allTargetsInScene.Remove(transform);
            Destroy(gameObject);
        }
    }

    void MoveState()
    {
        enemyState = EnemyState.move;
    }

    public void ReadyToTakeDamage()
    {
        
        enemyState = EnemyState.readyToTakeDamage;
        if (agent.enabled)
        {
            agent.ResetPath();
        }
        agent.enabled = false;
    }

    void HandleEnemyState()
    {
        switch (enemyState)
        {
            case EnemyState.idle:
                rb.velocity = Vector3.zero;
                agent.enabled = true;
                animator.SetBool("move", false);
                break;

            case EnemyState.move:
                rb.velocity = Vector3.zero;
                agent.enabled = true;
                agent.SetDestination(player.position);
                animator.SetBool("move", true);
                break;

            case EnemyState.readyToTakeDamage:
                animator.SetBool("move", false);
                break;

            case EnemyState.knockback:
              
                animator.SetBool("move", false);

                break;

            case EnemyState.attack:
                if (isAttacking)
                {
                    return;
                }
                isAttacking = true;
                
                animator.SetBool("move", false);
                if (agent.enabled)
                {
                    agent.ResetPath();
                }
                agent.enabled = false;

                //animator.SetBool("attack", true);
                Invoke("Attack", .3f);
               
                break;
        }
    }

    void Attack()
    {
        Instantiate(slashVfx, attackPos.position, Quaternion.identity);
        PerformAttack();
        FaceThis(player.transform.position);
        Invoke("ResetAttack", .5f);
    }

    public void FaceThis(Vector3 target)
    {
        Vector3 target_ = new Vector3(target.x, target.y, target.z);
        Quaternion lookAtRotation = Quaternion.LookRotation(target_ - transform.position);
        lookAtRotation.x = 0;
        lookAtRotation.z = 0;
        transform.DOLocalRotateQuaternion(lookAtRotation, 0.2f);
    }

    void ResetAttack()
    {
        
        animator.SetBool("attack", false);
        MoveState();
        isAttacking = false;
    }


    public void CheckPlayerInRange()
    {
        if(Vector3.Distance(transform.position, player.position) <= range)
        {
            enemyState = EnemyState.attack;
        }
    }
  
  
    public void SpawnHitVfx(Vector3 Pos_)
    {
        Instantiate(hitVfx, Pos_, Quaternion.identity);
    }

    public void ActiveTarget(bool bool_)
    {
        activeTargetObject.SetActive(bool_);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPos.position, attackRange);
    }

}
