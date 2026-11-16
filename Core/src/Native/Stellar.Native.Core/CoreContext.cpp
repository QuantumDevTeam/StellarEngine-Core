#include "pch.h"
#include "CoreContext.h"

namespace Stellar::Native::Core
{
    struct CoreContext::Impl
    {
        // Declared in reverse order of dependence: modules go first when the context is destroyed
        Modules::ModuleRegistry modules;
    };

    CoreContext::CoreContext() :
        _impl(std::make_unique<Impl>()) {}

    CoreContext::~CoreContext() = default;

    Link<CoreContext> CoreContext::Default()
    {
        static CoreContext instance;
        return instance;
    }

    Link<Modules::ModuleRegistry> CoreContext::Modules() const
    {
        return _impl->modules;
    }
}
