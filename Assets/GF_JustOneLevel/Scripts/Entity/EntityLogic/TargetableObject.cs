﻿using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 可作为目标的实体类。
/// 参考来源：https://github.com/EllanJiang/StarForce
/// </summary>
public abstract class TargetableObject : Entity {
    [SerializeField]
    private TargetableObjectData targetableObjectData = null;

    /// <summary>
    /// 血量条
    /// </summary>
    private PowerBar hpBar;

    /// <summary>
    /// 移动控制器
    /// </summary>
    protected IMoveController moveController = null;

    /// <summary>
    /// 手动触发类型武器
    /// </summary>
    /// <typeparam name="Weapon"></typeparam>
    /// <returns></returns>
    protected List<Weapon> manualWeapons = new List<Weapon> ();
    /// <summary>
    /// 自动触发类型武器
    /// </summary>
    /// <typeparam name="Weapon"></typeparam>
    /// <returns></returns>
    protected List<Weapon> autoWeapons = new List<Weapon> ();
    /// <summary>
    /// 技能触发类型武器
    /// </summary>
    /// <typeparam name="Weapon"></typeparam>
    /// <returns></returns>
    protected List<Weapon> skillWeapons = new List<Weapon> ();

    public bool IsDead {
        get {
            return targetableObjectData.HP <= 0;
        }
    }

    /// <summary>
    /// 移动控制器
    /// </summary>
    /// <returns></returns>
    public IMoveController MoveController {
        get {
            if (moveController == null) {
                moveController = CreateMoveController();
            }
            return moveController;
        }
    }

    public abstract ImpactData GetImpactData ();

    /// <summary>
    /// 当子类需要在特殊情况下才能创建移动控制器时，可以重写该函数
    /// </summary>
    /// <returns></returns>
    protected virtual IMoveController CreateMoveController () {
        return null;
    }

    /// <summary>
    /// 接受伤害
    /// </summary>
    /// <param name="damageHP"></param>
    public virtual void ApplyDamage (int damageHP) { }

    /// <summary>
    /// 真正执行伤害/加血逻辑
    /// </summary>
    /// <param name="damageHP">伤害值，可以正负，如果小于1，则按百分比执行</param>
    /// <param name="effectFollowMaxHP">是否以最大生命值为准</param>
    public void OnDamage (float damageHP, bool effectFollowMaxHP = false) {
        int changeHP = 0;

        // 按百分比改变当前血量
        if ((damageHP > 0 && damageHP < 1) || (damageHP < 0 && damageHP > -1)) {
            if (effectFollowMaxHP) {
                changeHP = (int) (targetableObjectData.MaxHP * damageHP);
            }
            else {
                changeHP = (int) (targetableObjectData.HP * damageHP);
            }
        } else {
            changeHP = (int) damageHP;
        }
        // 伤害
        if (changeHP > 0) {
            changeHP -= targetableObjectData.Def;

            if (changeHP < 0) {
                changeHP = 0;
            }

            targetableObjectData.HP -= changeHP;

            OnHurt ();
        }
        // 加血
        else if (changeHP < 0) {
            targetableObjectData.HP += -changeHP;
            if (targetableObjectData.HP > targetableObjectData.MaxHP) {
                targetableObjectData.HP = targetableObjectData.MaxHP;
            }

            OnCure ();
        }

        // 更新血量条
        RefreshHPBar ();

        if (targetableObjectData.HP <= 0) {
            OnDead ();
        }
    }

    /// <summary>
    /// 切换动画
    /// </summary>
    /// <param name="state"></param>
    public void ChangeAnimation (FightEntityAnimationState state) {
        // Log.Info("Hero ChangeAnimation:" + state);
        ResetAnimation ();

        if (state == FightEntityAnimationState.walk) {
            cachedAnimator.SetBool ("IsWalking", true);
        } else if (state == FightEntityAnimationState.idle) { } else if (state == FightEntityAnimationState.atk) {
            cachedAnimator.SetBool ("IsAttacking", true);
        } else if (state == FightEntityAnimationState.hurt) {
            cachedAnimator.SetBool ("IsHurting", true);
        } else if (state == FightEntityAnimationState.dead) {
            cachedAnimator.SetBool ("IsDead", true);
        }
    }

    /// <summary>
    /// 转身
    /// </summary>
    /// <param name="destVec">目标位置</param>
    public void Rotate (Vector3 destVec) {
        CachedTransform.Rotate (destVec);
    }

    /// <summary>
    /// 更新血量条
    /// </summary>
    protected void RefreshHPBar () {
        hpBar.UpdatePower (targetableObjectData.HP, targetableObjectData.MaxHP);
    }

    private void ResetAnimation () {
        cachedAnimator.SetBool ("IsWalking", false);
        cachedAnimator.SetBool ("IsAttacking", false);
        cachedAnimator.SetBool ("IsHurting", false);
        cachedAnimator.SetBool ("IsDead", false);
    }

    protected override void OnInit (object userData) {
        base.OnInit (userData);
        CachedTransform.SetLayerRecursively (Constant.Layer.TargetableObjectLayerId);
    }

    protected override void OnShow (object userData) {
        base.OnShow (userData);

        targetableObjectData = userData as TargetableObjectData;
        if (targetableObjectData == null) {
            Log.Error ("Targetable object data is invalid.");
            return;
        }

        CachedTransform.localScale = Vector3.one;

        /* 附加血量条 */
        PowerBarData hpBarData = new PowerBarData (EntityExtension.GenerateSerialId (), 1, this.Id, CampType.Player);
        EntityExtension.ShowPowerBar (typeof (PowerBar), "PowerBarGroup", hpBarData);

    }

    protected override void OnAttached (EntityLogic childEntity, Transform parentTransform, object userData) {
        base.OnAttached (childEntity, parentTransform, userData);

        if (childEntity is PowerBar) {
            hpBar = (PowerBar) childEntity;
            hpBar.UpdatePower (targetableObjectData.HP, targetableObjectData.MaxHP);
            return;
        } else if (childEntity is Weapon) {
            WeaponData weaponData = (WeaponData) userData;
            Weapon weapon = (Weapon) childEntity;

            switch (weaponData.AttackType) {
                case WeaponAttackType.手动触发:
                    manualWeapons.Add (weapon);
                    break;
                case WeaponAttackType.自动触发:
                    autoWeapons.Add (weapon);
                    break;
                case WeaponAttackType.技能触发:
                    skillWeapons.Add (weapon);
                    break;
            }
            return;
        }
    }

    protected virtual void OnHurt () { }
    protected virtual void OnCure () { }

    protected virtual void OnDead () {
        // GameEntry.Entity.HideEntity (this.Entity);
    }

    private void OnTriggerEnter (Collider other) {
        Entity entity = other.gameObject.GetComponent<Entity> ();
        if (entity == null) {
            return;
        }

        if (entity is TargetableObject && entity.Id >= Id) {
            // 碰撞事件由 Id 小的一方处理，避免重复处理
            return;
        }

        AIUtility.PerformCollision (this, entity);
    }
}