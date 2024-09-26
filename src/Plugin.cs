namespace ExtendedItemTracking;

[BepInPlugin(MOD_ID, "Extended Item Tracking", "1.0.0")]

//--------------------------------------------------------------------------------

public class Plugin : BaseUnityPlugin
{
    private const string MOD_ID = "bry.extendeditemtracking";

    public bool IsInit;


    public void OnEnable()
    {
        On.RainWorld.OnModsInit += InitiateExtendedItemTracking;
    }

    // - - - - - - - - - - - - - - - - - - - - - -

    public void InitiateExtendedItemTracking(On.RainWorld.orig_OnModsInit orig, RainWorld rw)
    {
        orig(rw);

        try
        {
            if (IsInit) return;
            IsInit = true;

            Hooks.Apply();

            Debug.LogWarning($"Extended Item Tracking is ready to go!");
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            Debug.LogException(ex);
            throw;
        }

    }

}