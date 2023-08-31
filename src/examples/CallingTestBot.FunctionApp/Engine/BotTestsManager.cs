using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallingTestBot.FunctionApp.Engine;

public class BotTestsManager
{
    public BotTestsManager()
    {
    }

    public async Task CallConnectedSuccesfully(string? callId)
    { 
    }

    internal Task CallTerminated(string callId)
    {
        throw new NotImplementedException();
    }
}
