using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeruTeruServer.ManageLogic.Protocol
{
    public enum MethodsSelector
    {
        RequestConnection = 0,
        RequestRegisterRole = 1,
        SendImage = 2,
        RequestObjectDetect = 3,
        ObjectDetectResult = 4,
      
        SendChatData = 5,
        NotifyPlayerExit = 6,
    }
    public class MethodsSelectorUtil
    {
        // enum 2 int
        public static int GetMethodId(MethodsSelector method)
        {
            return (int)method;
        }
    }
}
