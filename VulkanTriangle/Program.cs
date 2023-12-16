using VulkanTriangle;

VulkanTriangleApplication app = new("Vulkan Triangle", 1280, 720);
try
{
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
}
