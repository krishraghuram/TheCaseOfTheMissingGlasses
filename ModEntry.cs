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
                        Name        = ITEM_ID,
                        DisplayName = "Magnifying Glass",
                        Description =
                            "We shadow people are infatuated by glass! Clever of humans " +
                            "to make a material that shows what's behind it. Must be " +
                            "infused with some powerful magic, although of what kind I'm " +
                            "not so sure... And this one seems to be unique - it enlarges " +
                            "stuff behind it...",
                        Type        = "Basic",
                        Category    = -789, // Custom "Shadow Relic" category
                        Price       = 5000,
                        Edibility   = -300, // Inedible
                        // Sprite extracted from LooseSprites/Cursors
                        Texture     = Helper.ModContent.GetInternalAssetName("assets/magnifying_glass.png").Name,
                        SpriteIndex = 0,
                        ContextTags = new List<string>
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
                        Id               = entryId,
                        ItemId           = ITEM_ID,
                        Price            = 5000,
                        AvailableStock   = 1, // Unique — only one exists
                        PerItemCondition = "YEAR 1, !SEASON winter"
                    });
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

            // --- Krobus dialogue ---
            if (e.NameWithoutLocale.IsEquivalentTo("Characters/Dialogue/Krobus"))
            {
                e.Edit(asset =>
                {
                    var dialogue = asset.AsDictionary<string, string>().Data;
                    bool hasGlass = Game1.player?.mailReceived.Contains(MAIL_FLAG) ?? false;

                    if (hasGlass)
                    {
                        // Player has the Magnifying Glass — Krobus recognises it but insists they keep it.
                        // Uses SDV's dialogue cycling syntax ($0, $1...) so it rotates across visits.
                        dialogue["Tue"] =
                            "Oh hey... is that my magnifying glass? I'd recognise it anywhere.$s#$b#" +
                            "No no, please — you keep it. Finder's keeper's an' all...$s";
                        dialogue["Wed"] =
                            "I see you still have the glass. It suits you, I think.$s#$b#" +
                            "We shadow people have a saying — 'what shines in another's hands, " +
                            "was never truly yours'.$s";
                        dialogue["Thu"] =
                            "Every time I see that glass, I think of how it found its way to you.$s#$b#" +
                            "Perhaps that is its magic.$s";
                    }
                    else
                    {
                        // Player hasn't obtained the Magnifying Glass yet — Krobus laments its loss.
                        dialogue["Tue"] =
                            "I lost something precious recently... a glass that makes small things large.$s#$b#" +
                            "If only I had my magnifying glass, I could search for my magnifying glass...$s";
                        dialogue["Wed"] =
                            "We shadow people are fascinated by glass. It shows what is behind it — " +
                            "like a window into the unseen.$s#$b#" +
                            "I had one. A special one. I miss it dearly.$s";
                        dialogue["Thu"] =
                            "Someone took it. Another of my kind, I suspect.$s#$b#" +
                            "We are all drawn to glass. I cannot blame them. " +
                            "But I do wish they had asked.$s";
                    }
                });
            }
        }

        // -------------------------------------------------------
        // When the player receives our shop item:
        //   1. Remove it from inventory — it's a wallet item
        //   2. Set the HasMagnifyingGlass mail flag
        //   3. Invalidate Krobus's dialogue so it updates
        //      immediately to the "found" variant
        //   4. Close the shop and play the hold-up animation
        // -------------------------------------------------------
        private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
        {
            if (e.Player.mailReceived.Contains(MAIL_FLAG)) return;

            foreach (var item in e.Added)
            {
                if (item.ItemId != ITEM_ID) continue;

                e.Player.removeItemFromInventory(item);
                e.Player.mailReceived.Add(MAIL_FLAG);

                // Invalidate dialogue cache so Krobus's lines update
                // to the post-purchase variant right away
                Helper.GameContent.InvalidateCache("Characters/Dialogue/Krobus");

                Game1.activeClickableMenu?.exitThisMenu();
                Game1.player.holdUpItemThenMessage(new SpecialItem(5));

                Monitor.Log(
                    "Player purchased Magnifying Glass from Krobus. Granted wallet item, invalidated dialogue cache, closed shop, played hold-up animation.",
                    LogLevel.Debug
                );

                break;
            }
        }
    }
}