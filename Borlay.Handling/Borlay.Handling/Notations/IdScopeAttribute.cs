using Borlay.Arrays;
using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Handling.Notations
{
    public class IdScopeAttribute : ScopeAttribute
    {
        public int ActionId { get; }

        private readonly byte[] bytes = new byte[4];

        public IdScopeAttribute(int action)
        {
            this.ActionId = action;
            bytes.AddBytes<int>(action, 4, 0);
        }

        public override byte[] GetScopeId()
        {
            return bytes;
        }
    }
}
