using UnityEngine;

/// <summary>
/// Singleton that manages rebindable keyboard inputs. Stores bindings in PlayerPrefs.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public KeyCode GetKey(string keyName, KeyCode defaultKey)
    {
        string savedKey = PlayerPrefs.GetString(keyName, defaultKey.ToString());
        if (System.Enum.TryParse(savedKey, out KeyCode result))
        {
            return result;
        }
        return defaultKey;
    }

    public void SetKey(string keyName, KeyCode key)
    {
        PlayerPrefs.SetString(keyName, key.ToString());
        PlayerPrefs.Save();
    }

    public void ResetToDefaults()
    {
        PlayerPrefs.DeleteAll(); // Or delete specific keys if you want to preserve other settings
        PlayerPrefs.Save();
        Debug.Log("[InputManager] Input bindings reset to defaults.");
    }
}
