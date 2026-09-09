// ReSharper disable UnusedMember.Global
namespace Nocturne.Client;

/// <summary>
/// The drop-in class BepInEx's ConfigurationManager (the F12 window) looks for by name. It is never
/// referenced as a type - the fields are read reflectively - so this carries no reference to it and
/// is safe when that plugin is absent. Only the members this mod sets are declared.
/// </summary>
internal sealed class ConfigurationManagerAttributes
{
    /// <summary>Higher numbers sort earlier inside a section.</summary>
    public int? Order;

    /// <summary>Hide behind the "Advanced" toggle.</summary>
    public bool? IsAdvanced;
}
