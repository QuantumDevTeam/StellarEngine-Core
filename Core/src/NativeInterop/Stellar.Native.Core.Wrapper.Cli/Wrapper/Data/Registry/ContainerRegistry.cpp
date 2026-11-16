#include "Wrapper/Data/Registry/ContainerRegistry.cli.h"

#include <vector>

using Stellar::Core::Native::NativeCall;
using Stellar::Core::Native::NativeConvert;
using NativeRegistry = nc::Data::Registry::ContainerRegistry;

namespace Stellar::Core::Data::Registry
{
    namespace
    {
        array<::Stellar::Kernel::Identifier>^ ToManaged(Link<const std::vector<nc::Identifier>> identifiers)
        {
            auto result = gcnew array<::Stellar::Kernel::Identifier>(static_cast<int>(identifiers.size()));

            for (int i = 0; i < result->Length; ++i)
                result[i] = NativeConvert::ToManaged(identifiers[static_cast<std::size_t>(i)]);

            return result;
        }
    }

    bool ContainerRegistry::Create(::Stellar::Kernel::Identifier container)
    {
        return NativeCall<&NativeRegistry::Create>(_context->Native().Containers(), NativeConvert::ToNative(container));
    }

    bool ContainerRegistry::Destroy(::Stellar::Kernel::Identifier container)
    {
        return NativeCall<&NativeRegistry::Destroy>(_context->Native().Containers(), NativeConvert::ToNative(container));
    }

    bool ContainerRegistry::Exists(::Stellar::Kernel::Identifier container)
    {
        return NativeCall<&NativeRegistry::Exists>(_context->Native().Containers(), NativeConvert::ToNative(container));
    }

    void ContainerRegistry::Seal(::Stellar::Kernel::Identifier container)
    {
        NativeCall<&NativeRegistry::Seal>(_context->Native().Containers(), NativeConvert::ToNative(container));
    }

    bool ContainerRegistry::IsSealed(::Stellar::Kernel::Identifier container)
    {
        return NativeCall<&NativeRegistry::IsSealed>(_context->Native().Containers(), NativeConvert::ToNative(container));
    }

    bool ContainerRegistry::Add(::Stellar::Kernel::Identifier container, ::Stellar::Kernel::Identifier item)
    {
        return NativeCall<&NativeRegistry::Add>(_context->Native().Containers(), NativeConvert::ToNative(container), NativeConvert::ToNative(item));
    }

    bool ContainerRegistry::Remove(::Stellar::Kernel::Identifier container, ::Stellar::Kernel::Identifier item)
    {
        return NativeCall<&NativeRegistry::Remove>(_context->Native().Containers(), NativeConvert::ToNative(container), NativeConvert::ToNative(item));
    }

    bool ContainerRegistry::Contains(::Stellar::Kernel::Identifier container, ::Stellar::Kernel::Identifier item)
    {
        return NativeCall<&NativeRegistry::Contains>(_context->Native().Containers(), NativeConvert::ToNative(container), NativeConvert::ToNative(item));
    }

    int ContainerRegistry::Count(::Stellar::Kernel::Identifier container)
    {
        return static_cast<int>(NativeCall<&NativeRegistry::Count>(_context->Native().Containers(), NativeConvert::ToNative(container)));
    }

    array<::Stellar::Kernel::Identifier>^ ContainerRegistry::Items(::Stellar::Kernel::Identifier container)
    {
        return ToManaged(NativeCall<&NativeRegistry::Items>(_context->Native().Containers(), NativeConvert::ToNative(container)));
    }

    bool ContainerRegistry::Clear(::Stellar::Kernel::Identifier container)
    {
        return NativeCall<&NativeRegistry::Clear>(_context->Native().Containers(), NativeConvert::ToNative(container));
    }

    bool ContainerRegistry::AddBranch(::Stellar::Kernel::Identifier container, ::Stellar::Kernel::Identifier branch)
    {
        return NativeCall<&NativeRegistry::AddBranch>(_context->Native().Containers(), NativeConvert::ToNative(container), NativeConvert::ToNative(branch));
    }

    bool ContainerRegistry::RemoveBranch(::Stellar::Kernel::Identifier container, ::Stellar::Kernel::Identifier branch)
    {
        return NativeCall<&NativeRegistry::RemoveBranch>(_context->Native().Containers(), NativeConvert::ToNative(container), NativeConvert::ToNative(branch));
    }

    bool ContainerRegistry::ContainsBranch(::Stellar::Kernel::Identifier container, ::Stellar::Kernel::Identifier branch)
    {
        return NativeCall<&NativeRegistry::ContainsBranch>(_context->Native().Containers(), NativeConvert::ToNative(container), NativeConvert::ToNative(branch));
    }

    array<::Stellar::Kernel::Identifier>^ ContainerRegistry::Branches(::Stellar::Kernel::Identifier container)
    {
        return ToManaged(NativeCall<&NativeRegistry::Branches>(_context->Native().Containers(), NativeConvert::ToNative(container)));
    }
}
