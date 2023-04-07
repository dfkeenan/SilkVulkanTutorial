using Silk.NET.Maths;
using Silk.NET.Windowing;


var app = new HelloTriangleApplication();
app.Run();

unsafe class HelloTriangleApplication
{
    const int WIDTH = 800;
    const int HEIGHT = 600;

    private IWindow? window;

    public void Run()
    {
        InitWindow();
        InitVulkan();
        MainLoop();
        CleanUp();
    }

    private void InitWindow()
    {
        //Create a window.
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(WIDTH, HEIGHT),
            Title = "Vulkan"
        };

        window = Window.Create(options);
        window.Initialize();

        if (window.VkSurface is null)
        {
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }
    }

    private void InitVulkan()
    {

    }

    private void MainLoop()
    {
        window!.Run();
    }

    private void CleanUp()
    {
        window?.Dispose();
    }
}