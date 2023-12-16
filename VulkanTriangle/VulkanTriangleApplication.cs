using Silk.NET.Core.Contexts;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace VulkanTriangle;

internal struct Vertex
{
    public Vector2D<float> pos;
    public Vector3D<float> color;

    public static VertexInputBindingDescription GetBindingDescription()
    {
        VertexInputBindingDescription bindingDescription = new()
        {
            Binding = 0,
            Stride = (uint)Unsafe.SizeOf<Vertex>(),
            InputRate = VertexInputRate.Vertex,
        };

        return bindingDescription;
    }

    public static VertexInputAttributeDescription[] GetAttributeDescriptions()
    {
        VertexInputAttributeDescription[] attributeDescriptions =
        [
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 0,
                Format = Format.R32G32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(pos)),
            },
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 1,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(color)),
            }
        ];

        return attributeDescriptions;
    }
}

internal unsafe class VulkanTriangleApplication
{
    private IWindow? window;
    private WindowOptions options;

    private readonly bool enableValidationLayers = true;
    private readonly string[] validationLayers = ["VK_LAYER_KHRONOS_validation"];
    private ExtDebugUtils? debugUtils;
    private DebugUtilsMessengerEXT debugMessenger;

    private readonly Vk vk;
    private Instance instance;

    private KhrSurface? khrSurface;
    private SurfaceKHR surface;

    private PhysicalDevice physicalDevice;
    private Device device;
    private readonly string[] deviceExtensions = [KhrSwapchain.ExtensionName];

    private Queue graphicsQueue;
    private Queue presentQueue;

    private KhrSwapchain? khrSwapChain;
    private SwapchainKHR swapChain;

    private Image[]? swapChainImages;
    private Format swapChainImageFormat;
    private Extent2D swapChainExtent;

    private ImageView[]? swapChainImageViews;
    private Framebuffer[]? swapChainFramebuffers;

    private RenderPass renderPass;
    private PipelineLayout pipelineLayout;
    private Pipeline graphicsPipeline;

    private CommandPool commandPool;
    private CommandBuffer[]? commandBuffers;

    private Semaphore[]? imageAvailableSemaphores;
    private Semaphore[]? renderFinishedSemaphores;
    private Fence[]? inFlightFences;
    private Fence[]? imagesInFlight;
    private int currentFrame = 0;
    private const int MAX_FRAMES_IN_FLIGHT = 2;

    private bool frameBufferResized = false;

    private readonly Vertex[] vertices =
    [
        new() { pos = new Vector2D<float>(-0.5f,-0.5f), color = new Vector3D<float>(1.0f, 0.0f, 0.0f) },
        new() { pos = new Vector2D<float>(0.5f,-0.5f), color = new Vector3D<float>(0.0f, 1.0f, 0.0f) },
        new() { pos = new Vector2D<float>(0.5f,0.5f), color = new Vector3D<float>(0.0f, 0.0f, 1.0f) },
        new() { pos = new Vector2D<float>(-0.5f,0.5f), color = new Vector3D<float>(1.0f, 1.0f, 1.0f) },
    ];

    private readonly ushort[] indices =
    [
        0, 1, 2, 2, 3, 0
    ];

    private Buffer vertexBuffer;
    private DeviceMemory vertexBufferMemory;

    private Buffer indexBuffer;
    private DeviceMemory indexBufferMemory;

    private IVkSurface VkSurface => window!.VkSurface!;

    public VulkanTriangleApplication(string title, int width, int height)
    {
        vk = Vk.GetApi();
        options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(width, height),
            Title = title
        };
    }

    public void Run()
    {
        InitWindow();
        InitVulkan();
        MainLoop();
        CleanUp();
    }

    private void InitWindow()
    {
        window = Window.Create(options);
        window.Initialize();

        if (window.VkSurface is null)
        {
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }

        window.Resize += FramebufferResizeCallback;
    }

    private void FramebufferResizeCallback(Vector2D<int> obj)
    {
        frameBufferResized = true;
    }

    private void InitVulkan()
    {
        instance = VulkanInstanceCreator.CreateInstance(vk, VkSurface, enableValidationLayers, validationLayers);
        (debugUtils, debugMessenger) = VulkanInstanceCreator.SetupDebugMessenger(enableValidationLayers, vk, instance);
        (khrSurface, surface) = VulkanSurfaceCreator.CreateSurface(vk, instance, VkSurface);
        physicalDevice = VulkanPhysicalDevicePicker.PickPhysicalDevice(vk, instance, khrSurface!, surface, deviceExtensions);
        (device, graphicsQueue, presentQueue) = VulkanLogicalDeviceCreator.CreateLogicalDevice(vk, physicalDevice, enableValidationLayers, validationLayers, deviceExtensions, khrSurface!, surface);

        CreateSwapChainStuff(true);

        (imageAvailableSemaphores, renderFinishedSemaphores, inFlightFences, imagesInFlight) = VulkanSyncObjectCreator.CreateSyncObjects(vk, device, swapChainImages!, MAX_FRAMES_IN_FLIGHT);
    }

    private void CreateSwapChainStuff(bool createVertexBuffer)
    {
        (khrSwapChain, swapChain, swapChainImages, swapChainImageFormat, swapChainExtent) = VulkanSwapChainCreator.CreateSwapChain(vk, physicalDevice, khrSurface!, surface, instance, device, window!);
        swapChainImageViews = VulkanImageViewsCreator.CreateImageViews(vk, swapChainImages, swapChainImageFormat, device);
        renderPass = VulkanRenderPassCreator.CreateRenderPass(vk, swapChainImageFormat, device);
        (graphicsPipeline, pipelineLayout) = VulkanGraphicPipelineCreator.CreateGraphicsPipeline(vk, renderPass, device, swapChainExtent);
        swapChainFramebuffers = VulkanFrameBuffersCreator.CreateFramebuffers(vk, swapChainImageViews, renderPass, swapChainExtent, device);
        commandPool = VulkanCommandTools.CreateCommandPool(vk, device, physicalDevice, khrSurface!, surface);
        if (createVertexBuffer)
        {
            (vertexBuffer, vertexBufferMemory) = VulkanBuffersCreator.CreateVertexBuffer(vk, vertices, device, physicalDevice, commandPool, graphicsQueue);
            (indexBuffer, indexBufferMemory) = VulkanBuffersCreator.CreateIndexBuffer(vk, indices, device, physicalDevice, commandPool, graphicsQueue);
        }
        commandBuffers = VulkanCommandTools.CreateCommandBuffers(vk, swapChainFramebuffers, commandPool, device, renderPass, swapChainExtent, graphicsPipeline, vertexBuffer, vertices, indexBuffer, indices);
    }

    private void MainLoop()
    {
        window!.Render += DrawFrame;
        window!.Run();
        _ = vk!.DeviceWaitIdle(device);
    }

    private void DrawFrame(double delta)
    {
        _ = vk!.WaitForFences(device, 1, inFlightFences![currentFrame], true, ulong.MaxValue);

        uint imageIndex = 0;
        Result result = khrSwapChain!.AcquireNextImage(device, swapChain, ulong.MaxValue, imageAvailableSemaphores![currentFrame], default, ref imageIndex);

        if (result == Result.ErrorOutOfDateKhr)
        {
            RecreateSwapChain();
            return;
        }
        else if (result is not Result.Success and not Result.SuboptimalKhr)
        {
            throw new Exception("Failed to acquire swap chain image!");
        }

        if (imagesInFlight![imageIndex].Handle != default)
        {
            _ = vk!.WaitForFences(device, 1, imagesInFlight[imageIndex], true, ulong.MaxValue);
        }
        imagesInFlight[imageIndex] = inFlightFences[currentFrame];


        Semaphore* waitSemaphores = stackalloc[] { imageAvailableSemaphores[currentFrame] };
        PipelineStageFlags* waitStages = stackalloc[] { PipelineStageFlags.ColorAttachmentOutputBit };
        CommandBuffer buffer = commandBuffers![imageIndex];
        Semaphore* signalSemaphores = stackalloc[] { renderFinishedSemaphores![currentFrame] };

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = waitSemaphores,
            PWaitDstStageMask = waitStages,
            CommandBufferCount = 1,
            PCommandBuffers = &buffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = signalSemaphores,
        };

        _ = vk!.ResetFences(device, 1, inFlightFences[currentFrame]);

        if (vk!.QueueSubmit(graphicsQueue, 1, submitInfo, inFlightFences[currentFrame]) != Result.Success)
        {
            throw new Exception("Failed to submit draw command buffer!");
        }

        SwapchainKHR* swapChains = stackalloc[] { swapChain };
        PresentInfoKHR presentInfo = new()
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = signalSemaphores,
            SwapchainCount = 1,
            PSwapchains = swapChains,
            PImageIndices = &imageIndex
        };

        result = khrSwapChain.QueuePresent(presentQueue, presentInfo);

        if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr || frameBufferResized)
        {
            frameBufferResized = false;
            RecreateSwapChain();
        }
        else if (result != Result.Success)
        {
            throw new Exception("failed to present swap chain image!");
        }

        currentFrame = (currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;
    }

    private void RecreateSwapChain()
    {
        Vector2D<int> framebufferSize = window!.FramebufferSize;

        while (framebufferSize.X == 0 || framebufferSize.Y == 0)
        {
            framebufferSize = window.FramebufferSize;
            window.DoEvents();
        }

        _ = vk!.DeviceWaitIdle(device);

        CleanUpSwapChain();

        CreateSwapChainStuff(false);

        imagesInFlight = new Fence[swapChainImages!.Length];
    }

    private void CleanUpSwapChain()
    {
        foreach (Framebuffer framebuffer in swapChainFramebuffers!)
        {
            vk!.DestroyFramebuffer(device, framebuffer, null);
        }

        fixed (CommandBuffer* commandBuffersPtr = commandBuffers)
        {
            vk!.FreeCommandBuffers(device, commandPool, (uint)commandBuffers!.Length, commandBuffersPtr);
        }

        vk.DestroyPipeline(device, graphicsPipeline, null);
        vk.DestroyPipelineLayout(device, pipelineLayout, null);
        vk.DestroyRenderPass(device, renderPass, null);
        foreach (ImageView imageView in swapChainImageViews!)
        {
            vk.DestroyImageView(device, imageView, null);
        }

        khrSwapChain!.DestroySwapchain(device, swapChain, null);
    }

    private void CleanUp()
    {
        CleanUpSwapChain();

        vk!.DestroyBuffer(device, vertexBuffer, null);
        vk!.FreeMemory(device, vertexBufferMemory, null);

        vk!.DestroyBuffer(device, vertexBuffer, null);
        vk!.FreeMemory(device, vertexBufferMemory, null);

        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            vk!.DestroySemaphore(device, renderFinishedSemaphores![i], null);
            vk!.DestroySemaphore(device, imageAvailableSemaphores![i], null);
            vk!.DestroyFence(device, inFlightFences![i], null);
        }

        vk.DestroyCommandPool(device, commandPool, null);

        vk.DestroyDevice(device, null);
        if (enableValidationLayers)
        {
            debugUtils!.DestroyDebugUtilsMessenger(instance, debugMessenger, null);
        }

        khrSurface!.DestroySurface(instance, surface, null);
        vk.DestroyInstance(instance, null);
        vk.Dispose();
        window?.Dispose();
    }
}
