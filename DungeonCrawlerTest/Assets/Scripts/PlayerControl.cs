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
            Attack(0);
        }

        if (Input.GetMouseButtonDown(1))
        {
            Attack(1);
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



    public void Attack(int attackState)
    {
        if (isAttacking)
        {
            return;
        }

        thirdPersonController.canMove = false;
        RandomAttackAnim(attackState);
       
    }

   

    private void RandomAttackAnim(int attackState)
    {
        

        switch (attackState) 
        {
            case 0: //Quick Attack

                QuickAttack();
                break;

            case 1:
                HeavyAttack();
                break;

        }


       
    }

    void QuickAttack()
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
                {
                    MoveTowardsTarget(target.position, punchDeltaDistance, "punch");
                    isAttacking = true;
                }
                else
                {
                    thirdPersonController.canMove = true;
                }

                break;

            case 2: //kick

                if (target != null)
                {
                    MoveTowardsTarget(target.position, kickDeltaDistance, "kick");
                    isAttacking = true;
                }
                else
                {
                    thirdPersonController.canMove = true;
                }
                   

                break;

            case 3: //mmakick

                if (target != null)
                {
                    MoveTowardsTarget(target.position, kickDeltaDistance, "mmakick");

                    isAttacking = true;
                }
                else
                {
                    thirdPersonController.canMove = true;
                }
               

                break;
        }
    }

    void HeavyAttack()
    {
        //int attackIndex = Random.Range(1, 4);
        int attackIndex = 2;
        if (debug)
        {
            Debug.Log(attackIndex + " attack index");
        }

        switch (attackIndex)
        {
            case 1: //heavyAttack1

                if (target != null)
                {
                    MoveTowardsTarget(target.position, kickDeltaDistance, "heavyAttack1");

                    isAttacking = true;
                }
                else
                {
                    thirdPersonController.canMove = true;
                }


                break;

            case 2: //heavyAttack2

                if (target != null)
                {
                    //MoveTowardsTarget(target.position, kickDeltaDistance, "heavyAttack2");
                    FaceThis(target.position);
                    anim.SetBool("heavyAttack2", true);
                    isAttacking = true;
                }
                else
                {
                    thirdPersonController.canMove = true;
                }


                break;

            case 3: //heavyAttack3

                if (target != null)
                {
                    MoveTowardsTarget(target.position, kickDeltaDistance, "heavyAttack3");

                    isAttacking = true;
                }
                else
                {
                    thirdPersonController.canMove = true;
                }
           

                break;
        }
    }

    public void ResetAttack()
    {
        anim.SetBool("punch", false);
        anim.SetBool("kick", false);
        anim.SetBool("mmakick", false);
        anim.SetBool("heavyAttack1", false);
        anim.SetBool("heavyAttack2", false);
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

    public void GetClose() 
    {
        Vector3 target_ = target.position;
        FaceThis(target_);
        Vector3 finalPos = TargetOffset(target_, 1.4f);
        finalPos.y = 0;
        transform.DOMove(finalPos, 0.2f);
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
