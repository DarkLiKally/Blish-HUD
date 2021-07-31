﻿using ProtoBuf;

namespace Blish_HUD.DebugHelper.Models {

    [ProtoContract]
    public class KeyboardEventMessage : Message {

        [ProtoMember(101)] public uint EventType { get; set; }

        [ProtoMember(102)] public int Key { get; set; }

    }

}
