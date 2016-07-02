﻿using System;
using System.Collections.Generic;
using System.Text;
using GF.Unity.Common;

namespace Ps
{
    public class TbDataEffectLockScreen : EbData
    {
        //-------------------------------------------------------------------------
        public enum DataState
        {
            Default = -1,
            ACTIVE = 0,
            DEACTIVE = 1
        }

        //-------------------------------------------------------------------------
        public string Name { get; private set; }
        public DataState State { get; private set; }
        public int LastTime { get; private set; }

        //-------------------------------------------------------------------------
        public override void load(EbPropSet prop_set)
        {
            Name = prop_set.getPropString("T_Name").get();
            var prop_state = prop_set.getPropInt("I_State");
            State = prop_state == null ? DataState.Default : (DataState)prop_state.get();
            LastTime = prop_set.getPropInt("I_LastTime").get();
        }
    }
}
