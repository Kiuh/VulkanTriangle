using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanTriangle;

internal static unsafe class VulkanBufferTools
{
    public static (Buffer, DeviceMemory) CreateBuffer(
        Vk vk,
        Device device,
        PhysicalDevice physicalDevice,
        ulong size,
        BufferUsageFlags usage,
        MemoryPropertyFlags properties
    )
    {
        BufferCreateInfo bufferInfo =
            new()
            {
                SType = StructureType.BufferCreateInfo,
                Size = size,
                Usage = usage,
                SharingMode = SharingMode.Exclusive,
            };

        if (vk!.CreateBuffer(device, bufferInfo, null, out Buffer buffer) != Result.Success)
        {
            throw new Exception("Failed to create vertex buffer!");
        }

        _ = new MemoryRequirements();
        vk!.GetBufferMemoryRequirements(device, buffer, out MemoryRequirements memRequirements);

        MemoryAllocateInfo allocateInfo =
            new()
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memRequirements.Size,
                MemoryTypeIndex = FindMemoryType(
                    memRequirements.MemoryTypeBits,
                    properties,
                    physicalDevice,
                    vk
                ),
            };

        if (
            vk!.AllocateMemory(device, allocateInfo, null, out DeviceMemory bufferMemory)
            != Result.Success
        )
        {
            throw new Exception("Failed to allocate vertex buffer memory!");
        }

        _ = vk!.BindBufferMemory(device, buffer, bufferMemory, 0);

        return (buffer, bufferMemory);
    }

    public static uint FindMemoryType(
        uint typeFilter,
        MemoryPropertyFlags properties,
        PhysicalDevice physicalDevice,
        Vk vk
    )
    {
        vk!.GetPhysicalDeviceMemoryProperties(
            physicalDevice,
            out PhysicalDeviceMemoryProperties memProperties
        );

        for (int i = 0; i < memProperties.MemoryTypeCount; i++)
        {
            if (
                (typeFilter & (1 << i)) != 0
                && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties
            )
            {
                return (uint)i;
            }
        }

        throw new Exception("Failed to find suitable memory type!");
    }

    public static void CopyBuffer(
        Vk vk,
        CommandPool commandPool,
        Device device,
        Queue graphicsQueue,
        Buffer srcBuffer,
        Buffer dstBuffer,
        ulong size
    )
    {
        CommandBufferAllocateInfo allocateInfo =
            new()
            {
                SType = StructureType.CommandBufferAllocateInfo,
                Level = CommandBufferLevel.Primary,
                CommandPool = commandPool,
                CommandBufferCount = 1,
            };

        _ = vk!.AllocateCommandBuffers(device, allocateInfo, out CommandBuffer commandBuffer);

        CommandBufferBeginInfo beginInfo =
            new()
            {
                SType = StructureType.CommandBufferBeginInfo,
                Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
            };

        _ = vk!.BeginCommandBuffer(commandBuffer, beginInfo);

        BufferCopy copyRegion = new() { Size = size, };

        vk!.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, copyRegion);

        _ = vk!.EndCommandBuffer(commandBuffer);

        SubmitInfo submitInfo =
            new()
            {
                SType = StructureType.SubmitInfo,
                CommandBufferCount = 1,
                PCommandBuffers = &commandBuffer,
            };

        _ = vk!.QueueSubmit(graphicsQueue, 1, submitInfo, default);
        _ = vk!.QueueWaitIdle(graphicsQueue);

        vk!.FreeCommandBuffers(device, commandPool, 1, commandBuffer);
    }
}
