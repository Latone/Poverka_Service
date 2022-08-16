using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Poverka_Service
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// 
        /// ADMIN RIGHTS REQUIRED::
        /// to install service:: installutil Poverka_Service.exe
        /// to delete service:: installutil.exe /u Poverka_Service.exe
        /// </summary>
        static void Main(string[] args)
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Poverka_TCP_Listener(args)
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
