using Autofac;
using AutofacSerilogIntegration;
using Serilog;

namespace DiskAnalyzer.Configuration
{
    public static class ContainerFactory
    {
        public static IContainer Build(ILogger logger)
        {
            var builder = new ContainerBuilder();

            builder.RegisterAssemblyTypes(typeof(MainWindow).Assembly).AsSelf().AsImplementedInterfaces().SingleInstance();

            builder.RegisterLogger(logger);
            builder.RegisterInstance(logger).AsImplementedInterfaces();

            return builder.Build();
        }
    }
}