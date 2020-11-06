
using UnityEngine;
      
[CreateAssetMenu(menuName = ("Wargear/Melee"))]
public class MeleeWeapon : ScriptableObject
{
    [Header("Stats")]
    public float range;
    public float speed;
    public int ap;
    public int dmg;

    [Header("Animations")]
    public AnimationClip standIdle;
    public AnimationClip crouchIdle;
    public AnimationClip run;
    public AnimationClip standAttack;
    public AnimationClip crouchAttack;
    public AnimationClip standEquip;
    public AnimationClip crouchEquip;
    public AnimationClip standUnequip;
    public AnimationClip crouchUnequip;
}