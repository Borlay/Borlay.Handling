using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Handling
{
    public interface IActionConverterProvider
    {
        IActionConverter GetActionConverter(byte type);

        bool TryGetActionConverter(byte type, out IActionConverter actionConverter);
    }
}
