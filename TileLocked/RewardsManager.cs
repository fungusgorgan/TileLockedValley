using StardewValley;
using StardewValley.Locations;
using TileLocked.Config;
using xTile.Tiles;

namespace TileLocked
{
  internal sealed class RewardsManager
  {
    private readonly int[] VAULT_BUNDLE_KEYS = new int[] {23, 24, 25, 26};
    private readonly Dictionary<int, bool[]> bundleState = new();
    private readonly TileManager tileManager;
    private CommunityCenter? communityCenter;

    public RewardsManager(TileManager tileManager)
    {
      this.tileManager = tileManager;
    }

    public void Initialize()
    {
      communityCenter = Game1.getLocationFromName("CommunityCenter") as CommunityCenter;
      UpdateBundleState();
    }

    public void CheckForChanges()
    {
      if (communityCenter != null)
      {
        CheckForBundleChanges(communityCenter);
        UpdateBundleState();
      }
    }

    private void CheckForBundleChanges(CommunityCenter communityCenter)
    { 
      foreach (var kvp in communityCenter.bundles.Pairs)
      {
        if (!bundleState.ContainsKey(kvp.Key))
        {
          bundleState[kvp.Key] = (bool[])kvp.Value.Clone();
          continue;
        }

        for (int i = 0; i < bundleState[kvp.Key].Length; i++)
        {
          if (kvp.Value[i] && !bundleState[kvp.Key][i])
          {
            bool bundleComplete = false;
            if (communityCenter.bundles.TryGetValue(kvp.Key, out bool[] slots))
            {
              // Workaround for vault bundles
              if (slots.All(slot => slot) || VAULT_BUNDLE_KEYS.Contains(kvp.Key))
              {
                tileManager.AddBankedTiles(PerSaveConfig.GetInt(PerSaveConfig.Key.NUM_BONUS_TILES_FOR_CC_BUNDLES));
                UpdateBundleState();
                bundleComplete = true;
              }
            }
            if (!bundleComplete)
            {
              tileManager.AddBankedTiles(PerSaveConfig.GetInt(PerSaveConfig.Key.NUM_BONUS_TILES_FOR_CC_ITEMS));
              UpdateBundleState();
            }
          }
        }
      }
    }

    private void UpdateBundleState()
    {
      if (communityCenter != null)
      {
        bundleState.Clear();
        foreach (var kvp in communityCenter.bundles.Pairs)
        {
          bundleState[kvp.Key] = (bool[])kvp.Value.Clone();
        }
      }
    }
  }
}