using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Shops;
using StardewValley.Objects;

namespace KrobusMagnifyingGlass
{
    public class ModEntry : Mod
    {
        // Unique item ID — namespaced to avoid conflicts
        private const string ITEM_ID = "Raghu.KrobusMagnifyingGlass_MagnifyingGlass";

        // The mail flag SDV uses to track Magnifying Glass ownership
        private const string MAIL_FLAG = "HasMagnifyingGlass";

        // Vanilla Winter Mystery event key in Data/Events/BusStop
        private const string VANILLA_EVENT_KEY =
            "520702/a 11 23 11 24/t 600 1600/z spring/z fall/z summer";

        // Same event, with extra precondition: player must NOT have the mail flag
        private const string PATCHED_EVENT_KEY =
            "520702/a 11 23 11 24/t 600 1600/z spring/z fall/z summer/gameStateQuery !PLAYER_HAS_FLAG Current HasMagnifyingGlass";

        public override void Entry(IModHelper helper)
        {
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Player.InventoryChanged += OnInventoryChanged;
        }

        // -------------------------------------------------------
        // Asset patches: object definition, shop stock, event key
        // -------------------------------------------------------
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            Monitor.Log("Patching Data/Shops...", LogLevel.Debug);
            // --- Define the item in Data/Objects ---
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, ObjectData>().Data;
                    data[ITEM_ID] = new ObjectData
                    {
                        Name          = ITEM_ID,
                        DisplayName   = "Magnifying Glass",
                        Description   =
                            "We shadow people are infatuated by glass! Clever of humans " +
                            "to make a material that shows what's behind it. Must be " +
                            "infused with some powerful magic, although of what kind I'm " +
                            "not so sure... And this one seems to be unique - it enlarges " +
                            "stuff behind it...",
                        Type          = "Basic",
                        Category      = -789,       // Custom "Shadow Relic" category
                        Price         = 5000,
                        Edibility     = -300,       // Inedible
                        // Sprite extracted from LooseSprites/Cursors at (209, 320, 16, 16).
                        // See assets/magnifying_glass.png in the mod folder.
                        Texture       = Helper.ModContent.GetInternalAssetName("assets/magnifying_glass.png").Name,
                        SpriteIndex   = 0,
                        ContextTags   = new List<string>
                        {
                            "shadow_relic",
                            "not_placeable",
                            "prevent_loss_on_death"
                        }
                    };
                });
            }

            // --- Add to Krobus's shop ---
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops"))
            {
                e.Edit(asset =>
                {
                    var shops = asset.AsDictionary<string, ShopData>().Data;
                    if (!shops.TryGetValue("ShadowShop", out var shop)) return;

                    shop.Items ??= new List<ShopItemData>();

                    // Avoid adding a duplicate entry on repeated asset invalidations
                    var entryId = $"{ITEM_ID}_Stock";
                    if (shop.Items.Exists(i => i.Id == entryId)) return;

                    shop.Items.Add(new ShopItemData
                    {
                        Id                = entryId,
                        ItemId            = ITEM_ID,
                        Price             = 5000,
                        AvailableStock = 1, // Unique — only one exists
                        // Only available before Winter 1 Year 1.
                        PerItemCondition = "YEAR 1, !SEASON winter" // Only available Fall Year 1
                    });
                });
            }

            // --- Patch the Winter Mystery event ---
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/BusStop"))
            {
                e.Edit(asset =>
                {
                    var events = asset.AsDictionary<string, string>().Data;

                    // Only patch if the vanilla key still exists (guards against
                    // other mods or game updates changing it)
                    if (!events.TryGetValue(VANILLA_EVENT_KEY, out var script)) return;

                    events.Remove(VANILLA_EVENT_KEY);
                    events[PATCHED_EVENT_KEY] = script;  // Script is unchanged
                });
            }
        }

        // -------------------------------------------------------
        // When the player receives our shop item:
        //   1. Immediately remove it from inventory (wallet item,
        //      not an inventory item)
        //   2. Set the vanilla HasMagnifyingGlass mail flag
        //   3. Show the vanilla "You can now find secret notes!"
        //      HUD message, localized automatically
        // -------------------------------------------------------
        private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
        {
            // Already has the flag — nothing to do
            if (e.Player.mailReceived.Contains(MAIL_FLAG)) return;

            foreach (var item in e.Added)
            {
                if (item.ItemId != ITEM_ID) continue;

                // Remove the dummy item — it was just a shop vehicle
                e.Player.removeItemFromInventory(item);

                // Grant the wallet item
                e.Player.mailReceived.Add(MAIL_FLAG);

                // OLD APPROACH: Show a custom HUD message. Downside: Doesn't trigger the hold-up animation.
                // // Show the vanilla HUD message (auto-localized)
                // string displayName = Game1.content.LoadString("Strings\\Objects:MagnifyingGlass");
                // Game1.addHUDMessage(new HUDMessage(
                //     Game1.content.LoadString("Strings\\Objects:MagnifyingGlassDescription", displayName),
                //     HUDMessage.newQuest_type
                // ));

                // Force-close the shop dialog
                Game1.activeClickableMenu?.exitThisMenu();

                // Play the vanilla hold-up animation + message, same as the quest reward.
                // SpecialItem(5) is the Magnifying Glass wallet item.
                Game1.player.holdUpItemThenMessage(new SpecialItem(5));

                Monitor.Log(
                    "Player purchased Magnifying Glass from Krobus. Granted wallet item, closed shop, played hold-up animation.",
                    LogLevel.Debug
                );

                break;
            }
        }
    }
}