using System;
using UnityEngine;
using UI;
using Character.Components;
using Unity.VisualScripting;

namespace Game.UI
{
    /// <summary>
    /// BagView 纯逻辑核心（可单元测试）
    /// </summary>
    public class BagViewLogicCore
    {
        protected BagViewView _view;

        private PlayerManager playerManager;

        private CharacterStats localCharacterStats;
        private PlayerController localplayerController;


        public virtual void Bind(UIViewBase view)
        {
            _view = view as BagViewView;
        }

        public virtual void OnOpen(UIArgs args)
        {
            playerManager = GameRoot.Instance.PlayerManager;
            localplayerController = playerManager.GetLocalPlayerData()?.Controller;
            localCharacterStats = localplayerController?.GetComponent<CharacterStats>();
            if (localCharacterStats != null)
            {
                // 防止重复订阅：先移除再添加
                localCharacterStats.OnStatsChanged -= SetAllPlayerStatsText;
                localCharacterStats.OnStatsChanged += SetAllPlayerStatsText;
            }
            SetAllPlayerStatsText();
            _view.SetPlayerImage(localCharacterStats.Icon);


        }

        public virtual void OnClose()
        {
            // 关闭时清理
            if (localCharacterStats != null)
            {
                localCharacterStats.OnStatsChanged -= SetAllPlayerStatsText;
                localCharacterStats = null;
            }
            _view = null;
        }

        public void OnPlayerStats1Clicked()
        {
            // TODO: 处理按钮点击后的业务逻辑（纯逻辑）
            // 可在此调用 _view.SetXXX 方法更新文本内容
        }

        public void OnPlayerStats12Clicked()
        {
            // TODO: 处理按钮点击后的业务逻辑（纯逻辑）
            // 可在此调用 _view.SetXXX 方法更新文本内容
        }



        public void SetAllPlayerStatsText()
        {
             if (playerManager != null)
            {
                var player = playerManager.GetLocalPlayerData()?.Controller;
                if (player != null)
                {
                    if (localCharacterStats != null)
                    {
                        _view.SetMaxHP("MaxHP: " + localCharacterStats.MaxHP.ToString());
                        _view.SetHPRegen("HPRegen: " + localCharacterStats.HPRegen.ToString());
                        _view.SetMoveSpeed("MoveSpeed: " + localCharacterStats.MoveSpeed.ToString());
                        _view.SetAcceleration("Acceleration: " + localCharacterStats.Acceleration.ToString());
                        _view.SetAttackPower("AttackPower: " + localCharacterStats.AttackPower.ToString());
                        _view.SetAttackSpeed("AttackSpeed: " + localCharacterStats.AttackSpeed.ToString());
                        _view.SetAttackRange("AttackRange: " + localCharacterStats.AttackRange.ToString());
                        _view.SetArmor("Armor: " + localCharacterStats.Armor.ToString());
                        _view.SetDodge("Dodge: " + localCharacterStats.Dodge.ToString());
                        _view.SetSkillCooldownReductionRate("SkillCooldownReductionRate: " + localCharacterStats.SkillCooldownReductionRate.ToString());
                    }
                }
            }
        }

    }

    /// <summary>
    /// MonoBehaviour Wrapper：创建并持有 LogicCore，在运行时作为 IUILogic 注入到 View
    /// </summary>
    public class BagViewLogic : MonoBehaviour, IUILogic
    {
        private BagViewLogicCore _core = new BagViewLogicCore();

        public void Bind(UIViewBase view)
        {
            _core.Bind(view);
        }

        public void OnOpen(UIArgs args)
        {
            _core.OnOpen(args);
        }

        public void OnClose()
        {
            _core.OnClose();
        }

        private void OnPlayerStats1Clicked()
        {
            _core.OnPlayerStats1Clicked();
        }

        private void OnPlayerStats12Clicked()
        {
            _core.OnPlayerStats12Clicked();
        }
    }
}
