using ServiceHostedMediaCallingBot.Engine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceHostedMediaCallingBot.Engine;


public interface ICommsNotificationsPayloadHandler
{
    Task HandleNotificationsAndUpdateCallStateAsync(CommsNotificationsPayload notifications);
}
