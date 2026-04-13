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

        // Vanilla Winter Mystery event key in Data/Events/BusStop
        // We need to patch this event to add a precondition that the player must NOT have the Magnifying Glass,
        // otherwise the event will still trigger even after the player obtains it from Krobus's shop.
        private const string VANILLA_EVENT_KEY =
            "520702/a 11 23 11 24/t 600 1600/z spring/z fall/z summer";
        private const string PATCHED_EVENT_KEY =
            "520702/a 11 23 11 24/t 600 1600/z spring/z fall/z summer/gameStateQuery !PLAYER_HAS_FLAG Current HasMagnifyingGlass";

        public override void Entry(IModHelper helper)
        {
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Player.InventoryChanged += OnInventoryChanged;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
        }

        /**
        * When the game loads Data/Objects, we inject our custom item definition for the Magnifying Glass.
        * We do this instead of ContentPatcher because we need SMAPI anyway for the other logic that we have in this mod.
        *
        * We also patch the BusStop event to add a precondition that the player must not have the Magnifying Glass,
        * otherwise the event will still trigger even after the player obtains it from Krobus's shop.
        */
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            // --- Define the item in Data/Objects ---
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, ObjectData>().Data;
                    data[ITEM_ID] = new ObjectData
                    {
                        Name = ITEM_ID,
                        DisplayName = "Magnifying Glass",
                        Description =
                            "We shadow people are infatuated by glass! Clever of humans " +
                            "to make a material that shows what's behind it. Must be " +
                            "infused with some powerful magic, although of what kind I'm " +
                            "not so sure... And this one seems to be unique - it enlarges " +
                            "stuff behind it...",
                        Type = "Quest",
                        // Custom "Shadow Relic" category
                        Category = -200,
                        Price = 5000,
                        // Inedible
                        Edibility = -300,
                        // Sprite extracted from LooseSprites/Cursors at (209, 320, 16, 16).
                        // See assets/magnifying_glass.png in the mod folder.
                        Texture = Helper.ModContent.GetInternalAssetName("assets/magnifying_glass.png").Name,
                        SpriteIndex = 0,
                        ContextTags = new List<string>
                        {
                            "shadow_relic",
                            "not_placeable",
                            "prevent_loss_on_death",
                            "not_giftable",
                            "not_placeable",
                            "not_museum_donatable",
                        }
                    };
                });
            }

            // --- Patch the Winter Mystery event ---
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/BusStop"))
            {
                e.Edit(asset =>
                {
                    var events = asset.AsDictionary<string, string>().Data;
                    if (!events.TryGetValue(VANILLA_EVENT_KEY, out var script)) return;
                    events.Remove(VANILLA_EVENT_KEY);
                    events[PATCHED_EVENT_KEY] = script;
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
        // TODO: The animation triggers when item is placed in inventory.
        //       It would be better if animation triggered "as soon as we right click to buy and have it in the 'cursor'"
        //       Since that is how other similar items (like Stardrop from Krobus's shop) work)
        //       It would also prevent edge cases like player's inventory being full
        private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
        {
            // Already has the flag — nothing to do
            if (e.Player.hasMagnifyingGlass) return;

            foreach (var item in e.Added)
            {
                if (item.ItemId != ITEM_ID) continue;

                // Remove the dummy item — it was just a shop vehicle
                e.Player.removeItemFromInventory(item);

                // Grant the wallet item
                e.Player.hasMagnifyingGlass = true;

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

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            // Ensure the shop stock is updated at the start of each day, in case the player buys the item or time advances past the availability window
            if (Game1.content.Load<Dictionary<string, ShopData>>("Data/Shops").TryGetValue("ShadowShop", out var shop))
            {
                UpdateKrobusShop(shop);
            }
        }

        /**
        * Adds the Magnifying Glass to Krobus's shop stock until Fall 28 Year 1, if the player doesn't already have it.
        */
        private void UpdateKrobusShop(ShopData shop)
        {
            if ((Game1.stats.DaysPlayed <= 28 * 3) && (!Game1.player.hasMagnifyingGlass))
            {
                Monitor.Log("Adding shop item: Magnifying Glass", LogLevel.Debug);
                // Only available until Fall 28 Year 1
                shop.Items ??= new List<ShopItemData>();

                // Avoid adding a duplicate entry on repeated asset invalidations
                var entryId = $"{ITEM_ID}_Stock";
                if (shop.Items.Exists(i => i.Id == entryId)) return;

                // TODO: Add conditional Krobus dialogue lines:
                //   - If player hasn't obtained the Magnifying Glass yet:
                //      "I lost my magnifying glasses, if only I had my magnifying glasses, I could search for my magnifying glasses"
                //   - If player has obtained it (HasMagnifyingGlass flag set):
                //     "Oh hey, you found my missing magnifying glasses.. No no, you should keep it.. I insist.. Finder's keeper's an' all.."
                shop.Items.Add(new ShopItemData
                {
                    Id = entryId,
                    ItemId = ITEM_ID,
                    Price = 5000,
                    AvailableStock = 1, // Unique — only one exists
                });
            }
            else
            {
                // After Fall 28 Year 1, ensure it's removed from the shop (in case player hasn't bought it yet)
                Monitor.Log("Removing shop item: Magnifying Glass", LogLevel.Debug);
                shop.Items?.RemoveAll(i => i.ItemId == ITEM_ID);
            }
        }
    }
}