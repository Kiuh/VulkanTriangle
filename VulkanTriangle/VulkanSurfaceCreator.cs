using Silk.NET.Core.Contexts;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace VulkanTriangle;

internal static class VulkanSurfaceCreator
{
    public static unsafe (KhrSurface, SurfaceKHR) CreateSurface(
        Vk vkInstance,
        Instance instance,
        IVkSurface vkSurface
    )
    {
        return !vkInstance!.TryGetInstanceExtension(instance, out KhrSurface khrSurface)
            ? throw new NotSupportedException("KHR_surface extension not found.")
            : (
                khrSurface,
                vkSurface.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface()
            );
    }
}
