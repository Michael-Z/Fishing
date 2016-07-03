﻿using System;
using System.Collections.Generic;
using GF.Unity.Common;

namespace Fishing
{
    public class EtPlayerMirror : EntityDef
    {
        //---------------------------------------------------------------------
        public override void declareAllComponent(byte node_type)
        {
            declareComponent<DefActorMirror>();
            if (node_type == (byte)NodeType.Cell)
            {
                declareComponent<DefActorMirrorAi>();
            }
        }
    }
}
