using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace VulkanTriangle;

internal static unsafe class VulkanSyncObjectCreator
{
    public static (Semaphore[], Semaphore[], Fence[], Fence[]) CreateSyncObjects(
        Vk vk,
        Device device,
        Image[] swapChainImages,
        int MAX_FRAMES_IN_FLIGHT
    )
    {
        Semaphore[] imageAvailableSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
        Semaphore[] renderFinishedSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
        Fence[] inFlightFences = new Fence[MAX_FRAMES_IN_FLIGHT];
        Fence[] imagesInFlight = new Fence[swapChainImages!.Length];

        SemaphoreCreateInfo semaphoreInfo = new() { SType = StructureType.SemaphoreCreateInfo, };

        FenceCreateInfo fenceInfo =
            new() { SType = StructureType.FenceCreateInfo, Flags = FenceCreateFlags.SignaledBit, };

        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            if (
                vk!.CreateSemaphore(device, semaphoreInfo, null, out imageAvailableSemaphores[i])
                    != Result.Success
                || vk!.CreateSemaphore(device, semaphoreInfo, null, out renderFinishedSemaphores[i])
                    != Result.Success
                || vk!.CreateFence(device, fenceInfo, null, out inFlightFences[i]) != Result.Success
            )
            {
                throw new Exception("Failed to create synchronization objects for a frame!");
            }
        }

        return (imageAvailableSemaphores, renderFinishedSemaphores, inFlightFences, imagesInFlight);
    }
}
