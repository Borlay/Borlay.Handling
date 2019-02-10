using Borlay.Arrays;
using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Handling.Notations
{
    public class IdActionAttribute : ActionAttribute
    {
        public uint ActionId { get; }

        public override byte ActionType => 1;

        public IdActionAttribute(uint action)
        {
            this.ActionId = action;
        }

        public override string GetActionId()
        {
            return ActionId.ToString();
        }

        public override void AddActionId(byte[] bytes, ref int index) // todo implementuoti
        {
            bytes.AddBytes<uint>(ActionId, 4, ref index);
        }

        public override string GetActionId(byte[] bytes, ref int index)
        {
            var actionId = bytes.GetValue<uint>(4, ref index);
            return actionId.ToString();
        }
    }
}
