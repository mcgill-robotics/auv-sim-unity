using UnityEngine;
using UnityEngine.UIElements;

namespace Utils
{
    public static class UIUtils
    {
        /// <summary>
        /// Global check to see if the mouse is currently hovering over a meaningful UI element.
        /// This handles both UI Toolkit and prevents camera/world interaction through menus.
        /// </summary>
        public static bool IsMouseOverUI()
        {
            // UI Toolkit detection
            var hudDocument = SimulatorHUD.Instance?.uiDocument;
            if (hudDocument != null && hudDocument.rootVisualElement != null)
            {
                var panelPosition = RuntimePanelUtils.ScreenToPanel(
                    hudDocument.rootVisualElement.panel,
                    new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)
                );
                
                var pickedElement = hudDocument.rootVisualElement.panel.Pick(panelPosition);
                
                // Check if picked element is a meaningful UI element (not root or transparent container)
                if (pickedElement != null && pickedElement != hudDocument.rootVisualElement)
                {
                    // Walk up the tree to find if we're over an actual visible UI element
                    var element = pickedElement;
                    while (element != null && element != hudDocument.rootVisualElement)
                    {
                        // Check if this element has visible background/border or is interactive
                        if (element.resolvedStyle.backgroundColor.a > 0.01f ||
                            element.resolvedStyle.borderTopWidth > 0 ||
                            element is Button || element is Toggle || element is TextField ||
                            element is ScrollView || element is DropdownField ||
                            element.name.StartsWith("Drawer") || element.name.StartsWith("TopBar"))
                        {
                            return true;
                        }
                        element = element.parent;
                    }
                }
            }
            
            return false;
        }
    }
}
