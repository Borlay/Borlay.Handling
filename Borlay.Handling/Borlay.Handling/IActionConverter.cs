using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Handling
{
    public interface IActionConverter
    {
        byte ActionType { get; }

        void AddActionId(byte[] bytes, ref int index);

        string GetActionId(byte[] bytes, ref int index);
    }
}
