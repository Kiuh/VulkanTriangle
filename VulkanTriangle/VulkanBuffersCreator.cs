using Silk.NET.Vulkan;
using System.Runtime.CompilerServices;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanTriangle;

internal static unsafe class VulkanBuffersCreator
{
    public static (Buffer, DeviceMemory) CreateVertexBuffer(
        Vk vulkanApi,
        Vertex[] vertices,
        Device device,
        PhysicalDevice physicalDevice,
        CommandPool commandPool,
        Queue graphicsQueue
    )
    {
        ulong bufferSize = (ulong)(Unsafe.SizeOf<Vertex>() * vertices.Length);

        (Buffer stagingBuffer, DeviceMemory stagingBufferMemory) = VulkanBufferTools.CreateBuffer(
            vulkanApi,
            device,
            physicalDevice,
            bufferSize,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
        );

        void* data;
        _ = vulkanApi!.MapMemory(device, stagingBufferMemory, 0, bufferSize, 0, &data);
        vertices.AsSpan().CopyTo(new Span<Vertex>(data, vertices.Length));
        vulkanApi!.UnmapMemory(device, stagingBufferMemory);

        (Buffer vertexBuffer, DeviceMemory vertexBufferMemory) = VulkanBufferTools.CreateBuffer(
            vulkanApi,
            device,
            physicalDevice,
            bufferSize,
            BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit,
            MemoryPropertyFlags.DeviceLocalBit
        );

        VulkanBufferTools.CopyBuffer(
            vulkanApi,
            commandPool,
            device,
            graphicsQueue,
            stagingBuffer,
            vertexBuffer,
            bufferSize
        );

        vulkanApi!.DestroyBuffer(device, stagingBuffer, null);
        vulkanApi!.FreeMemory(device, stagingBufferMemory, null);

        return (vertexBuffer, vertexBufferMemory);
    }

    public static (Buffer, DeviceMemory) CreateIndexBuffer(
        Vk vulkanApi,
        ushort[] indices,
        Device device,
        PhysicalDevice physicalDevice,
        CommandPool commandPool,
        Queue graphicsQueue
    )
    {
        ulong bufferSize = (ulong)(Unsafe.SizeOf<ushort>() * indices.Length);

        (Buffer stagingBuffer, DeviceMemory stagingBufferMemory) = VulkanBufferTools.CreateBuffer(
            vulkanApi,
            device,
            physicalDevice,
            bufferSize,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
        );

        void* data;
        _ = vulkanApi!.MapMemory(device, stagingBufferMemory, 0, bufferSize, 0, &data);
        indices.AsSpan().CopyTo(new Span<ushort>(data, indices.Length));
        vulkanApi!.UnmapMemory(device, stagingBufferMemory);

        (Buffer indexBuffer, DeviceMemory indexBufferMemory) = VulkanBufferTools.CreateBuffer(
            vulkanApi,
            device,
            physicalDevice,
            bufferSize,
            BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit,
            MemoryPropertyFlags.DeviceLocalBit
        );

        VulkanBufferTools.CopyBuffer(
            vulkanApi,
            commandPool,
            device,
            graphicsQueue,
            stagingBuffer,
            indexBuffer,
            bufferSize
        );

        vulkanApi!.DestroyBuffer(device, stagingBuffer, null);
        vulkanApi!.FreeMemory(device, stagingBufferMemory, null);

        return (indexBuffer, indexBufferMemory);
    }
}
