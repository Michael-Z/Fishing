﻿using System;
using System.Collections.Generic;
using System.Text;
using GF.Unity.Common;

namespace Ps
{
    public class TbDataEffectFrameAnimation : EbData
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
        public string Note { get; private set; }
        public DataState State { get; private set; }
        public string SourceName;
        public int Scale;
        public int LastTime;

        //-------------------------------------------------------------------------
        public override void load(EbPropSet prop_set)
        {
            Name = prop_set.getPropString("T_Name").get();
            Note = prop_set.getPropString("T_Note").get();
            var prop_state = prop_set.getPropInt("I_State");
            State = prop_state == null ? DataState.Default : (DataState)prop_state.get();
            SourceName = prop_set.getPropString("T_SourceName").get();
            Scale = prop_set.getPropInt("I_Scale").get();
            LastTime = prop_set.getPropInt("I_LastTime").get();
        }
    }
}
