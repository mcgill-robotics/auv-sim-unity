using UnityEngine.UIElements;
/// <summary>
/// Base class for all UI controllers in the SimulatorHUD. Provides a common structure for querying UI elements and registering callbacks.
/// Extracted from SimulatorHUD for better separation of concerns.

/// </summary>
public abstract class Controller
{

    protected void Initialize(VisualElement root)
    {
        QueryElements(root);
        RegisterCallbacks();
    }
    protected abstract void QueryElements(VisualElement root);
    protected abstract void RegisterCallbacks();
}