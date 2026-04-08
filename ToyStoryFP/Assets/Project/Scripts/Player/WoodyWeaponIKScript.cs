using UnityEngine;

[DisallowMultipleComponent]
public class WoodyWeaponIKScript : MonoBehaviour
{
    [Header("Weapon Lookup")]
    [SerializeField] private string weaponPath = "FPCamara/AssaultRifle";
    [SerializeField] private string leftGripName = "LeftHandGrip";
    [SerializeField] private string rightGripName = "RightHandGrip";
    [SerializeField] private string leftHintName = "LeftElbowHint";
    [SerializeField] private string rightHintName = "RightElbowHint";

    [Header("IK Weights")]
    [Range(0f, 1f)] [SerializeField] private float leftHandWeight = 1f;
    [Range(0f, 1f)] [SerializeField] private float rightHandWeight = 0.75f;
    [Range(0f, 1f)] [SerializeField] private float leftHintWeight = 0.75f;
    [Range(0f, 1f)] [SerializeField] private float rightHintWeight = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float airborneWeightMultiplier = 0.85f;

    private Animator animator;
    private MovementScript movementScript;
    private WeaponLoadoutScript weaponLoadout;
    private Transform weaponRoot;
    private Transform leftGrip;
    private Transform rightGrip;
    private Transform leftHint;
    private Transform rightHint;

    void Awake()
    {
        animator = GetComponent<Animator>();
        movementScript = transform.root.GetComponent<MovementScript>();
        weaponLoadout = transform.root.GetComponentInChildren<WeaponLoadoutScript>(true);
        WeaponScript activeWeapon = GetActiveWeapon();

        if (activeWeapon != null)
        {
            ResolveTargets(activeWeapon.transform);
        }
    }

    void LateUpdate()
    {
        WeaponScript activeWeapon = GetActiveWeapon();

        if (activeWeapon == null)
        {
            weaponRoot = null;
            leftGrip = null;
            rightGrip = null;
            leftHint = null;
            rightHint = null;
            return;
        }

        if (weaponRoot != activeWeapon.transform || leftGrip == null || rightGrip == null)
        {
            ResolveTargets(activeWeapon.transform);
        }
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || !animator.isHuman)
        {
            return;
        }

        if (leftGrip == null && rightGrip == null)
        {
            return;
        }

        float weightMultiplier = movementScript != null && !movementScript.IsGrounded
            ? airborneWeightMultiplier
            : 1f;

        ApplyGoal(AvatarIKGoal.LeftHand, leftGrip, leftHandWeight * weightMultiplier);
        ApplyGoal(AvatarIKGoal.RightHand, rightGrip, rightHandWeight * weightMultiplier);
        ApplyHint(AvatarIKHint.LeftElbow, leftHint, leftHintWeight * weightMultiplier);
        ApplyHint(AvatarIKHint.RightElbow, rightHint, rightHintWeight * weightMultiplier);
    }

    // Obtiene activo arma.
    private WeaponScript GetActiveWeapon()
    {
        if (weaponLoadout == null)
        {
            weaponLoadout = transform.root.GetComponentInChildren<WeaponLoadoutScript>(true);
        }

        if (weaponLoadout != null && weaponLoadout.CurrentWeapon != null)
        {
            return weaponLoadout.CurrentWeapon;
        }

        WeaponScript[] weapons = transform.root.GetComponentsInChildren<WeaponScript>(true);

        foreach (WeaponScript weapon in weapons)
        {
            if (weapon.gameObject.activeInHierarchy)
            {
                return weapon;
            }
        }

        return weapons.Length > 0 ? weapons[0] : null;
    }

    // Resuelve objetivos.
    private void ResolveTargets(Transform activeWeaponRoot)
    {
        weaponRoot = activeWeaponRoot;

        if (weaponRoot == null)
        {
            leftGrip = null;
            rightGrip = null;
            leftHint = null;
            rightHint = null;
            return;
        }

        if (!string.IsNullOrWhiteSpace(weaponPath))
        {
            Transform pathMatch = transform.root.Find(weaponPath);

            if (pathMatch != null && pathMatch.gameObject.activeInHierarchy)
            {
                weaponRoot = pathMatch;
            }
        }

        leftGrip = FindNamedChild(weaponRoot, leftGripName);
        rightGrip = FindNamedChild(weaponRoot, rightGripName);
        leftHint = FindNamedChild(weaponRoot, leftHintName);
        rightHint = FindNamedChild(weaponRoot, rightHintName);
    }

    // Aplica goal.
    private void ApplyGoal(AvatarIKGoal goal, Transform target, float weight)
    {
        if (target == null)
        {
            animator.SetIKPositionWeight(goal, 0f);
            animator.SetIKRotationWeight(goal, 0f);
            return;
        }

        animator.SetIKPositionWeight(goal, weight);
        animator.SetIKRotationWeight(goal, weight);
        animator.SetIKPosition(goal, target.position);
        animator.SetIKRotation(goal, target.rotation);
    }

    // Aplica hint.
    private void ApplyHint(AvatarIKHint hint, Transform target, float weight)
    {
        if (target == null)
        {
            animator.SetIKHintPositionWeight(hint, 0f);
            return;
        }

        animator.SetIKHintPositionWeight(hint, weight);
        animator.SetIKHintPosition(hint, target.position);
    }

    // Busca named hijo.
    private static Transform FindNamedChild(Transform parent, string childName)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }
}
