﻿using System;
using System.IO;
using UberStrok.Core.Views;

namespace UberStrok.Core.Serialization.Views
{
    public static class ItemInventoryViewProxy
    {
        public static ItemInventoryView Deserialize(Stream bytes)
        {
            int mask = Int32Proxy.Deserialize(bytes);
            ItemInventoryView view = new ItemInventoryView
            {
                AmountRemaining = Int32Proxy.Deserialize(bytes),
                Cmid = Int32Proxy.Deserialize(bytes)
            };

            if ((mask & 1) != 0)
            {
                view.ExpirationDate = new DateTime?(DateTimeProxy.Deserialize(bytes));
            }

            view.ItemId = Int32Proxy.Deserialize(bytes);
            return view;
        }

        public static void Serialize(Stream stream, ItemInventoryView instance)
        {
            int mask = 0;
            using (MemoryStream bytes = new MemoryStream())
            {
                Int32Proxy.Serialize(bytes, instance.AmountRemaining);
                Int32Proxy.Serialize(bytes, instance.Cmid);

                if (instance.ExpirationDate.HasValue)
                {
                    DateTimeProxy.Serialize(bytes, (!instance.ExpirationDate.HasValue) ? default : instance.ExpirationDate.Value);
                }
                else
                {
                    mask |= 1;
                }

                Int32Proxy.Serialize(bytes, instance.ItemId);
                Int32Proxy.Serialize(stream, ~mask);
                bytes.WriteTo(stream);
            }
        }
    }
}
