using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UberStrok.Core.Common;
using UberStrok.Core.Views;
using UberStrok.WebServices.AspNetCore.Core.Manager;
using UberStrok.WebServices.AspNetCore.Core.Session;

namespace UberStrok.WebServices.AspNetCore.Helper
{
    public static class UserHelper
    {
        public static async Task<int> AddItemToInventory(string authToken, int itemID, int days = -1, int itemCount = -1)
        {
            if (authToken != null && itemID != 0 && days != 0 && itemCount != 0 && GameSessionManager.TryGet(authToken, out GameSession session))
            {
                foreach (ItemInventoryView item in session.Document.Inventory) //search inventory for item
                {
                    if (item.ItemId == itemID) //if item is in inventory
                    {
                        if (item.ExpirationDate == null) //if user already has item as permanent
                        {
                            return 2;
                        }

                        if (days == -1) //if user gets item as permanent from achievement
                        {
                            item.ExpirationDate = null;
                        }
                        else if (item.ExpirationDate <= DateTime.UtcNow) //if item has been expired in inventory, it needs to be renewed, by adding duration to current date, instead of expired date
                        {
                            item.ExpirationDate = DateTime.UtcNow.AddDays(days);
                        }
                        else //if item is not permanent, and has not expired, extra days are added to expiry date
                        {
                            DateTime expirationdate = item.ExpirationDate.Value;
                            _ = expirationdate.AddDays(days);
                            item.ExpirationDate = expirationdate;
                        }
                        await UserManager.Save(session.Document);
                        return 1;
                    }
                }
                //if user doesnt have the item in inventory
                DateTime? date = null;
                if (days > 0)
                {
                    date = DateTime.Now.AddDays(days);
                }
                session.Document.Inventory.Add(new ItemInventoryView(itemID, date, itemCount, session.Document.UserId));
                await UserManager.Save(session.Document);
                return 1;
            }
            return -1;
        }

        public static async Task<bool> AddItemToInventory(GameSession session, int itemID, int days = -1, int itemCount = -1)
        {
            if (session != null && itemID != 0 && days != 0 && itemCount != 0)
            {
                foreach (ItemInventoryView item in session.Document.Inventory) //search inventory for item
                {
                    if (item.ItemId == itemID) //if item is in inventory
                    {
                        if (item.ExpirationDate == null) //if user already has item as permanent
                        {
                            return true;
                        }

                        if (days == -1) //if user gets item as permanent from achievement
                        {
                            item.ExpirationDate = null;
                        }
                        else if (item.ExpirationDate <= DateTime.UtcNow) //if item has been expired in inventory, it needs to be renewed, by adding duration to current date, instead of expired date
                        {
                            item.ExpirationDate = DateTime.UtcNow.AddDays(days);
                        }
                        else //if item is not permanent, and has not expired, extra days are added to expiry date
                        {
                            DateTime expirationdate = item.ExpirationDate.Value;
                            _ = expirationdate.AddDays(days);
                            item.ExpirationDate = expirationdate;
                        }
                        await UserManager.Save(session.Document);
                        return true;
                    }
                }
                //if user doesnt have the item in inventory
                DateTime? date = null;
                if (days > 0)
                {
                    date = DateTime.Now.AddDays(days);
                }
                session.Document.Inventory.Add(new ItemInventoryView(itemID, date, itemCount, session.Document.UserId));
                await UserManager.Save(session.Document);
                return true;
            }
            return false;
        }

        public static async Task<bool> AddCurrencyToWallet(string authToken, UberStrikeCurrencyType currency, int amount)
        {
            if (GameSessionManager.TryGet(authToken, out GameSession session))
            {
                if (currency == UberStrikeCurrencyType.Credits)
                {
                    session.Document.Wallet.Credits += amount;
                    await UserManager.Save(session.Document);
                    return true;
                }
                else if (currency == UberStrikeCurrencyType.Points)
                {
                    session.Document.Wallet.Points += amount;
                    await UserManager.Save(session.Document);
                    return true;
                }
            }
            return false;
        }

        public static async Task<bool> AddCurrencyToWallet(GameSession session, UberStrikeCurrencyType currency, int amount)
        {
            if (currency == UberStrikeCurrencyType.Credits)
            {
                session.Document.Wallet.Credits += amount;
                await UserManager.Save(session.Document);
                return true;
            }
            else if (currency == UberStrikeCurrencyType.Points)
            {
                session.Document.Wallet.Points += amount;
                await UserManager.Save(session.Document);
                return true;
            }
            return false;
        }

        public static bool CheckLoadoutAndInventory(LoadoutView loadout, List<ItemInventoryView> inventory)
        {
            if (!inventory.Any(x => x.ItemId == loadout.Boots) && loadout.Boots != 0 && loadout.Boots != 1089)
            {
                return false;
            }

            if (!inventory.Any(x => x.ItemId == loadout.Face) && loadout.Face != 0)
            {
                return false;
            }

            if (!inventory.Any(x => x.ItemId == loadout.FunctionalItem1) && loadout.FunctionalItem1 != 0)
            {
                return false;
            }

            if (!inventory.Any(x => x.ItemId == loadout.FunctionalItem2) && loadout.FunctionalItem2 != 0)
            {
                return false;
            }

            if (!inventory.Any(x => x.ItemId == loadout.FunctionalItem3) && loadout.FunctionalItem3 != 0)
            {
                return false;
            }

            if (!inventory.Any(x => x.ItemId == loadout.Gloves) && loadout.Gloves != 0 && loadout.Gloves != 1086)
            {
                return false;
            }

            if (!inventory.Any(x => x.ItemId == loadout.Head) && loadout.Head != 0 && loadout.Head != 1084)
            {
                return false;
            }

            if (!inventory.Any(x => x.ItemId == loadout.LowerBody) && loadout.LowerBody != 0 && loadout.LowerBody != 1088)
            {
                return false;
            }

            if (!inventory.Any(x => x.ItemId == loadout.MeleeWeapon) && loadout.MeleeWeapon != 0)
            {
                return false;
            }

            return (inventory.Any(x => x.ItemId == loadout.QuickItem1) || loadout.QuickItem1 == 0)
&& (inventory.Any(x => x.ItemId == loadout.QuickItem2) || loadout.QuickItem2 == 0)
&& (inventory.Any(x => x.ItemId == loadout.QuickItem3) || loadout.QuickItem3 == 0)
&& (inventory.Any(x => x.ItemId == loadout.UpperBody) || loadout.UpperBody == 0 || loadout.UpperBody == 1087)
&& (inventory.Any(x => x.ItemId == loadout.Weapon1) || loadout.Weapon1 == 0)
&& (inventory.Any(x => x.ItemId == loadout.Weapon2) || loadout.Weapon2 == 0)
&& (inventory.Any(x => x.ItemId == loadout.Weapon3) || loadout.Weapon3 == 0);
        }
    }
}
