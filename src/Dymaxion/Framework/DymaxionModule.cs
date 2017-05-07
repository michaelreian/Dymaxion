using Autofac;
using Autofac.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Dymaxion.Framework
{
    public class DymaxionModule : Autofac.Module
    {

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(Assembly.GetEntryAssembly())
                .AsSelf()
                .AsImplementedInterfaces();

            //builder.Register(x => Statsd.New<Udp>(o =>
            //{
            //    var settings = x.Resolve<IOptions<StatsdSettings>>();
            //    o.Port = settings.Value.Port;
            //    o.HostOrIp = settings.Value.Host;
            //    o.Prefix = settings.Value.Prefix;
            //})).As<IStatsd>().SingleInstance();

            builder.Register(x => Log.Logger).As<ILogger>().SingleInstance();



            base.Load(builder);
        }

        protected override void AttachToComponentRegistration(IComponentRegistry componentRegistry, IComponentRegistration registration)
        {
            registration.Preparing += Registration_Preparing;
        }

        private void Registration_Preparing(object sender, PreparingEventArgs e)
        {
            Type t = e.Component.Activator.LimitType;
            e.Parameters =
                e.Parameters.Union(
                    new[]
                    {
                        new ResolvedParameter(
                            (p, i) => p.ParameterType == typeof(ILogger),
                            (p, i) => i.Resolve<ILogger>().ForContext(t))
                    });
        }
    }
}
