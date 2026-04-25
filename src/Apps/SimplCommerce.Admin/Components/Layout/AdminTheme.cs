using MudBlazor;

namespace SimplCommerce.Admin.Components.Layout;

public static class AdminTheme
{
    public static MudTheme Instance { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1976d2",
            Secondary = "#5c6bc0",
            AppbarBackground = "#ffffff",
            AppbarText = "#121a29",
            Background = "#f4f6fb",
            DrawerBackground = "#ffffff",
            DrawerText = "#121a29",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#90caf9",
            Secondary = "#9fa8da",
            AppbarBackground = "#1e2430",
            AppbarText = "#e6eaf2",
            Background = "#121824",
            DrawerBackground = "#1e2430",
            DrawerText = "#e6eaf2",
        },
        LayoutProperties = new LayoutProperties { DefaultBorderRadius = "6px" },
    };
}
