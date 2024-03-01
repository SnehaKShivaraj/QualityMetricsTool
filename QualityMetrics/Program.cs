

using ServiceLayer.Implementations;
using ServiceLayer.Interfaces;
using Unity;
using System;

namespace QualityMetrics
{
    class Program
    {
        static void Main(string[] args)
        {
            UnityContainer unityContainer = new UnityContainer();


            unityContainer.RegisterType<IQualityMetrics, QualityMetrics>();

            unityContainer.RegisterType<IExcelUtilities, ExcelUtilities>();
            unityContainer.RegisterType<IEmailUtilities, EmailUtilities>();
            unityContainer.RegisterType<ITfsServices, TfsServices>();

            var qualityMetrics = unityContainer.Resolve<IQualityMetrics>();
            qualityMetrics.GatherQualityMetrics();

            Console.ReadKey();
        }
    }
}
