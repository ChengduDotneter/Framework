using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Common.DAL.Transaction
{
    interface IDeadlockDetection : IGrainWithIntegerKey
    {
        Task EnterLock(long identity, string resourceName, int weight);
        Task ExitLock(long identity, string resourceName);
    }
}
