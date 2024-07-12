using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using StarterAssets;
public class PlayerControl : MonoBehaviour
{
    [Space]
    [Header("Components")]
    [SerializeField] private Animator anim;
    [SerializeField] private ThirdPersonController thirdPersonController;
  
 
    [Space]
    [Header("Combat")]
    public Transform target;
    [SerializeField] private Transform attackPos;
    [SerializeField] private float punchDeltaDistance;
    [SerializeField] private float kickDeltaDistance;
    [SerializeField] private float knockbackForce = 10f; 
    [SerializeField] private float airknockbackForce = 10f; 
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float reachTime = 0.3f;
    [SerializeField] private LayerMask enemyLayer;
    bool isAttacking = false;

    [Space]
    [Header("Debug")]
    [SerializeField] private bool debug;

    // Start is called before the first frame update
    void Start()
    {
        ChangeTargetInRange();
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Attack();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            ChangeTargetInRange();
        }
    }

    #region Attack, PerformAttack, Reset Attack, Change Target
    public void ChangeTargetInRange() 
    {
        //GameControl.instance.RepopulateTarget();
        target = GameControl.instance.GetNextTargetInRange();
    }



    public void Attack()
    {
        if (isAttacking)
        {
            return;
        }

        thirdPersonController.canMove = false;
        RandomAttackAnim();
       
    }

    private void RandomAttackAnim()
    {
        int attackIndex = Random.Range(1, 4);
        if (debug)
        {
            Debug.Log(attackIndex + " attack index");
        }

        switch (attackIndex)
        {
            case 1: //punch

                if (target != null)
                    MoveTowardsTarget(target.position, punchDeltaDistance, "punch");

                isAttacking = true;

                break;

            case 2: //kick

                

                if (target != null)
                    MoveTowardsTarget(target.position, kickDeltaDistance, "kick");

                isAttacking = true;

                break;

            case 3: //mmakick

            

                if (target != null)
                    MoveTowardsTarget(target.position, kickDeltaDistance, "mmakick");

                isAttacking = true;

                break;
        }
    }

    public void ResetAttack()
    {
        anim.SetBool("punch", false);
        anim.SetBool("kick", false);
        anim.SetBool("mmakick", false);
        thirdPersonController.canMove = true;
        isAttacking = false;
    }

    public void PerformAttack()
    {
        // Assuming we have a melee attack with a short range
       
        Collider[] hitEnemies = Physics.OverlapSphere(attackPos.position, attackRange, enemyLayer);

        foreach (Collider enemy in hitEnemies)
        {
            Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
            EnemyBase enemyBase = enemy.GetComponent<EnemyBase>();
            if (enemyRb != null)
            {
                // Calculate knockback direction
                Vector3 knockbackDirection = enemy.transform.position - transform.position;
                knockbackDirection.y = airknockbackForce; // Keep the knockback horizontal

                // Apply force to the enemy
                enemyRb.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode.Impulse);
                enemyBase.SpawnHitVfx(enemyBase.transform.position);
            }
        }
    }
    public void CheckTargetInRange()
    {
        if (target == null && GameControl.instance.allTargetsInRange.Count > 0)
        {
            ChangeTargetInRange();
        }
    }
    #endregion


    #region MoveTowards, Target Offset and FaceThis
    public void MoveTowardsTarget(Vector3 target_, float deltaDistance, string animationName_)
    {

        PerformAttackAnimation(animationName_);
        //transform.DOLookAt(target, .2f);
        FaceThis(target_);
        Vector3 finalPos = TargetOffset(target_, deltaDistance);
        finalPos.y = 0;
        //anim.SetBool("readyAttack", true);
        transform.DOMove(finalPos, reachTime);
        //transform.DOMove(finalPos, reachTime).OnComplete(() => PerformAttackAnimation(animationName_));

    }

    void PerformAttackAnimation(string animationName_)
    {
        //anim.SetBool("readyAttack", false);
        anim.SetBool(animationName_, true);
    }

    public Vector3 TargetOffset(Vector3 target, float deltaDistance)
    {
        Vector3 position;
        position = target;
        return Vector3.MoveTowards(position, transform.position, deltaDistance);
    }

    public void FaceThis(Vector3 target)
    {
        Vector3 target_ = new Vector3(target.x, target.y, target.z);
        Quaternion lookAtRotation = Quaternion.LookRotation(target_ - transform.position);
        lookAtRotation.x = 0;
        lookAtRotation.z = 0;
        transform.DOLocalRotateQuaternion(lookAtRotation, 0.2f);
    }
    #endregion

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPos.position, attackRange); // Visualize the attack range
    }
}
