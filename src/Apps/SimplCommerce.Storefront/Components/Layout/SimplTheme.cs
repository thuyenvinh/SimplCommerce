using MudBlazor;

namespace SimplCommerce.Storefront.Components.Layout;

public static class SimplTheme
{
    public static MudTheme Instance { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#3d72b4",
            Secondary = "#ff6b35",
            AppbarBackground = "#ffffff",
            AppbarText = "#202a3a",
            Background = "#f7f9fc",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#7aa7d9",
            Secondary = "#ff9466",
            AppbarBackground = "#1f2430",
            AppbarText = "#e6eaf2",
            Background = "#121824",
        },
        LayoutProperties = new LayoutProperties { DefaultBorderRadius = "8px" },
    };
}
