using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceHostedMediaCallingBot.Engine.Models;

public static class Extentions
{
    public static bool IsConnected(this CallMediaState? callMediaState)
    {
        return callMediaState != null && callMediaState.Audio.HasValue && callMediaState.Audio.Value == MediaState.Active;
    }
}
