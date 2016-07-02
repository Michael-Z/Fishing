﻿using System;
using System.Collections.Generic;
using System.Text;
using GF.Unity.Common;

namespace Ps
{
    public class TbDataEffectFullScreen : EbData
    {
        //-------------------------------------------------------------------------
        public enum DataState
        {
            Default = -1,
            ACTIVE = 0,
            DEACTIVE = 1
        }

        //-------------------------------------------------------------------------
        public DataState State { get; private set; }

        //-------------------------------------------------------------------------
        public override void load(EbPropSet prop_set)
        {
            var prop_state = prop_set.getPropInt("I_State");
            State = prop_state == null ? DataState.Default : (DataState)prop_state.get();
        }
    }
}
