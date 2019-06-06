// TSS - Unity visual tweener plugin
// © 2018 ObelardO aka Vladislav Trubitsyn
// obelardos@gmail.com
// https://obeldev.ru/tss
// MIT License

using System;
using System.Collections.Generic;
using UnityEngine;
using TSS.Base;

namespace TSS
{
    [Serializable]
    public class TSSProfile : ScriptableObject
    {
        #region Properties

        public bool isUI = false;

        public void Awake()
        {
            TSSItemBase.InitValues(ref values);
        }

        public string version;
        public TSSItemValues values;
        public List<TSSTween> tweens = new List<TSSTween>();

        #endregion

        #region Values throw

        /// <summary>
        /// Record values from specified item to profile
        /// </summary>
        /// <param name="item">item pointer</param>
        /// <param name="profile">profile pointer</param>
        public static void ProfileApply(TSSItem item, TSSProfile profile)
        {
            profile.values = item.values;
            profile.values.path = item.values.path.Clone();
            profile.tweens = item.tweens.Clone(null);

            profile.isUI = (item.rect != null);

            profile.values.colors = new Color[TSSItemBase.stateCount]
            {
                item.values.colors[0],
                item.values.colors[1]
            };

            profile.version = TSSInfo.version;
        }

        /// <summary>
        /// Record values from specified profile to item
        /// </summary>
        /// <param name="item">item pointer</param>
        /// <param name="profile">profile pointer</param>
        public static void ProfileRevert(TSSItem item, TSSProfile profile)
        {
            if (string.IsNullOrEmpty(profile.version)) profile.version = TSSInfo.version;

            if (profile.version.Substring(0, 3) != TSSInfo.version.Substring(0, 3))
            {
                Debug.LogWarningFormat("TSS Profile version ({0}) is mismatch the version of the installed TSS plugin version ({1}).", profile.version, TSSInfo.version);
                return;
            }

            item.values = profile.values;
            item.values.path = profile.values.path.Clone();
            item.tweens = profile.tweens.Clone(item);

            item.values.colors = new Color[TSSItemBase.stateCount]
            {
                profile.values.colors[0],
                profile.values.colors[1]
            };

            item.CloseBranchImmediately();
        }

        #endregion

        #region Unity methods

        private void OnDrawGizmos()
        {

        }

        #endregion
    }
}