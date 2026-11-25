using System.Reflection;

using Stellar.Kernel.Configuration;
using Stellar.Kernel.Configuration.Modules;
using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.LoggingSystem;
using Stellar.Kernel.Services;
using Stellar.Runtime.Diagnostics;
using Stellar.Runtime.Hosting;
using Stellar.Runtime.Lifecycle;
using Stellar.Runtime.Modules;
using Stellar.Runtime.Threading;

namespace Stellar.Runtime.Tests;

public class ModuleStarterTests
{
    [Fact]
    public void ALoggerWithdrawnByItsModule_IsNotWrittenToAnyMore()
    {
        var output = new StringWriter();
        using var dispatcher = new MainThreadDispatcher();
        using var control = new EngineControl();
        var context =
            new RuntimeContext(new RuntimeLaunch("Tests"), new RuntimeLog(fallback: output), dispatcher, control)
            {
                Services = DispatchProxy.Create<IServiceCatalog, Services>()
            };
        var module = new LoadedModule(new RuntimeModuleData { Id = "Logging" }, typeof(Logging).Assembly,
            typeof(Logging));

        ModuleStarter.Start(context, module);
        context.Log.Logger.Should().NotBeNull();

        ModuleStarter.Stop(context, module);
        context.Log.Error("after the logger");

        output.ToString().Should().Contain("after the logger");
    }

    /// <summary>Publishes a logger, and takes it back when it stops.</summary>
    public sealed class Logging(RuntimeConfiguration configuration)
        : StellarEntryPoint("Tests/Logging", configuration)
    {
        private IServiceCatalog? _services;

        public override int Run<TContext>(ref readonly TContext context)
        {
            _services = context.Services;
            _services!.Set(DispatchProxy.Create<ILogger, Silent>());
            return 0;
        }

        public override void RequestStop<TContext>(ref readonly TContext context)
        {
            _services!.Remove<ILogger>();
        }

        public override void Dispose()
        {
        }
    }

    public class Silent
        : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            return null;
        }
    }

    public class Services
        : DispatchProxy
    {
        private readonly Dictionary<Type, object> _items = [];

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            var key = (Type)args![0]!;

            switch (method!.Name)
            {
                case "Set":
                    _items[key] = args[1]!;
                    return null;
                case "Remove":
                    return _items.Remove(key);
                case "TryGet":
                    bool found = _items.TryGetValue(key, out object? value);
                    args[1] = value;
                    return found;
                default:
                    throw new NotSupportedException(method.Name);
            }
        }
    }
}
